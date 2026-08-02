using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Har sahifaga MOS chiroyli mini-o'yin. UI to'liq kod bilan (yumaloq karta, soya, quvnoq ranglar).
/// To'g'ri javobda bayram animatsiyasi (konfetti + sakrash).
/// Turlar: welcome, count, numrec, match, count_color, find.
/// Ma'lumot: Assets/Resources/book_games.json. BookPro logikasiga TEGMAYDI.
/// </summary>
public class BookGameController : MonoBehaviour
{
    [Serializable]
    public class GameEntry
    {
        public int page;
        public string type;
        public int target;
        public List<int> numbers;
        public List<int> targets;
        public string prompt;
    }
    [Serializable] public class GameData { public List<GameEntry> games; }

    static BookGameController _instance;
    public static BookGameController Instance => _instance;

    Dictionary<int, GameEntry> byPage = new Dictionary<int, GameEntry>();
    TMP_FontAsset font;
    BookPro book;

    GameObject root;
    RectTransform cardRt;
    Transform contentArea;
    Transform actionRow;
    TMP_Text titleText;
    TMP_Text promptText;
    TMP_Text feedbackText;
    GameEntry current;
    Sprite currentObj;
    Dictionary<int, Sprite> objCache = new Dictionary<int, Sprite>();

    // ---- Rang palitrasi (quvnoq, bolalar uchun) ----
    readonly Color overlayCol = new Color(0.10f, 0.12f, 0.25f, 0.72f);
    readonly Color cardCol    = new Color(1f, 1f, 1f);
    readonly Color headerCol  = new Color(0.36f, 0.42f, 0.96f);   // binafsha-ko'k
    readonly Color accent     = new Color(0.20f, 0.60f, 0.98f);   // ko'k
    readonly Color okCol      = new Color(0.18f, 0.78f, 0.42f);   // yashil
    readonly Color badCol     = new Color(0.97f, 0.42f, 0.42f);   // marjon
    readonly Color inkCol     = new Color(0.18f, 0.20f, 0.30f);
    static readonly Color[] palette = {
        new Color(0.96f,0.40f,0.42f), new Color(0.99f,0.73f,0.24f), new Color(0.30f,0.78f,0.48f),
        new Color(0.28f,0.62f,0.98f), new Color(0.66f,0.44f,0.94f), new Color(0.98f,0.55f,0.78f),
        new Color(0.30f,0.80f,0.80f), new Color(0.98f,0.56f,0.30f), new Color(0.55f,0.78f,0.32f),
        new Color(0.52f,0.56f,0.98f)
    };

    void Awake()
    {
        _instance = this;
        font = TMP_Settings.defaultFontAsset;
        book = UnityEngine.Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        LoadData();
        BuildUI();
        // Asosiy rejim endi "kitob ustida chizish" (BookDrawing). Mini-o'yin tugmasi o'chirilgan.
        // CreateLauncher();
        Hide();
    }

    void OnDestroy() { if (_instance == this) _instance = null; }

    void LoadData()
    {
        var ta = Resources.Load<TextAsset>("book_games");
        if (ta == null) { Debug.LogWarning("[BookGame] Resources/book_games.json topilmadi"); return; }
        var data = JsonUtility.FromJson<GameData>(ta.text);
        if (data?.games == null) return;
        foreach (var g in data.games) byPage[g.page] = g;
    }

    // ---------- Ochish / dispatch ----------
    public void OpenCurrent()
    {
        int page = 0;
        if (book != null) page = Mathf.Clamp(book.CurrentPaper, 0, Mathf.Max(0, book.papers.Length - 1));
        OpenForPage(page);
    }

    public void OpenForPage(int page)
    {
        byPage.TryGetValue(page, out current);
        currentObj = LoadObj(page);
        ClearContent();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        StopAllCoroutines();
        StartCoroutine(PopIn(cardRt));

        string title = "O‘YIN";
        if (current == null) { titleText.text = "O‘YIN"; ShowMessage("Bu betda o‘yin yo‘q 🙂"); return; }

        switch (current.type)
        {
            case "welcome":     title = "SALOM";        SetupWelcome(); break;
            case "count":       title = "SANA VA TOP";  SetupCount(current.target); break;
            case "numrec":      title = "NECHTA?";      SetupNumrec(current.numbers); break;
            case "count_color": title = "SANA VA BO‘YA"; SetupCountColor(current.target); break;
            case "match":       title = "JUFTLA";       SetupMatch(); break;
            case "find":        title = "TOP";          SetupFind(current.targets); break;
            default:            SetupCount(current.target > 0 ? current.target : 3); break;
        }
        titleText.text = title;
    }

    public void Hide() { if (root != null) { StopAllCoroutines(); root.SetActive(false); } }

    void ClearContent()
    {
        foreach (Transform c in contentArea) Destroy(c.gameObject);
        foreach (Transform c in actionRow) Destroy(c.gameObject);
        if (feedbackText != null) feedbackText.text = "";
    }

    void SetFeedback(string msg, Color col) { feedbackText.text = msg; feedbackText.color = col; StartCoroutine(PopIn(feedbackText.rectTransform, 0.25f)); }
    void Wrong(string msg) { SetFeedback(msg, badCol); StartCoroutine(Shake(cardRt)); }
    void Win()
    {
        SetFeedback("Barakalla! 🎉", okCol);
        StartCoroutine(Celebrate());
    }

    // ================= O'yin turlari =================

    void SetupWelcome()
    {
        promptText.text = "QUVNOQ MATEMATIKA";
        var t = MakeText("msg", contentArea, 30, inkCol);
        t.text = "Xush kelibsiz!\nKitobni varaqlab, har betda\no‘yin o‘ynaymiz. 🎉";
        SetH(t.gameObject, 160);
        var b = MakeButton("BOSHLASH", 280, 96, okCol, Color.white, 36);
        b.transform.SetParent(actionRow, false);
        b.GetComponent<Button>().onClick.AddListener(Hide);
    }

    void SetupCount(int target)
    {
        if (target < 1) target = 1;
        promptText.text = "Shakllarni sana, to‘g‘ri raqamni bos!";
        AddCircleGrid(contentArea, target, palette[target % palette.Length], 240);
        foreach (int v in OptionsAround(target, 4))
            AddNumberOption(v, () => { if (v == target) Win(); else Wrong("Yana bir bor sanab ko‘r!"); });
    }

    void SetupNumrec(List<int> numbers)
    {
        var nums = (numbers != null && numbers.Count >= 2) ? numbers.Distinct().OrderBy(x => x).ToList()
                                                           : new List<int> { 1, 2, 3 };
        int shown = nums[UnityEngine.Random.Range(0, nums.Count)];
        promptText.text = "Nechta? Sanab, to‘g‘ri raqamni bos!";
        AddCircleGrid(contentArea, shown, palette[shown % palette.Length], 240);
        foreach (int v in nums)
            AddNumberOption(v, () => { if (v == shown) Win(); else Wrong("Yana bir bor sanab ko‘r!"); });
    }

    void SetupCountColor(int target)
    {
        if (target < 1) target = 1;
        promptText.text = $"Sanab, {target} ta katakni bo‘ya, keyin TEKSHIR!";

        Color empty = new Color(0.90f, 0.93f, 0.99f);
        int boxes = Mathf.Max(target, 10);
        int colored = 0;

        var boxRow = NewUI("Boxes", contentArea);
        var g = boxRow.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(96, 96); g.spacing = new Vector2(14, 14);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 5;
        g.childAlignment = TextAnchor.MiddleCenter;
        SetH(boxRow, 220);

        for (int i = 0; i < boxes; i++)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(boxRow.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = RoundSprite(); img.type = Image.Type.Sliced;
            img.color = empty; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
            var rt = go.GetComponent<RectTransform>();
            bool on = false;
            btn.onClick.AddListener(() =>
            {
                on = !on; img.color = on ? accent : empty;
                colored += on ? 1 : -1;
                StartCoroutine(PopIn(rt, 0.15f));
            });
        }

        var check = MakeButton("TEKSHIR", 280, 100, okCol, Color.white, 38);
        check.transform.SetParent(actionRow, false);
        check.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (colored == target) Win();
            else Wrong($"{target} ta bo‘lsin. Hozir {colored} ta.");
        });
    }

    void SetupMatch()
    {
        promptText.text = "Bir xil sonlarni juftla! (chapdan tanla → o‘ngga bos)";
        int k = 3;
        var counts = Enumerable.Range(1, 5).OrderBy(_ => UnityEngine.Random.value).Take(k).ToList();
        var rightVals = counts.OrderBy(_ => UnityEngine.Random.value).ToList();

        var rowGO = NewUI("MatchRow", contentArea);
        var hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 90; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false; hl.childControlHeight = false;
        SetH(rowGO, 300);

        var leftCol = ColumnUI(rowGO.transform);
        var rightCol = ColumnUI(rowGO.transform);

        int matched = 0;
        var leftBtns = new List<(Button btn, int count, Image img)>();
        var rightBtns = new List<(Button btn, int val, Image img)>();

        foreach (int c in counts)
        {
            var tile = MakeTile(leftCol, 170, 74);
            AddCircleRow(tile.go.transform, c);
            leftBtns.Add((tile.btn, c, tile.img));
        }
        foreach (int v in rightVals)
        {
            var tile = MakeTile(rightCol, 120, 74);
            var t = MakeText("n", tile.go.transform, 42, inkCol); t.text = v.ToString(); Stretch(t.rectTransform);
            rightBtns.Add((tile.btn, v, tile.img));
        }

        int selLeft = -1;
        for (int li = 0; li < leftBtns.Count; li++)
        {
            int idx = li;
            leftBtns[li].btn.onClick.AddListener(() =>
            {
                selLeft = idx;
                for (int j = 0; j < leftBtns.Count; j++)
                    if (leftBtns[j].btn.interactable)
                        leftBtns[j].img.color = (j == idx) ? new Color(0.80f,0.90f,1f) : Color.white;
                StartCoroutine(PopIn(leftBtns[idx].img.rectTransform, 0.15f));
            });
        }
        for (int ri = 0; ri < rightBtns.Count; ri++)
        {
            int idx = ri;
            rightBtns[idx].btn.onClick.AddListener(() =>
            {
                if (selLeft < 0) { SetFeedback("Avval chapdan tanla.", accent); return; }
                if (leftBtns[selLeft].count == rightBtns[idx].val)
                {
                    leftBtns[selLeft].img.color = okCol; rightBtns[idx].img.color = okCol;
                    leftBtns[selLeft].btn.interactable = false; rightBtns[idx].btn.interactable = false;
                    StartCoroutine(PopIn(rightBtns[idx].img.rectTransform, 0.2f));
                    matched++; selLeft = -1;
                    if (matched == counts.Count) Win();
                }
                else Wrong("Bu mos emas, yana ko‘r.");
            });
        }
    }

    void SetupFind(List<int> targets)
    {
        var tg = (targets != null && targets.Count > 0) ? targets : new List<int> { 2, 3 };
        promptText.text = string.Join(" va ", tg) + " ta bo‘lgan guruhni top!";
        var groupCounts = new List<int>(tg);
        var pool = Enumerable.Range(1, 6).Where(x => !tg.Contains(x)).OrderBy(_ => UnityEngine.Random.value).ToList();
        for (int i = 0; i < 4 && i < pool.Count; i++) groupCounts.Add(pool[i]);
        groupCounts = groupCounts.OrderBy(_ => UnityEngine.Random.value).ToList();

        int remaining = tg.Count;
        var grid = NewUI("Groups", contentArea);
        var gg = grid.AddComponent<GridLayoutGroup>();
        gg.cellSize = new Vector2(155, 115); gg.spacing = new Vector2(16, 16);
        gg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gg.constraintCount = 3;
        gg.childAlignment = TextAnchor.MiddleCenter;
        SetH(grid, 270);

        foreach (int c in groupCounts)
        {
            var tile = MakeTile(grid.transform, 155, 115);
            AddCircleRow(tile.go.transform, c);
            int cc = c;
            tile.btn.onClick.AddListener(() =>
            {
                if (tg.Contains(cc))
                {
                    tile.img.color = okCol; tile.btn.interactable = false;
                    StartCoroutine(PopIn(tile.img.rectTransform, 0.2f));
                    remaining--; if (remaining <= 0) Win();
                }
                else Wrong("Bu emas!");
            });
        }
    }

    // ================= Animatsiyalar =================

    IEnumerator PopIn(RectTransform rt, float dur = 0.35f)
    {
        if (rt == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float s = EaseOutBack(p);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator Shake(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector2 basePos = rt.anchoredPosition;
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Sin(t * 50f) * 16f * (1f - t / dur);
            rt.anchoredPosition = basePos + new Vector2(x, 0);
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    IEnumerator Celebrate()
    {
        // konfetti — root ustida, karta tepasidan tushadi
        int n = 28;
        var pieces = new List<RectTransform>();
        var vels = new List<Vector2>();
        var spins = new List<float>();
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("confetti", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(UnityEngine.Random.Range(14, 24), UnityEngine.Random.Range(14, 24));
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-180f, 180f), 300f);
            var img = go.GetComponent<Image>();
            img.sprite = RoundSprite(); img.type = Image.Type.Sliced;
            img.color = palette[UnityEngine.Random.Range(0, palette.Length)];
            pieces.Add(rt);
            vels.Add(new Vector2(UnityEngine.Random.Range(-160f, 160f), UnityEngine.Random.Range(60f, 260f)));
            spins.Add(UnityEngine.Random.Range(-360f, 360f));
        }
        float t = 0f, dur = 1.5f;
        while (t < dur)
        {
            float dt = Time.unscaledDeltaTime; t += dt;
            for (int i = 0; i < pieces.Count; i++)
            {
                var v = vels[i]; v.y -= 900f * dt; vels[i] = v;
                pieces[i].anchoredPosition += v * dt;
                pieces[i].Rotate(0, 0, spins[i] * dt);
                if (t > dur * 0.6f)
                {
                    var c = pieces[i].GetComponent<Image>().color;
                    c.a = Mathf.Clamp01(1f - (t - dur * 0.6f) / (dur * 0.4f));
                    pieces[i].GetComponent<Image>().color = c;
                }
            }
            yield return null;
        }
        foreach (var p in pieces) if (p != null) Destroy(p.gameObject);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = 2.70158f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ================= UI yordamchilari =================

    void ShowMessage(string msg)
    {
        promptText.text = "";
        var t = MakeText("msg", contentArea, 30, inkCol);
        t.text = msg; SetH(t.gameObject, 140);
        var b = MakeButton("YOPISH", 220, 88, accent, Color.white, 32);
        b.transform.SetParent(actionRow, false);
        b.GetComponent<Button>().onClick.AddListener(Hide);
    }

    List<int> OptionsAround(int target, int count)
    {
        var set = new HashSet<int> { target };
        int guard = 0;
        while (set.Count < count && guard++ < 60)
            set.Add(Mathf.Clamp(target + UnityEngine.Random.Range(-3, 4), 1, 10));
        return set.OrderBy(_ => UnityEngine.Random.value).ToList();
    }

    void AddNumberOption(int val, UnityEngine.Events.UnityAction onClick)
    {
        var b = MakeButton(val.ToString(), 132, 132, accent, Color.white, 64);
        b.transform.SetParent(actionRow, false);
        b.GetComponent<Button>().onClick.AddListener(onClick);
    }

    void AddCircleGrid(Transform parent, int n, Color col, float height)
    {
        var go = NewUI("Grid", parent);
        var grid = go.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110, 110); grid.spacing = new Vector2(16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.MiddleCenter;
        SetH(go, height);
        for (int i = 0; i < n; i++) AddCircle(go.transform, col, 110);
    }

    void AddCircleRow(Transform parent, int n)
    {
        var go = NewUI("Row", parent);
        Stretch(go.GetComponent<RectTransform>());
        var grid = go.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(32, 32); grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;
        for (int i = 0; i < n; i++) AddCircle(go.transform, palette[n % palette.Length], 32);
    }

    void AddCircle(Transform parent, Color col, float size)
    {
        var go = new GameObject("c", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        if (currentObj != null) { img.sprite = currentObj; img.color = Color.white; img.preserveAspect = true; }
        else { img.color = col; img.sprite = CircleSprite(); }
    }

    Sprite LoadObj(int page)
    {
        if (objCache.TryGetValue(page, out var s)) return s;
        var tex = Resources.Load<Texture2D>("GameSprites/obj_" + page.ToString("D3"));
        Sprite sp = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
        objCache[page] = sp;
        return sp;
    }

    Transform ColumnUI(Transform parent)
    {
        var go = NewUI("Col", parent);
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = 22; v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = false; v.childControlHeight = false;
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 200;
        return go.transform;
    }

    (GameObject go, Image img, Button btn) MakeTile(Transform parent, float w, float h)
    {
        var go = MakePanel(parent, w, h, Color.white);
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;
        var img = go.GetComponent<Image>();
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None; // rang qo'lda boshqariladi (juftlash/topish)
        return (go, img, btn);
    }

    // yumaloq panel + soya
    GameObject MakePanel(Transform parent, float w, float h, Color col)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.sprite = RoundSprite(); img.type = Image.Type.Sliced; img.color = col;
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0,0,0,0.16f); sh.effectDistance = new Vector2(0,-3);
        return go;
    }

    // ---- generatsiya qilinadigan sprite'lar ----
    static Sprite _circle, _round;
    static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        int s = 64; float r = s / 2f - 1;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s]; Vector2 c = new Vector2(s / 2f, s / 2f);
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), c);
            byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
            px[y * s + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px); tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return _circle;
    }

    static Sprite RoundSprite()
    {
        if (_round != null) return _round;
        int s = 48, r = 16;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float cx = Mathf.Clamp(x, r, s - 1 - r);
            float cy = Mathf.Clamp(y, r, s - 1 - r);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
            px[y * s + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px); tex.Apply();
        _round = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return _round;
    }

    // ---------- Karkas ----------
    void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault();
        Transform parent = canvas != null ? canvas.transform : transform;

        root = NewUI("GamePanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        var bg = root.AddComponent<Image>(); bg.color = overlayCol; bg.raycastTarget = true;

        var card = MakePanel(root.transform, 880, 800, cardCol);
        card.name = "Card";
        cardRt = card.GetComponent<RectTransform>();
        // Ekranga nisbatan katta, moslashuvchan karta
        cardRt.anchorMin = new Vector2(0.08f, 0.06f);
        cardRt.anchorMax = new Vector2(0.92f, 0.94f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.offsetMin = Vector2.zero; cardRt.offsetMax = Vector2.zero;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(34, 34, 26, 30); vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;   // preferredHeight ishlashi uchun
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Sarlavha (rangli badge)
        var header = MakePanel(card.transform, 300, 62, headerCol);
        SetH(header, 62);
        titleText = MakeText("Title", header.transform, 32, Color.white);
        titleText.fontStyle = FontStyles.Bold; Stretch(titleText.rectTransform);

        promptText = MakeText("Prompt", card.transform, 27, inkCol);
        SetH(promptText.gameObject, 60);

        var content = NewUI("Content", card.transform);
        var cvl = content.AddComponent<VerticalLayoutGroup>();
        cvl.spacing = 14; cvl.childAlignment = TextAnchor.MiddleCenter;
        cvl.childControlWidth = true; cvl.childControlHeight = true;   // preferredHeight ishlashi uchun
        cvl.childForceExpandWidth = false; cvl.childForceExpandHeight = false;
        SetH(content, 400); contentArea = content.transform;

        feedbackText = MakeText("Feedback", card.transform, 30, okCol);
        feedbackText.fontStyle = FontStyles.Bold; SetH(feedbackText.gameObject, 42);

        var act = NewUI("Actions", card.transform);
        var hl = act.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        SetH(act, 140); actionRow = act.transform;

        var close = MakeButton("✕", 60, 60, badCol, Color.white, 30);
        close.GetComponent<LayoutElement>().ignoreLayout = true;
        var crt = close.GetComponent<RectTransform>();
        crt.SetParent(card.transform, false);
        crt.anchorMin = crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(1, 1);
        crt.anchoredPosition = new Vector2(-10, -10);
        close.GetComponent<Button>().onClick.AddListener(Hide);
    }

    void CreateLauncher()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault();
        Transform parent = canvas != null ? canvas.transform : transform;

        var btn = MakeButton("O‘YIN", 190, 92, accent, Color.white, 34);
        btn.name = "GameLauncher";
        var rt = btn.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 100f);
        btn.GetComponent<LayoutElement>().ignoreLayout = true;
        btn.GetComponent<Button>().onClick.AddListener(OpenCurrent);
    }

    GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    TMP_Text MakeText(string name, Transform parent, float size, Color color)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false; // matn tugma bosishini to'smasin
        return t;
    }

    GameObject MakeButton(string label, float w, float h, Color bg, Color fg, float fontSize)
    {
        var go = MakePanel(null, w, h, bg);
        go.name = "Btn";
        var btn = go.AddComponent<Button>(); btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors; colors.pressedColor = new Color(bg.r*0.85f, bg.g*0.85f, bg.b*0.85f); colors.fadeDuration = 0.06f; btn.colors = colors;
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;
        var txtGO = NewUI("Label", go.transform);
        Stretch(txtGO.GetComponent<RectTransform>());
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = label; t.fontSize = fontSize; t.color = fg;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        return go;
    }

    void SetH(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }
}
