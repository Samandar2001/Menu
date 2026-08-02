using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ================== UMUMIY MEXANIZM ==================
// Har mavzu shu dvigatelда ishlaydi. Yangi o'yin qo'shish = kichik TaskProvider yozish.

/// <summary>Bir raundдаги narsa (sudraladigan). accept=true bo'lsa og'izga berilса to'g'ri.</summary>
public class TaskItem
{
    public Sprite sprite;
    public Color color = Color.white;
    public bool accept;
    public bool preserveAspect = true;
}

/// <summary>Bir raund: maqsad (rasm yoki N nuqta + ovoz), narsalar, nechta to'g'ri kerak.</summary>
public class Task
{
    public Sprite goalIcon;             // shakl kabi — bitta rasm ko'rsatiladi
    public Color goalColor = Color.white;
    public int goalDots;                // sanash kabi — shuncha nuqta ko'rsatiladi
    public string voice = "";           // aytiladigan gap (audio kaliti)
    public List<TaskItem> items = new List<TaskItem>();
    public int need = 1;                // nechta to'g'ri berish kerak
}

/// <summary>Har mavzu shu interfeysни beradi. Menu ikonkasi + keyingi raund.</summary>
public interface ITaskProvider
{
    Sprite MenuIcon(AntGameController g);
    Color MenuColor { get; }
    Task Next(AntGameController g);
}

public class AntGameController : MonoBehaviour
{
    TMP_FontAsset font;
    RectTransform antRoot;
    Image mouthOpen, mouthClose, eyelids;
    TMP_Text counterText, feedbackText, bubbleText;
    Transform itemArea, goalRow, confettiParent, canvasT, menuBar;

    bool busy, animating;
    Vector2 antBasePos;

    // ---- dvigatel holati ----
    readonly List<ITaskProvider> providers = new List<ITaskProvider>();
    ITaskProvider provider;
    Task task;
    int progress;

    // Sozlamalar + o'sish (chumoli g'alaba bilan kichikdan kattalashadi)
    bool sound = true;
    float growth;   // 0..1
    float GrowScalar => Mathf.Lerp(0.55f, 1.2f, Mathf.Clamp01(growth));
    public int MaxNumber => growth < 0.34f ? 3 : (growth < 0.67f ? 5 : 10);
    public int ShapeCount => growth < 0.34f ? 3 : 4;

    // Ekranlar
    GameObject mainScreen, gamesScreen, settingsScreen, homeBtn;
    Image soundImg;

    readonly Color okCol = new Color(0.18f, 0.78f, 0.42f);
    public Color ShapeColor(int k) { return shapeCols[k % shapeCols.Length]; }
    static readonly Color[] shapeCols = {
        new Color(0.95f,0.35f,0.35f), new Color(0.30f,0.62f,0.98f),
        new Color(0.30f,0.78f,0.48f), new Color(0.70f,0.45f,0.94f)
    };

    void Awake()
    {
        font = TMP_Settings.defaultFontAsset;
        sound = PlayerPrefs.GetInt("antSound", 1) == 1;
        growth = PlayerPrefs.GetFloat("antGrowth", 0f);
        providers.Add(new CountProvider());
        providers.Add(new ShapeProvider());
        BuildCanvasAndUI();
        BuildAnt();
        BuildScreens();
        EnsureEventSystem();
    }

    IEnumerator Start()
    {
        yield return null;
        StartCoroutine(Breathe());
        StartCoroutine(BlinkLoop());
        ShowScreen(mainScreen);   // avval bosh menyu
    }

    // ================= DVIGATEL =================
    void SetProvider(ITaskProvider p)
    {
        provider = p;
        busy = false;
        ShowScreen(null);   // menyuni yopamiz, o'yin ko'rinadi
        NewTask();
    }

    void NewTask()
    {
        busy = false;
        progress = 0;
        foreach (Transform c in itemArea) Destroy(c.gameObject);
        SetFeedback("");
        task = provider.Next(this);
        ShowGoal(task);
        UpdateCounter();
        Say(task.voice);
        SpawnItems(task.items);
    }

    void ShowGoal(Task t)
    {
        foreach (Transform c in goalRow) Destroy(c.gameObject);
        if (t.goalIcon != null)
        {
            var go = NewImg("g", goalRow, 120, 120);
            go.sprite = t.goalIcon; go.color = t.goalColor; go.preserveAspect = true;
        }
        else
        {
            for (int i = 0; i < t.goalDots; i++)
            {
                var go = NewImg("d", goalRow, 60, 60);
                go.sprite = FoodSprite(); go.color = new Color(0.95f, 0.5f, 0.3f);
            }
        }
    }

    void SpawnItems(List<TaskItem> items)
    {
        int n = items.Count;
        for (int i = 0; i < n; i++)
        {
            var it = items[i];
            var go = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FoodDrag));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(itemArea, false);
            rt.sizeDelta = new Vector2(160, 160);
            float span = 760f;
            float x = n > 1 ? -span / 2 + span * i / (n - 1) : 0;
            rt.anchoredPosition = new Vector2(x, Random.Range(-40f, 40f));
            var img = go.GetComponent<Image>();
            img.sprite = it.sprite; img.color = it.color; img.preserveAspect = it.preserveAspect; img.raycastTarget = true;
            var fd = go.GetComponent<FoodDrag>();
            fd.mouth = mouthClose.transform; fd.eatRadius = Screen.height * 0.16f;
            fd.accept = it.accept; fd.onDropped = OnDrop;
            StartCoroutine(PopIn(rt));
        }
    }

    bool OnDrop(FoodDrag fd)
    {
        if (busy) return false;
        if (!fd.accept) { SetFeedback("Yo‘q, qayta!"); return false; }
        progress++;
        UpdateCounter();
        Say(NumberWord(progress));
        StartCoroutine(Eat());
        if (progress >= task.need) StartCoroutine(Celebrate());
        return true;
    }

    IEnumerator Celebrate()
    {
        busy = true;
        SetFeedback("Barakalla!"); Say("Barakalla!");
        StartCoroutine(Talk(1.2f));
        StartCoroutine(Confetti());
        for (int i = 0; i < 2; i++) yield return Bounce(1.25f, 0.35f);
        // chumoli biroz o'sadi (g'alaba mukofoti) — saqlanadi
        growth = Mathf.Min(1f, growth + 0.04f);
        PlayerPrefs.SetFloat("antGrowth", growth); PlayerPrefs.Save();
        yield return new WaitForSeconds(0.4f);
        NewTask();
    }

    // ================= CHUMOLI (personaj) =================
    Sprite LoadSprite(string name)
    {
        var tex = Resources.Load<Texture2D>("AntGame/" + name);
        return tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
    }

    void BuildAnt()
    {
        var root = NewUI("Ant", canvasT);
        antRoot = root.GetComponent<RectTransform>();
        antRoot.anchorMin = antRoot.anchorMax = new Vector2(0.5f, 0.5f);
        antRoot.pivot = new Vector2(0.5f, 0.5f);
        float h = 1000f, w = h * 484f / 962f;
        antRoot.sizeDelta = new Vector2(w, h);
        antRoot.anchoredPosition = new Vector2(0, 120);
        antBasePos = antRoot.anchoredPosition;

        AddLayer(antRoot, "base", LoadSprite("ant_base"), true);
        mouthClose = AddLayer(antRoot, "mouthClose", LoadSprite("ant_mouth_close"), false);
        mouthOpen = AddLayer(antRoot, "mouthOpen", LoadSprite("ant_mouth_open"), false);
        eyelids = AddLayer(antRoot, "eyelids", LoadSprite("ant_eyelids"), false);
        mouthOpen.gameObject.SetActive(false);
        eyelids.gameObject.SetActive(false);

        var btn = root.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => { if (!busy) { StartCoroutine(Bounce(1.12f, 0.25f)); StartCoroutine(Talk(0.5f)); } });
    }

    Image AddLayer(Transform parent, string name, Sprite sp, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.sprite = sp; img.raycastTarget = raycast; img.preserveAspect = true;
        return img;
    }

    IEnumerator Breathe()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float gs = GrowScalar;
            float s = 1f + Mathf.Sin(t * 2.2f) * 0.02f;
            if (antRoot != null && !busy && !animating) antRoot.localScale = new Vector3(gs, gs * s, 1f);
            yield return null;
        }
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2.2f, 4.5f));
            if (eyelids == null) continue;
            eyelids.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.11f);
            eyelids.gameObject.SetActive(false);
        }
    }

    IEnumerator Talk(float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            mouthOpen.gameObject.SetActive(true); mouthClose.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.12f);
            mouthOpen.gameObject.SetActive(false); mouthClose.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.10f);
            t += 0.22f;
        }
        mouthOpen.gameObject.SetActive(false); mouthClose.gameObject.SetActive(true);
    }

    IEnumerator Eat()
    {
        StartCoroutine(Bounce(1.12f, 0.28f));
        for (int i = 0; i < 2; i++)
        {
            mouthOpen.gameObject.SetActive(true); mouthClose.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.13f);
            mouthOpen.gameObject.SetActive(false); mouthClose.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator Bounce(float peak, float dur)
    {
        animating = true;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float gs = GrowScalar;
            float p = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
            float sy = Mathf.Lerp(1f, peak, p);
            float sx = 1f + (1f - sy) * 0.5f;
            antRoot.localScale = new Vector3(sx * gs, sy * gs, 1f);
            antRoot.anchoredPosition = antBasePos + new Vector2(0, p * 40f);
            yield return null;
        }
        antRoot.localScale = new Vector3(GrowScalar, GrowScalar, 1f); antRoot.anchoredPosition = antBasePos;
        animating = false;
    }

    // ================= UI =================
    void BuildCanvasAndUI()
    {
        var cgo = new GameObject("AntGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = cgo.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 50;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080, 1920);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;
        canvasT = cgo.transform;

        var bg = NewUI("BG", canvasT); Stretch(bg.GetComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.80f, 0.92f, 1f);

        confettiParent = NewUI("Confetti", canvasT).transform; Stretch((RectTransform)confettiParent);

        // maqsad qatori (rasm/nuqta) — yozuvsiz ko'rsatma
        var goal = NewUI("Goal", canvasT);
        var grt = goal.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1f); grt.pivot = new Vector2(0.5f, 1f);
        grt.anchoredPosition = new Vector2(0, -230); grt.sizeDelta = new Vector2(860, 150);
        var ghl = goal.AddComponent<HorizontalLayoutGroup>();
        ghl.spacing = 12; ghl.childAlignment = TextAnchor.MiddleCenter; ghl.childControlWidth = false; ghl.childControlHeight = false;
        goalRow = goal.transform;

        counterText = MakeText("Counter", canvasT, 110, new Color(0.20f, 0.55f, 0.95f));
        var crt = counterText.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(400, 150); crt.anchoredPosition = new Vector2(0, 760);
        counterText.fontStyle = FontStyles.Bold;

        feedbackText = MakeText("Fb", canvasT, 60, okCol);
        var frt = feedbackText.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f); frt.sizeDelta = new Vector2(900, 120); frt.anchoredPosition = new Vector2(0, 600);
        feedbackText.fontStyle = FontStyles.Bold;

        bubbleText = MakeText("Bubble", canvasT, 1, Color.clear); // hozircha ko'rinmas (ovoz kelganda ishlatiladi)

        var fa = NewUI("Items", canvasT);
        var fart = fa.GetComponent<RectTransform>();
        fart.anchorMin = fart.anchorMax = new Vector2(0.5f, 0f); fart.pivot = new Vector2(0.5f, 0f);
        fart.sizeDelta = new Vector2(1000, 360); fart.anchoredPosition = new Vector2(0, 60);
        itemArea = fa.transform;
    }

    // ================= EKRANLAR (menyu / o'yinlar / sozlamalar) =================
    void BuildScreens()
    {
        // BOSH MENYU
        mainScreen = FullScreen("MainMenu", new Color(0.80f, 0.92f, 1f));
        var mc = CenterRow(mainScreen.transform);
        BigBtn(mc, LoadSprite("ant_base"), new Color(0.98f, 0.72f, 0.18f), () => ShowScreen(gamesScreen));
        BigBtn(mc, GearSprite(), new Color(0.55f, 0.60f, 0.72f), () => ShowScreen(settingsScreen));

        // O'YINLAR (mini o'yinlar)
        gamesScreen = FullScreen("Games", new Color(0.86f, 0.95f, 0.88f));
        var gc = CenterRow(gamesScreen.transform);
        foreach (var p in providers) { var prov = p; BigBtn(gc, prov.MenuIcon(this), prov.MenuColor, () => SetProvider(prov)); }
        CornerBtn(gamesScreen.transform, ArrowSprite(), () => ShowScreen(mainScreen));

        // SOZLAMALAR
        settingsScreen = FullScreen("Settings", new Color(0.95f, 0.90f, 0.99f));
        var sc = CenterRow(settingsScreen.transform);
        var sb = BigBtn(sc, SpeakerSprite(), sound ? okCol : new Color(0.62f, 0.62f, 0.66f), ToggleSound);
        soundImg = sb.GetComponent<Image>();
        CornerBtn(settingsScreen.transform, ArrowSprite(), () => ShowScreen(mainScreen));

        // UY tugmasi (o'yin ichida — o'yinlar ro'yxatiga qaytadi)
        homeBtn = CornerBtn(canvasT, ArrowSprite(), () => ShowScreen(gamesScreen));
        homeBtn.SetActive(false);

        mainScreen.SetActive(false); gamesScreen.SetActive(false); settingsScreen.SetActive(false);
    }

    void ShowScreen(GameObject go)
    {
        if (mainScreen) mainScreen.SetActive(go == mainScreen);
        if (gamesScreen) gamesScreen.SetActive(go == gamesScreen);
        if (settingsScreen) settingsScreen.SetActive(go == settingsScreen);
        if (homeBtn) homeBtn.SetActive(go == null);   // faqat o'yin ichida
    }

    void ToggleSound()
    {
        sound = !sound;
        PlayerPrefs.SetInt("antSound", sound ? 1 : 0); PlayerPrefs.Save();
        if (soundImg) soundImg.color = sound ? okCol : new Color(0.62f, 0.62f, 0.66f);
    }

    GameObject FullScreen(string name, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI"); go.transform.SetParent(canvasT, false);
        Stretch((RectTransform)go.transform);
        var img = go.GetComponent<Image>(); img.color = bg; img.raycastTarget = true; // orqani to'sadi
        return go;
    }

    Transform CenterRow(Transform parent)
    {
        var go = NewUI("Row", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 320);
        var hl = go.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 40; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = false; hl.childControlHeight = false;
        return go.transform;
    }

    GameObject BigBtn(Transform parent, Sprite icon, Color col, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakePanel("bb", parent, 260, 260, col);
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 260; le.preferredHeight = 260;
        var b = go.AddComponent<Button>(); b.targetGraphic = go.GetComponent<Image>(); b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        var ic = NewImg("i", go.transform, 180, 180);
        ic.sprite = icon; ic.color = Color.white; ic.preserveAspect = true;
        var irt = ic.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.anchoredPosition = Vector2.zero;
        return go;
    }

    GameObject CornerBtn(Transform parent, Sprite icon, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakePanel("corner", parent, 110, 110, new Color(0.36f, 0.42f, 0.96f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(24, -24);
        var b = go.AddComponent<Button>(); b.targetGraphic = go.GetComponent<Image>(); b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        var ic = NewImg("i", go.transform, 66, 66);
        ic.sprite = icon; ic.color = Color.white; ic.preserveAspect = true;
        var irt = ic.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.anchoredPosition = Vector2.zero;
        return go;
    }

    // ---- menyu ikonkalari (generatsiya) ----
    static Sprite _gear, _arrow, _speaker;
    static Sprite Tex(System.Func<float, float, bool> inside, ref Sprite cache)
    {
        if (cache != null) return cache;
        int sz = 128; var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var px = new Color32[sz * sz];
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
            px[y * sz + x] = inside(x / (float)(sz - 1), y / (float)(sz - 1)) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        tex.SetPixels32(px); tex.Apply();
        cache = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        return cache;
    }
    Sprite GearSprite() => Tex((u, v) => {
        float dx = u - .5f, dy = v - .5f; float r = Mathf.Sqrt(dx * dx + dy * dy);
        float ang = Mathf.Atan2(dy, dx);
        float rOuter = 0.34f + (Mathf.Cos(ang * 8f) > 0.25f ? 0.10f : 0f);
        return r < rOuter && r > 0.13f;
    }, ref _gear);
    Sprite ArrowSprite() => Tex((u, v) => u > .18f && u < .82f && Mathf.Abs(v - .5f) < 0.62f * (u - .16f), ref _arrow); // chapga
    Sprite SpeakerSprite() => Tex((u, v) => (u < .34f && Mathf.Abs(v - .5f) < .22f) || (u < .82f && Mathf.Abs(v - .5f) < 0.6f * (.82f - u)), ref _speaker);

    void Say(string s) { /* ovoz kelganда shu yerда ijro qilinadi */ }
    void SetFeedback(string s) { if (feedbackText != null) feedbackText.text = s; }
    void UpdateCounter() { if (counterText != null) counterText.text = task != null ? (progress + " / " + task.need) : ""; }

    string NumberWord(int n)
    {
        string[] w = { "", "bir", "ikki", "uch", "to‘rt", "besh", "olti", "yetti", "sakkiz", "to‘qqiz", "o‘n" };
        return (n >= 1 && n <= 10) ? w[n] : n.ToString();
    }

    IEnumerator Confetti()
    {
        int n = 26; var pieces = new List<RectTransform>(); var vel = new List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("c", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(confettiParent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Random.Range(18, 30), Random.Range(18, 30));
            rt.anchoredPosition = new Vector2(Random.Range(-200f, 200f), 500f);
            go.GetComponent<Image>().color = shapeCols[Random.Range(0, shapeCols.Length)];
            pieces.Add(rt); vel.Add(new Vector2(Random.Range(-260f, 260f), Random.Range(120f, 420f)));
        }
        float t = 0f, dur = 1.6f;
        while (t < dur)
        {
            float dt = Time.unscaledDeltaTime; t += dt;
            for (int i = 0; i < pieces.Count; i++)
            { var v = vel[i]; v.y -= 1400f * dt; vel[i] = v; pieces[i].anchoredPosition += v * dt; pieces[i].Rotate(0, 0, 300f * dt); }
            yield return null;
        }
        foreach (var p in pieces) if (p) Destroy(p.gameObject);
    }

    IEnumerator PopIn(RectTransform rt, float dur = 0.3f)
    {
        if (rt == null) yield break;
        float t = 0f;
        while (t < dur) { t += Time.unscaledDeltaTime; float p = Mathf.Clamp01(t / dur); float s = 1f + 0.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f); rt.localScale = new Vector3(s, s, 1f); yield return null; }
        rt.localScale = Vector3.one;
    }

    // ---- sprite generatorlari (providerlar ishlatadi) ----
    static Sprite _apple; static bool _appleTried;
    public Sprite AppleSprite()
    {
        if (_appleTried) return _apple; _appleTried = true;
        var tex = Resources.Load<Texture2D>("AntGame/food_apple");
        if (tex != null) _apple = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _apple;
    }

    static Sprite _circle;
    public Sprite FoodSprite()
    {
        if (_circle != null) return _circle;
        int s = 64; float r = s / 2f - 1;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s]; Vector2 c = new Vector2(s / 2f, s / 2f);
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            px[y * s + x] = new Color32(255, 255, 255, (byte)(Vector2.Distance(new Vector2(x + .5f, y + .5f), c) <= r ? 255 : 0));
        tex.SetPixels32(px); tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return _circle;
    }

    static readonly Dictionary<int, Sprite> _shapes = new Dictionary<int, Sprite>();
    public Sprite ShapeSprite(int kind)
    {
        if (_shapes.TryGetValue(kind, out var sp)) return sp;
        int sz = 128; var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var px = new Color32[sz * sz];
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float u = x / (float)(sz - 1), v = y / (float)(sz - 1); bool inside;
            if (kind == 0) inside = Mathf.Sqrt((u - .5f) * (u - .5f) + (v - .5f) * (v - .5f)) <= 0.46f;
            else if (kind == 1) inside = u > .08f && u < .92f && v > .08f && v < .92f;
            else if (kind == 2) { float yy = 1 - v; inside = yy > .08f && yy < .92f && Mathf.Abs(u - .5f) < 0.5f * yy; }
            else inside = (Mathf.Abs(u - .5f) + Mathf.Abs(v - .5f)) <= 0.46f;
            px[y * sz + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px); tex.Apply();
        sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        _shapes[kind] = sp; return sp;
    }

    // ---- kichik yordamchilar ----
    GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false); return go;
    }
    Image NewImg(string name, Transform parent, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI"); go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(w, h);
        return go.GetComponent<Image>();
    }
    void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    TMP_Text MakeText(string n, Transform p, float sz, Color col)
    {
        var go = NewUI(n, p); var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font; t.fontSize = sz; t.color = col; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t;
    }
    GameObject MakePanel(string n, Transform p, float w, float h, Color col)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI"); go.transform.SetParent(p, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = col;
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.15f); sh.effectDistance = new Vector2(0, -4);
        return go;
    }
    void EnsureEventSystem()
    {
        if (Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 0)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

// ================== MAVZULAR (konfiguratsiya) ==================

/// <summary>Sanash: N ta olma ber. Maqsad = N nuqta.</summary>
public class CountProvider : ITaskProvider
{
    int lvl = 1;
    public Color MenuColor => new Color(0.98f, 0.72f, 0.18f);
    public Sprite MenuIcon(AntGameController g) => g.AppleSprite() ?? g.FoodSprite();
    public Task Next(AntGameController g)
    {
        int max = g.MaxNumber;
        if (lvl > max) lvl = 1;
        int n = lvl; lvl = lvl >= max ? 1 : lvl + 1;
        var t = new Task { goalDots = n, voice = n.ToString(), need = n };
        var apple = g.AppleSprite();
        for (int i = 0; i < n; i++)
            t.items.Add(new TaskItem { sprite = apple ?? g.FoodSprite(), color = apple ? Color.white : new Color(0.95f, 0.4f, 0.4f), accept = true });
        return t;
    }
}

/// <summary>Shakllar: so'ralган shaklni ber. Maqsad = shakl rasmi.</summary>
public class ShapeProvider : ITaskProvider
{
    public Color MenuColor => new Color(0.30f, 0.62f, 0.98f);
    public Sprite MenuIcon(AntGameController g) => g.ShapeSprite(2);
    public Task Next(AntGameController g)
    {
        int types = Mathf.Clamp(g.ShapeCount, 2, 4);
        int target = Random.Range(0, types);
        var kinds = new List<int> { target };
        int guard = 0;
        while (kinds.Count < types && guard++ < 40) { int k = Random.Range(0, types); if (!kinds.Contains(k)) kinds.Add(k); }
        for (int i = kinds.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (kinds[i], kinds[j]) = (kinds[j], kinds[i]); }
        var t = new Task { goalIcon = g.ShapeSprite(target), goalColor = g.ShapeColor(target), need = 1, voice = "shakl" };
        foreach (int k in kinds)
            t.items.Add(new TaskItem { sprite = g.ShapeSprite(k), color = g.ShapeColor(k), accept = (k == target) });
        return t;
    }
}

/// <summary>Narsani chumoli og'ziga sudrab olib borilса — dvigatel tekshiradi.</summary>
public class FoodDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform mouth;
    public float eatRadius = 240f;
    public bool accept;
    public System.Func<FoodDrag, bool> onDropped;

    RectTransform rt; Canvas canvas; Transform startParent; Vector3 startPos;
    void Awake() { rt = (RectTransform)transform; }

    public void OnBeginDrag(PointerEventData e)
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        startParent = transform.parent; startPos = rt.position;
        if (canvas != null) { transform.SetParent(canvas.transform, true); transform.SetAsLastSibling(); }
    }
    public void OnDrag(PointerEventData e) { var p = rt.position; p.x = e.position.x; p.y = e.position.y; rt.position = p; }
    public void OnEndDrag(PointerEventData e)
    {
        bool near = mouth != null && Vector2.Distance(e.position, (Vector2)mouth.position) < eatRadius;
        bool ok = near && onDropped != null && onDropped(this);
        if (ok) Destroy(gameObject);
        else { transform.SetParent(startParent, true); rt.position = startPos; StartCoroutine(Pop()); }
    }
    System.Collections.IEnumerator Pop()
    {
        float t = 0f;
        while (t < 0.15f) { t += Time.unscaledDeltaTime; float s = Mathf.Lerp(1.12f, 1f, t / 0.15f); rt.localScale = new Vector3(s, s, 1f); yield return null; }
        rt.localScale = Vector3.one;
    }
}
