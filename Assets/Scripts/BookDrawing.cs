using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kitob sahifasida chizish/bo'yash. MOBIL uchun optimallashtirilgan:
/// chizayotgan paytda tekstura GPU'ga yuklanmaydi — jonli chiziq VEKTOR MESH (InkLine) bilan
/// ko'rsatiladi (arzon), barmoq ko'tarilganda bir marta doimiy teksturaga "bake" qilinadi.
/// O'chirg'ich teksturaga ishlaydi. Chizmalar diskка (persistentDataPath) saqlanadi.
/// BookPro logikasiga tegmaydi (faqat interactable).
/// </summary>
public class BookDrawing : MonoBehaviour
{
    BookPro book;
    Canvas canvas;
    Camera cam;
    TMP_FontAsset font;

    Color brushColor = new Color(0.90f, 0.25f, 0.25f);
    bool drawMode = false;
    bool eraser = false;

    class Layer { public Texture2D tex; public Color32[] buf; public int w, h; public bool dirty; }
    readonly Dictionary<GameObject, Layer> layers = new Dictionary<GameObject, Layer>();

    RectTransform toolbarRt;
    readonly List<GameObject> toolButtons = new List<GameObject>();
    TMP_Text toggleLabel;

    // jonli chiziq (pen)
    InkLine activeInk; GameObject inkPage; Layer inkLayer; RectTransform inkRect; float inkWidth;
    // o'chirg'ich holati
    bool eLastValid; int eLastX, eLastY; GameObject ePage; float lastApplyT;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void TouchInk_Init();
    [DllImport("__Internal")] static extern string TouchInk_Read();
    [DllImport("__Internal")] static extern void TouchInk_Sync();
    [DllImport("__Internal")] static extern string SSA_InitData();
    [DllImport("__Internal")] static extern string SSA_ApiBase();
#endif

    string apiBase = "", initData = "";
    bool CfgReady => !string.IsNullOrEmpty(apiBase) && !string.IsNullOrEmpty(initData);
    void RefreshCfg()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrEmpty(apiBase)) apiBase = SSA_ApiBase();
        if (string.IsNullOrEmpty(initData)) initData = SSA_InitData();
#endif
    }

    [System.Serializable] class PagesResp { public List<int> pages; }

    int PageKey(GameObject page)
    {
        if (page == null) return -1;
        return int.TryParse(page.name.Replace("Page", ""), out int n) ? n : -1;
    }
    GameObject PageByKey(int n)
    {
        if (book == null || book.papers == null) return null;
        string nm = "Page" + n;
        foreach (var p in book.papers)
        {
            if (p == null) continue;
            if (p.Front && p.Front.name == nm) return p.Front;
            if (p.Back && p.Back.name == nm) return p.Back;
        }
        return null;
    }

    // Chizmani mahalliy + backend'га (akkountга) saqlaydi
    void PersistPage(GameObject page)
    {
        SaveLayer(page);
        if (isActiveAndEnabled) StartCoroutine(CloudSave(page));
    }

    IEnumerator CloudSave(GameObject page)
    {
        RefreshCfg();
        if (!CfgReady || page == null || !layers.TryGetValue(page, out var l)) yield break;
        int k = PageKey(page); if (k < 0) yield break;
        byte[] png = l.tex.EncodeToPNG();
        string url = apiBase + "/draw/save?page=" + k + "&initData=" + UnityWebRequest.EscapeURL(initData);
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(png);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/octet-stream");
        yield return req.SendWebRequest();
        req.Dispose();
    }

    IEnumerator CloudLoad()
    {
        float t = 0f;
        while (!CfgReady && t < 8f) { RefreshCfg(); t += 0.4f; yield return new WaitForSeconds(0.4f); }
        if (!CfgReady) yield break;

        string listUrl = apiBase + "/draw/list?initData=" + UnityWebRequest.EscapeURL(initData);
        var req = UnityWebRequest.Get(listUrl);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { req.Dispose(); yield break; }
        PagesResp resp = null;
        try { resp = JsonUtility.FromJson<PagesResp>(req.downloadHandler.text); } catch { }
        req.Dispose();
        if (resp?.pages == null) yield break;

        foreach (int pg in resp.pages) yield return CloudGetPage(pg);
    }

    IEnumerator CloudGetPage(int pg)
    {
        var page = PageByKey(pg);
        if (page == null) yield break;
        string url = apiBase + "/draw/get?page=" + pg + "&initData=" + UnityWebRequest.EscapeURL(initData);
        var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.data != null && req.downloadHandler.data.Length > 100)
        {
            var rt = page.GetComponent<RectTransform>();
            var layer = GetLayer(page, rt);
            if (layer.tex.LoadImage(req.downloadHandler.data))
            {
                layer.w = layer.tex.width; layer.h = layer.tex.height;
                layer.buf = layer.tex.GetPixels32();
                SaveLayer(page); // mahalliy keshni yangilaymiz
            }
        }
        req.Dispose();
    }

    void SyncFS()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { TouchInk_Sync(); } catch { }
#endif
    }

    string SaveDir => Path.Combine(Application.persistentDataPath, "drawings");
    string SavePath(GameObject page) => Path.Combine(SaveDir, page.name + ".png");

    void Awake()
    {
        font = TMP_Settings.defaultFontAsset;
        book = Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        // Sahifalar shaffof emas (skan rasmlar) -> faqat joriy yoyilma render qilinsin (112 emas).
        // Bu mobil tezlikni keskin oshiradi. (BookPro.Start dan oldin, Awake'da.)
        if (book != null) book.hasTransparentPages = false;
        canvas = book != null ? book.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault();
        cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        BuildToolbar();
        SetDrawMode(false);
    }

    IEnumerator Start()
    {
        yield return null; yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
        TouchInk_Init();
#endif
        LoadSavedDrawings();
        StartCoroutine(CloudLoad()); // akkount (backend) chizmalarini yuklaymiz
    }

    bool OverToolbar(Vector2 sp)
    {
        return toolbarRt != null && RectTransformUtility.RectangleContainsScreenPoint(toolbarRt, sp, null);
    }

    void Update()
    {
        if (book == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Buferni HAR DOIM bo'shatamiz — chizish o'chiq bo'lsa nuqtalar tashlab yuboriladi
        // (aks holda o'chiq paytda yurgizган qo'l keyin CHIZISH bosilганда chizilib qolardi).
        string data = TouchInk_Read();
        if (drawMode && !string.IsNullOrEmpty(data))
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var parts = data.Split(';');
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                var c = p.Split(',');
                if (c.Length < 3) continue;
                if (!float.TryParse(c[0], System.Globalization.NumberStyles.Float, inv, out float x)) continue;
                if (!float.TryParse(c[1], System.Globalization.NumberStyles.Float, inv, out float y)) continue;
                float.TryParse(c[2], System.Globalization.NumberStyles.Float, inv, out float d);
                if (d > 0.5f)
                {
                    Vector2 sp = new Vector2(x, y);
                    if (OverToolbar(sp)) EndStroke(true); else PaintAt(sp);
                }
                else EndStroke(true);
            }
        }
#else
        if (drawMode)
        {
            bool down; Vector2 sp;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                sp = t.position;
                down = t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            }
            else { down = Input.GetMouseButton(0); sp = Input.mousePosition; }

            if (down)
            {
                if (OverToolbar(sp)) EndStroke(true);
                else PaintAt(sp);
            }
            else EndStroke(true);
        }
#endif

        // o'chirg'ich teksturaga yozadi — cheklangan tezlikda yuklaymiz
        if (Time.unscaledTime - lastApplyT > 0.033f)
        {
            foreach (var l in layers.Values)
                if (l.dirty) { l.tex.SetPixels32(l.buf); l.tex.Apply(false); l.dirty = false; }
            lastApplyT = Time.unscaledTime;
        }
    }

    GameObject PageUnder(Vector2 sp)
    {
        if (book.papers == null) return null;
        int cp = book.CurrentPaper;
        var cands = new List<GameObject>();
        if (cp >= 0 && cp < book.papers.Length && book.papers[cp].Front) cands.Add(book.papers[cp].Front);
        if (cp - 1 >= 0 && cp - 1 < book.papers.Length && book.papers[cp - 1].Back) cands.Add(book.papers[cp - 1].Back);
        foreach (var go in cands)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, sp, cam)) return go;
        }
        return null;
    }

    void PaintAt(Vector2 sp)
    {
        var page = PageUnder(sp);
        if (page == null) { EndStroke(true); return; }
        var rt = page.GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, sp, cam, out Vector2 local)) return;

        if (eraser) { EraseAt(page, rt, local); return; }

        // PEN — jonli mesh
        if (activeInk == null || inkPage != page)
        {
            EndStroke(true); // oldingi chiziqni yakunlaymiz
            StartInk(page, rt);
        }
        activeInk.color = brushColor;
        activeInk.AddPoint(local);
    }

    void StartInk(GameObject page, RectTransform rt)
    {
        inkPage = page; inkRect = rt;
        inkLayer = GetLayer(page, rt);
        inkWidth = Mathf.Max(6f, rt.rect.width * 0.018f);

        var go = new GameObject("InkLive", typeof(RectTransform), typeof(CanvasRenderer), typeof(InkLine));
        go.layer = page.layer;
        var crt = go.GetComponent<RectTransform>();
        crt.SetParent(rt, false);
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        crt.pivot = new Vector2(0.5f, 0.5f);
        activeInk = go.GetComponent<InkLine>();
        activeInk.width = inkWidth;
        activeInk.raycastTarget = false;
    }

    void EndStroke(bool commit)
    {
        // o'chirg'ich yakuni
        if (eraser) { if (commit && ePage != null) PersistPage(ePage); eLastValid = false; ePage = null; }

        if (activeInk == null) return;
        if (commit && activeInk.points.Count > 0) BakeInk();
        Destroy(activeInk.gameObject);
        activeInk = null; inkPage = null; inkLayer = null; inkRect = null;
    }

    void BakeInk()
    {
        var rect = inkRect.rect;
        float texW = inkLayer.w, texH = inkLayer.h;
        int r = Mathf.Max(2, Mathf.RoundToInt(activeInk.width / rect.width * texW * 0.5f));
        Color32 c = (Color32)activeInk.color;
        bool have = false; int lpx = 0, lpy = 0;
        foreach (var lp in activeInk.points)
        {
            float u = (lp.x - rect.x) / rect.width, v = (lp.y - rect.y) / rect.height;
            int px = Mathf.RoundToInt(u * (texW - 1)), py = Mathf.RoundToInt(v * (texH - 1));
            if (have) Stroke(inkLayer, lpx, lpy, px, py, r, c); else Dot(inkLayer, px, py, r, c);
            lpx = px; lpy = py; have = true;
        }
        inkLayer.tex.SetPixels32(inkLayer.buf); inkLayer.tex.Apply(false); inkLayer.dirty = false;
        PersistPage(inkPage);
    }

    void EraseAt(GameObject page, RectTransform rt, Vector2 local)
    {
        var layer = GetLayer(page, rt);
        var rect = rt.rect;
        int px = Mathf.RoundToInt((local.x - rect.x) / rect.width * (layer.w - 1));
        int py = Mathf.RoundToInt((local.y - rect.y) / rect.height * (layer.h - 1));
        int r = Mathf.Max(8, Mathf.RoundToInt(rt.rect.width * 0.02f / rect.width * layer.w));
        var clear = new Color32(0, 0, 0, 0);
        if (page != ePage) eLastValid = false;
        if (eLastValid) Stroke(layer, eLastX, eLastY, px, py, r, clear); else Dot(layer, px, py, r, clear);
        layer.dirty = true; eLastValid = true; ePage = page; eLastX = px; eLastY = py;
    }

    // ---- rasterizatsiya (bake / erase) ----
    void Dot(Layer l, int cx, int cy, int r, Color32 c)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y > r * r) continue;
                int px = cx + x, py = cy + y;
                if (px < 0 || py < 0 || px >= l.w || py >= l.h) continue;
                l.buf[py * l.w + px] = c;
            }
    }
    void Stroke(Layer l, int x0, int y0, int x1, int y1, int r, Color32 c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Dot(l, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), r, c);
        }
    }

    Layer GetLayer(GameObject page, RectTransform rt)
    {
        if (layers.TryGetValue(page, out var l)) return l;
        int w = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(rt.rect.width)), 64, 640);
        int h = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(rt.rect.height)), 64, 860);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] buf;
        string path = SavePath(page);
        if (File.Exists(path) && tex.LoadImage(File.ReadAllBytes(path)))
        {
            w = tex.width; h = tex.height; buf = tex.GetPixels32();
        }
        else { buf = new Color32[w * h]; tex.SetPixels32(buf); tex.Apply(); }

        var go = new GameObject("PaintLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = page.layer;
        var crt = go.GetComponent<RectTransform>();
        crt.SetParent(rt, false);
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

        l = new Layer { tex = tex, buf = buf, w = w, h = h };
        layers[page] = l;
        return l;
    }

    void ClearCurrent()
    {
        EndStroke(false);
        if (book.papers == null) return;
        int cp = book.CurrentPaper;
        var pages = new List<GameObject>();
        if (cp >= 0 && cp < book.papers.Length) pages.Add(book.papers[cp].Front);
        if (cp - 1 >= 0 && cp - 1 < book.papers.Length) pages.Add(book.papers[cp - 1].Back);
        foreach (var p in pages)
            if (p != null && layers.TryGetValue(p, out var l))
            {
                for (int i = 0; i < l.buf.Length; i++) l.buf[i] = new Color32(0, 0, 0, 0);
                l.tex.SetPixels32(l.buf); l.tex.Apply(false); l.dirty = false;
                PersistPage(p);
            }
    }

    // ---- saqlash / yuklash ----
    void SaveLayer(GameObject page)
    {
        if (page == null || !layers.TryGetValue(page, out var l)) return;
        try
        {
            Directory.CreateDirectory(SaveDir);
            File.WriteAllBytes(SavePath(page), l.tex.EncodeToPNG());
            SyncFS(); // IndexedDB ga yozamiz -> qayta ochilganda saqlanib qoladi
        }
        catch (System.Exception e) { Debug.LogWarning("[BookDrawing] saqlash xatosi: " + e.Message); }
    }

    void SaveAll() { foreach (var kv in layers) SaveLayer(kv.Key); }
    void OnApplicationQuit() { SaveAll(); }
    void OnApplicationPause(bool pause) { if (pause) SaveAll(); }

    void LoadSavedDrawings()
    {
        if (book == null || book.papers == null) return;
        foreach (var p in book.papers)
        {
            if (p == null) continue;
            foreach (var page in new[] { p.Front, p.Back })
            {
                if (page == null) continue;
                if (!File.Exists(SavePath(page))) continue;
                var rt = page.GetComponent<RectTransform>();
                if (rt != null) GetLayer(page, rt);
            }
        }
    }

    void SetDrawMode(bool on)
    {
        drawMode = on;
        if (book != null) book.interactable = !on;
        if (toggleLabel != null) toggleLabel.text = on ? "VARAQLASHGA" : "CHIZISH";
        foreach (var b in toolButtons) if (b != null) b.SetActive(on);
        if (!on) { EndStroke(true); SaveAll(); }
    }

    // ---------- Toolbar ----------
    void BuildToolbar()
    {
        var canvasGO = new GameObject("DrawUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = canvasGO.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        Transform parent = canvasGO.transform;

        var bar = NewUI("DrawToolbar", parent);
        toolbarRt = bar.GetComponent<RectTransform>();
        toolbarRt.anchorMin = new Vector2(0.5f, 1f);
        toolbarRt.anchorMax = new Vector2(0.5f, 1f);
        toolbarRt.pivot = new Vector2(0.5f, 1f);
        toolbarRt.sizeDelta = new Vector2(900, 96);
        toolbarRt.anchoredPosition = new Vector2(0, -16);
        var hl = bar.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.padding = new RectOffset(16, 16, 10, 10);
        var bg = bar.AddComponent<Image>(); bg.color = new Color(1, 1, 1, 0.94f); bg.raycastTarget = true;

        var toggle = MakeBtn("CHIZISH", 180, 74, new Color(0.36f, 0.42f, 0.96f), () => SetDrawMode(!drawMode));
        toggle.transform.SetParent(bar.transform, false);
        toggleLabel = toggle.GetComponentInChildren<TMP_Text>();

        Color[] cols = {
            new Color(0.90f,0.25f,0.25f), new Color(0.20f,0.55f,0.95f), new Color(0.20f,0.70f,0.35f),
            new Color(0.98f,0.80f,0.20f), new Color(0.15f,0.15f,0.18f)
        };
        foreach (var col in cols)
        {
            var cc = col;
            var sw = MakeBtn("", 66, 74, cc, () => { eraser = false; brushColor = cc; });
            sw.transform.SetParent(bar.transform, false);
            toolButtons.Add(sw);
        }
        var er = MakeBtn("O‘CH", 92, 74, new Color(0.85f, 0.85f, 0.88f), () => { eraser = true; });
        er.transform.SetParent(bar.transform, false); toolButtons.Add(er);
        var clr = MakeBtn("TOZA", 100, 74, new Color(0.95f, 0.45f, 0.45f), ClearCurrent);
        clr.transform.SetParent(bar.transform, false); toolButtons.Add(clr);
    }

    GameObject MakeBtn(string label, float w, float h, Color col, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>(); img.color = col;
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;
        var btn = go.GetComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(onClick);
        if (!string.IsNullOrEmpty(label))
        {
            var t = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(go.transform, false);
            var rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            t.font = font; t.text = label; t.fontSize = 28; t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold; t.raycastTarget = false;
        }
        return go;
    }

    GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }
}

/// <summary>Jonli chiziq — nuqtalardan qalin polilin mesh quradi (teksturasiz, GPU'ga arzon).</summary>
public class InkLine : MaskableGraphic
{
    public readonly List<Vector2> points = new List<Vector2>();
    public float width = 12f;

    public void AddPoint(Vector2 p)
    {
        if (points.Count > 0 && (points[points.Count - 1] - p).sqrMagnitude < 1f) return;
        points.Add(p); SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count == 0) return;
        float hw = width * 0.5f;
        if (points.Count == 1) { Cap(vh, points[0], hw); return; }
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i], b = points[i + 1];
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) continue;
            dir.Normalize();
            Vector2 n = new Vector2(-dir.y, dir.x) * hw;
            Quad(vh, a - n, a + n, b + n, b - n);
            Cap(vh, b, hw); // yumaloqroq ulanish
        }
    }

    void Cap(VertexHelper vh, Vector2 c, float hw)
    {
        Quad(vh, c + new Vector2(-hw, -hw), c + new Vector2(-hw, hw), c + new Vector2(hw, hw), c + new Vector2(hw, -hw));
    }

    void Quad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        int idx = vh.currentVertCount;
        vh.AddVert(p0, color, new Vector2(0, 0));
        vh.AddVert(p1, color, new Vector2(0, 1));
        vh.AddVert(p2, color, new Vector2(1, 1));
        vh.AddVert(p3, color, new Vector2(1, 0));
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}
