using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Momiq — o'zbekcha gapiradigan qo'zichoq (Talking-Tom uslubi) o'yini.
/// Claude Design "Qo'zichoq O'yini" dizayni asosida, Unity uGUI (runtime) versiyasi.
/// Personaj — Desktop'dagi haqiqiy 3D qatlamli qo'zichoq (Resources/Momiq/*.png).
/// Web versiya koordinatalari (390x844 sahna) 1:1 saqlangan; sahna ekranga fit-scale qilinadi.
/// </summary>
public class MomiqController : MonoBehaviour
{
    // ---- Sahna o'lchami (dizayn) ----
    const float SW = 390f, SH = 844f;

    // ---- State ----
    string screen = "menu";
    int coins = 248, hunger = 72, joy = 84, energy = 58, clean = 66;
    string mood = "xursand";
    string msg = "Salom, do'stim! Meni erkalab qo'y.";
    bool hearts = false, bubbles = false;
    bool sound = true, music = true, titrash = true, nazorat = false;
    bool giftTaken = false;
    int kun = 3;
    string nom = "Momiq";
    bool nomSet = false;
    string makon = "home";
    string fan = "matem";
    int statTogri = 0, statXato = 0, stikerCount = 3;
    System.Collections.Generic.List<int> tartibSeq = new System.Collections.Generic.List<int>();
    int tartibIdx = 0;
    class MQ { public string prompt; public int dots; public Color dotCol; public string[] opts; public int correct; }
    readonly System.Collections.Generic.List<MQ> mq = new System.Collections.Generic.List<MQ>();
    int mqIdx = 0; string mqResult = "";
    string bola = "";
    int xp = 30, daraja = 2;
    bool levelUp = false;

    int harfIndex = 0;
    readonly List<string> organgan = new List<string>();
    int son = 3;
    List<int> sanoqVariant = new List<int> { 2, 3, 5 };
    string target = "Qizil";
    string natija = "";
    int inglizIndex = 0;

    readonly Dictionary<string, int> fanlar = new Dictionary<string, int> { { "matem", 45 }, { "ingliz", 20 }, { "tabiiy", 35 }, { "savod", 60 } };
    readonly Dictionary<string, int> hisob = new Dictionary<string, int> { { "erkalash", 0 }, { "ovqat", 0 }, { "yuvish", 0 }, { "oyin", 0 } };
    readonly List<string> olingan = new List<string>();
    readonly Dictionary<string, bool> wear = new Dictionary<string, bool> { { "dopi", true }, { "chopon", true }, { "sharf", true }, { "kozoynak", false } };
    readonly Dictionary<string, bool> owned = new Dictionary<string, bool> { { "dopi", true }, { "chopon", true }, { "sharf", true }, { "kozoynak", false }, { "gilam", false } };

    // Olma tut o'yini
    bool gActive = false, gOver = false;
    int gScore = 0, gTime = 20;

    // Cho'milish / tun
    bool bath = false;
    readonly List<GameObject> dirtObjs = new List<GameObject>();
    int dirtLeft = 0;

    // Xotira o'yini (Juftini top)
    class MCard { public string harf; public Color rang; public int id; }
    readonly List<MCard> mCards = new List<MCard>();
    readonly List<int> mOpen = new List<int>();
    readonly List<string> mMatched = new List<string>();
    int mMoves = 0; bool mBusy = false;

    // ---- Ma'lumot jadvallari ----
    struct Harf { public string h, s; public Color r; public Harf(string h, string s, string hex) { this.h = h; this.s = s; this.r = Hex(hex); } }
    Harf[] harflar;
    struct Soz { public string en, uz; public Soz(string e, string u) { en = e; uz = u; } }
    Soz[] inglizSozlar;
    struct Rang { public string nom; public Color kod; public Rang(string n, string hex) { nom = n; kod = Hex(hex); } }
    Rang[] ranglar;

    // ---- UI ----
    Canvas canvas;
    RectTransform stage;      // 390x844, ekranga fit-scale
    RectTransform screenRoot;
    RectTransform buildRoot;
    RectTransform scrollContent; // joriy ekran shu yerga quriladi
    MomiqRig rig;             // joriy ekrandagi personaj
    TMP_FontAsset font;

    // sprite kesh
    readonly Dictionary<string, Sprite> partCache = new Dictionary<string, Sprite>();
    Sprite roundedSp, circleSp, glowSp, triSp, gearSp;
    readonly Dictionary<string, Sprite> gradCache = new Dictionary<string, Sprite>();
    readonly Dictionary<string, Sprite> uiCache = new Dictionary<string, Sprite>();

    // coroutine tutqichlar
    Coroutine resetCo, gameCo;

    void Start()
    {
        BuildTables();
        font = TMP_Settings.defaultFontAsset;
        roundedSp = GenRounded(64, 22);
        circleSp = GenCircle(64);
        glowSp = GenGlow(128);
        triSp = GenTriangle(256);
        gearSp = GenGear(64);
        LoadState();
        EnsureEventSystem();
        BuildCanvas();
        Show(screen);
        StartCoroutine(Decay());
        TgInit();
    }

    void BuildTables()
    {
        harflar = new[]{
            new Harf("A","Anor","#C8452F"), new Harf("B","Baliq","#1E7A8C"),
            new Harf("D","Do'ppi","#12303B"), new Harf("G","Gul","#E4655C"),
            new Harf("I","It","#8A5A3C"), new Harf("K","Kitob","#7FA650"),
            new Harf("L","Lola","#C8452F"), new Harf("N","Non","#E9A62B"),
            new Harf("O","Olma","#B23F2E"), new Harf("Q","Qo'zichoq","#12303B"),
            new Harf("S","Somsa","#C98A3C"), new Harf("U","Uzum","#6B4A7A"),
        };
        inglizSozlar = new[]{
            new Soz("Sheep","Qo'zichoq"), new Soz("Apple","Olma"), new Soz("Bread","Non"), new Soz("Milk","Sut"),
            new Soz("Cat","Mushuk"), new Soz("Sun","Quyosh"), new Soz("Water","Suv"), new Soz("Book","Kitob"),
        };
        ranglar = new[]{
            new Rang("Qizil","#C8452F"), new Rang("Ko'k","#1E7A8C"), new Rang("Yashil","#7FA650"), new Rang("Sariq","#E9A62B"),
        };
    }

    // ================= CANVAS / STAGE =================
    void BuildCanvas()
    {
        var cgo = new GameObject("MomiqCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // fit-scale'ni o'zimiz qilamiz
        sc.scaleFactor = 1f;

        // to'liq ekran fon (sahnadan tashqari joy)
        var bg = Node("Backdrop", canvas.transform, 0, 0, 0, 0);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = Hex("#12303B");

        stage = Node("Stage", canvas.transform, 0, 0, SW, SH);
        stage.anchorMin = new Vector2(0.5f, 0.5f); stage.anchorMax = new Vector2(0.5f, 0.5f); stage.pivot = new Vector2(0.5f, 0.5f);
        stage.anchoredPosition = Vector2.zero;
        FitStage();
    }

    void Update() { FitStage(); }

    void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 0)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void FitStage()
    {
        if (stage == null) return;
        float s = Mathf.Min(Screen.width / SW, Screen.height / SH);
        stage.localScale = new Vector3(s, s, 1f);
    }

    // ================= EKRAN QURISH =================
    void Show(string scr)
    {
        screen = scr;
        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }
        if (gameCo != null && scr != "oyin") { StopCoroutine(gameCo); gameCo = null; gActive = false; }
        rig = null;
        dirtObjs.Clear();

        if (screenRoot != null) Destroy(screenRoot.gameObject);
        screenRoot = Node("Screen_" + scr, stage, 0, 0, SW, SH);
        screenRoot.anchorMin = new Vector2(0, 1); screenRoot.anchorMax = new Vector2(0, 1); screenRoot.pivot = new Vector2(0, 1);
        screenRoot.anchoredPosition = Vector2.zero;
        buildRoot = screenRoot;
        scrollContent = null;

        switch (scr)
        {
            case "menu": BuildMenu(); break;
            case "ism": BuildIsm(); break;
            case "home": BuildHome(); break;
            case "sozlamalar": BuildSettings(); break;
            case "talim": BuildTalim(); break;
            case "sanoq": BuildSanoq(); break;
            case "rang": BuildRang(); break;
            case "ingliz": BuildIngliz(); break;
            case "harf": BuildHarf(); break;
            case "oyinlar": BuildHub(); break;
            case "oyin": BuildGame(); break;
            case "oshxona": BuildKitchen(); break;
            case "garderob": BuildWardrobe(); break;
            case "dokon": BuildShop(); break;
            case "yutuqlar": BuildAch(); break;
            case "xotira": BuildMemory(); break;
            case "yotoq": BuildYotoq(); break;
            case "kitob": BuildKitob(); break;
            case "matem": BuildMatem(); break;
            case "fanjadval": BuildFanJadval(); break;
            case "mashq": BuildMashq(); break;
            case "hamyon": BuildHamyon(); break;
            case "stiker": BuildStiker(); break;
            case "chop": BuildChop(); break;
            case "vazifalar": BuildVazifalar(); break;
            case "rekordlar": BuildRekordlar(); break;
            case "analitika": BuildAnalitika(); break;
            case "diplom": BuildDiplom(); break;
            case "sertifikat": BuildSertifikat(); break;
            case "tartib": BuildTartib(); break;
            default: BuildMenu(); break;
        }
        if (levelUp) BuildLevelUp();
    }

    void Refresh() { Show(screen); }

    // ================= EKRANLAR =================
    void BuildMenu()
    {
        ImgBg("menu");
        float mapTop = 172f, mapH = SH - 172f - 152f;
        string[] bek = { "home", "talim", "oshxona", "yotoq", "oyinlar", "dokon", "garderob", "hammom", "upgxona" };
        float[] bx = { 26, 74, 74, 26, 26, 74, 74, 26, 50 };
        float[] by = { 1, 1, 24, 24, 46, 46, 68, 68, 90 };
        for (int i = 0; i < 9; i++)
        {
            float cx = bx[i] / 100f * SW, cy = mapTop + by[i] / 100f * mapH;
            string ek = bek[i];
            Zone(cx - 46, cy - 8, 92, 112, () => MenuGo(ek));
        }
        Zone(14, 50, 108, 46, () => Show("hamyon"));
        Zone(SW - 108, 50, 44, 46, () => Show("yutuqlar"));
        Zone(SW - 60, 50, 44, 46, () => Show("sozlamalar"));
        Zone(SW - 134, SH - 74, 118, 60, () => { if (nomSet) Show("home"); else Show("ism"); });
    }

    void MenuGo(string ek)
    {
        makon = ek;
        Show("home");
    }

    // makon -> [top, bot, floor] rang; joy nomi/holati/tugma
    string JoyNomi(string m)
    {
        switch (m) { case "talim": return "Sinfxona"; case "oshxona": return "Oshxona"; case "yotoq": return "Yotoqxona";
            case "oyinlar": return "O'yin maydoni"; case "dokon": return "Do'kon"; case "garderob": return "Garderob";
            case "hammom": return "Yuvinish xonasi"; case "upgxona": return "UPG xonasi"; default: return "Momiq uyi"; }
    }
    string JoyHolat(string m)
    {
        switch (m) {
            case "talim": return organgan.Count + " dars";
            case "oshxona": return hunger > 70 ? "qorni to'q" : (hunger > 35 ? "bir oz och" : "juda och");
            case "yotoq": return "kuch " + energy + "%";
            case "oyinlar": return "rekord 0";
            case "dokon": return coins + " SSA coin";
            case "garderob": { int c = 0; foreach (var kv in wear) if (kv.Value) c++; return c + " ta kiyim"; }
            case "hammom": return "toza " + clean + "%";
            case "upgxona": return "0 UPG daraja";
            default: return "uyda";
        }
    }
    string JoyTugma(string m)
    {
        switch (m) { case "talim": return "Darsni ochish"; case "oshxona": return "Ovqat berish"; case "yotoq": return "Uxlatish";
            case "oyinlar": return "O'yinlar"; case "dokon": return "Xarid qilish"; case "garderob": return "Kiyintirish";
            case "hammom": return "Yuvintirish"; case "upgxona": return "UPG do'koni"; default: return ""; }
    }
    void JoyOchish(string m)
    {
        switch (m) {
            case "talim": Show("talim"); break;
            case "oshxona": Show("oshxona"); break;
            case "oyinlar": Show("oyinlar"); break;
            case "dokon": Show("dokon"); break;
            case "garderob": Show("garderob"); break;
            case "yotoq": Show("yotoq"); break;
            case "hammom": StartBath(); break;
            case "upgxona": Show("dokon"); break;
        }
    }

    void BuildIsm()
    {
        ImgBg("ism");
        Zone(26, SH - 110, SW - 52, 80, () => { nomSet = true; Save(); Show("home"); });
    }


    void BuildHome()
    {
        ImgBg("home_" + makon);
        if (makon != "home") Zone(18, SH - 150, SW - 36, 50, () => JoyOchish(makon));
        else Zone(18, SH - 150, SW - 36, 50, () => { makon = "talim"; Show("home"); });
        Zone(18, SH - 91, 66, 48, () => Show("yutuqlar"));
        Zone(92, SH - 91, SW - 110, 48, () => Show("menu"));
        Zone(24, 56, 100, 40, () => Show("hamyon"));
        Zone(SW - 64, 56, 40, 40, () => { if (mood == "uyquda") React("xursand", "Xayrli tong!", 1.5f); else React("uyquda", "Alla-yo... uxlayapman.", 0); });
        Zone(SW / 2f - 70, 300, 140, 220, () => { Bump(ref joy, 6); AddXp(6, "erkalash"); React("kulgan", "Ie-he-he, yoqimli!", 1.5f); });
        if (mood == "uyquda" && !bath) BuildNightOverlay();
        if (bath) BuildBathOverlay();
    }

    void BuildNightOverlay()
    {
        var ov = Rect2("night", 0, 0, SW, SH, new Color(0.035f, 0.094f, 0.16f, 0.82f));
        Label("nt", 0, 212, SW, 30, nom + " uxlayapti", 26, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);
        Label("ns", 0, 250, SW, 20, "Kuch to'lib boradi — sekin gapiring", 13, new Color(1, 0.96f, 0.9f, 0.65f), TextAlignmentOptions.Center, false);
        BigBtn("Uyg'otish", SW / 2f - 90, 560, 180, 52, Hex("#E9A62B"), Hex("#3B2A1E"), 17, () => React("kulgan", "Xayrli tong! Kuchim to'ldi.", 1.8f));
    }

    void BuildBathOverlay()
    {
        Rect2("bath", 0, 0, SW, SH, new Color(0.12f, 0.48f, 0.55f, 0.4f));
        var tip = Panel("bt", SW / 2f - 90, 198, 180, 44, Hex("#FFF8EA"));
        Label("btt", SW / 2f - 90, 208, 180, 24, "Kirlarni bosing: " + dirtLeft, 15, Hex("#16606F"), TextAlignmentOptions.Center, true);
        float[,] pos = { { 0.31f, 0.62f }, { 0.56f, 0.67f }, { 0.43f, 0.74f }, { 0.62f, 0.57f } };
        dirtObjs.Clear();
        for (int i = 0; i < dirtLeft; i++)
        {
            float x = pos[i, 0] * SW - 26, y = pos[i, 1] * SH - 26;
            var d = Circle("dirt" + i, x, y, 52, Hex("#6E5A44"));
            dirtObjs.Add(d.gameObject);
            Clickable(d.gameObject, () => RubDirt());
        }
    }

    void BuildSettings()
    {
        ImgBg("sozlamalar");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(22, SH - 90, SW - 44, 60, () => Show("menu"));
    }


    void BuildTalim()
    {
        ImgBg("talim");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(60, 42, 50, 52, () => Show("menu"));
        Zone(22, 150, SW - 44, 155, () => { fan = "matem"; Show("matem"); });
        Zone(22, 310, SW - 44, 155, () => Show("ingliz"));
        Zone(22, 470, SW - 44, 155, () => { fan = "tabiiy"; Show("matem"); });
        Zone(22, 630, SW - 44, 150, () => { fan = "savod"; Show("matem"); });
    }


    void BuildSanoq()
    {
        ImgBg("sanoq");
        Zone(10, 42, 56, 52, () => Show("matem"));
    }


    void BuildRang()
    {
        ImgBg("rang");
        Zone(10, 42, 56, 52, () => Show("matem"));
    }


    void BuildIngliz()
    {
        ImgBg("ingliz");
        Zone(10, 42, 56, 52, () => Show("talim"));
    }


    void BuildHarf()
    {
        ImgBg("harf");
        Zone(10, 42, 56, 52, () => Show("talim"));
    }


    void BuildHub()
    {
        ImgBg("oyinlar");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(22, 120, SW - 44, 190, () => Show("oyin"));
        Zone(22, 320, SW - 44, 190, () => StartMemory());
    }


    void BuildGame()
    {
        ImgBg("oyin");
        Zone(10, 42, 56, 52, () => Show("oyinlar"));
    }


    void BuildKitchen()
    {
        ImgBg("oshxona");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(60, 42, 50, 52, () => Show("menu"));
        Zone(22, 400, SW - 44, 220, () => { Bump(ref hunger, 18); Bump(ref joy, 4); AddXp(8, "ovqat"); React("ovqat", "Juda mazza! Rahmat!", 1.6f); });
    }


    void BuildWardrobe()
    {
        ImgBg("garderob");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(60, 42, 50, 52, () => Show("menu"));
    }


    void BuildShop()
    {
        ImgBg("dokon");
        Zone(10, 42, 56, 52, () => Show("home"));
    }


    void BuildAch()
    {
        GradBg("#F3E2C4", "#FFF9EE");
        HeaderBack("Yutuqlar", "Natijalar va bo'limlar", "home", Hex("#3A3330"));
        MapBtnAt(() => Show("menu"), Hex("#3A3330"));
        BeginScroll(80);
        string[] ln = { "Kunlik vazifalar", "Rekordlar", "Tahlil", "Diplom", "Sertifikat", "Stikerlar", "Kitoblar", "Mashqlar", "Tartib o'yini", "Chop etish" };
        string[] lk = { "vazifalar", "rekordlar", "analitika", "diplom", "sertifikat", "stiker", "kitob", "mashq", "tartib", "chop" };
        Color[] lc = { Hex("#28D62C"), Hex("#E29B18"), Hex("#3B9BF0"), Hex("#C8452F"), Hex("#B06BA8"), Hex("#FFB800"), Hex("#12A83A"), Hex("#3B9BF0"), Hex("#6B5CC4"), Hex("#5C6BA8") };
        for (int i = 0; i < ln.Length; i++)
        {
            float cw = (SW - 44 - 12) / 2f;
            float x = (i % 2) * (cw + 12);
            float y = 4 + (i / 2) * 92;
            var c = Panel("lk" + i, x, y, cw, 80, Color.white);
            Circle("lc" + i, x + 14, y + 14, 40, lc[i]);
            Label("ln" + i, x + 62, y + 20, cw - 74, 40, ln[i], 15, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            string key = lk[i];
            Clickable(c.gameObject, () => Show(key));
        }
        EndScroll(5 * 92 + 20);
    }

    void BuildMemory()
    {
        ImgBg("xotira");
        Zone(10, 42, 56, 52, () => Show("oyinlar"));
    }


    void BuildLevelUp()
    {
        var ov = Rect2("lvl", 0, 0, SW, SH, new Color(0.07f, 0.19f, 0.23f, 0.55f));
        var card = Panel("lc", SW / 2f - 140, SH / 2f - 90, 280, 160, Hex("#FFF8EA"));
        Label("lt", SW / 2f - 140, SH / 2f - 70, 280, 16, "YANGI DARAJA", 11, Hex("#C8452F"), TextAlignmentOptions.Center, true);
        Label("ld", SW / 2f - 140, SH / 2f - 52, 280, 50, daraja.ToString(), 46, Hex("#12303B"), TextAlignmentOptions.Center, true);
        Label("ls", SW / 2f - 140, SH / 2f + 10, 280, 20, nom + " kattaroq bo'ldi · +20 tanga", 13, new Color(0.07f, 0.19f, 0.23f, 0.6f), TextAlignmentOptions.Center, false);
    }

    // ================= LOGIKA =================
    void React(string m, string message, float ms)
    {
        if (resetCo != null) StopCoroutine(resetCo);
        mood = m; msg = message;
        if (screen == "home" || screen == "oshxona" || screen == "garderob") Refresh();
        if (ms > 0) resetCo = StartCoroutine(ResetMood(ms));
    }
    IEnumerator ResetMood(float ms)
    {
        yield return new WaitForSeconds(ms);
        mood = "xursand"; hearts = false; bubbles = false;
        if (screen == "home" || screen == "oshxona" || screen == "garderob") Refresh();
    }
    void Bump(ref int v, int amt) { v = Mathf.Clamp(v + amt, 0, 100); }

    void AddXp(int n, string tur)
    {
        xp += n;
        if (tur != null && hisob.ContainsKey(tur)) hisob[tur]++;
        if (xp >= 100) { xp -= 100; daraja++; levelUp = true; coins += 20; StartCoroutine(ClearLevelUp()); }
        Save();
    }
    IEnumerator ClearLevelUp() { yield return new WaitForSeconds(2.6f); levelUp = false; Refresh(); }
    void AddFan(string k, int n) { if (fanlar.ContainsKey(k)) fanlar[k] = Mathf.Min(100, fanlar[k] + n); }

    void PickLetter(int i)
    {
        harfIndex = i;
        if (!organgan.Contains(harflar[i].h)) organgan.Add(harflar[i].h);
        msg = harflar[i].h + " harfi — " + harflar[i].s;
        mood = "kulgan"; AddXp(3, null); AddFan("savod", 5); Refresh();
        resetCo = StartCoroutine(ResetMoodOnScreen(1.3f));
    }
    void PickWord(int i)
    {
        inglizIndex = i; msg = inglizSozlar[i].en + " — " + inglizSozlar[i].uz;
        mood = "kulgan"; AddXp(3, null); AddFan("ingliz", 5); Refresh();
        resetCo = StartCoroutine(ResetMoodOnScreen(1.3f));
    }
    IEnumerator ResetMoodOnScreen(float ms) { yield return new WaitForSeconds(ms); mood = "xursand"; Refresh(); }

    void NewCount()
    {
        son = UnityEngine.Random.Range(1, 6);
        var set = new HashSet<int> { son };
        while (set.Count < 3) set.Add(UnityEngine.Random.Range(1, 6));
        sanoqVariant = new List<int>(set);
        for (int i = 0; i < sanoqVariant.Count; i++) { int j = UnityEngine.Random.Range(i, sanoqVariant.Count); var t = sanoqVariant[i]; sanoqVariant[i] = sanoqVariant[j]; sanoqVariant[j] = t; }
        natija = ""; Show("sanoq");
    }
    void AnswerCount(int n)
    {
        if (n == son) { natija = "To'g'ri!"; coins += 2; mood = "kulgan"; msg = n + " ta! Barakalla!"; AddXp(5, null); AddFan("matem", 7); Refresh(); StartCoroutine(Delay(1.1f, NewCount)); }
        else { natija = "Yana sanab ko'ring"; mood = "xafa"; Refresh(); StartCoroutine(Delay(1.1f, () => { mood = "xursand"; natija = ""; Refresh(); })); }
    }
    void NewColor() { target = ranglar[UnityEngine.Random.Range(0, ranglar.Length)].nom; natija = ""; Show("rang"); }
    void AnswerColor(string nm)
    {
        if (nm == target) { natija = "To'g'ri!"; coins += 2; mood = "kulgan"; msg = target + " rang topildi!"; AddXp(5, null); AddFan("tabiiy", 7); Refresh(); StartCoroutine(Delay(1.1f, NewColor)); }
        else { natija = "Bu boshqa rang"; mood = "xafa"; Refresh(); StartCoroutine(Delay(1.1f, () => { mood = "xursand"; natija = ""; Refresh(); })); }
    }
    IEnumerator Delay(float s, Action a) { yield return new WaitForSeconds(s); a(); }

    void ToggleSetting(int i) { if (i == 0) sound = !sound; else if (i == 1) music = !music; else if (i == 2) titrash = !titrash; else nazorat = !nazorat; Save(); }

    void StartBath()
    {
        bath = true; dirtLeft = 4; bubbles = true; mood = "yuvinish"; msg = "Kirlarni bosib tozalang!"; Refresh();
    }
    void RubDirt()
    {
        dirtLeft = Mathf.Max(0, dirtLeft - 1);
        Bump(ref clean, 9); coins += 1; xp += 4; hisob["yuvish"]++;
        if (dirtLeft == 0) { bath = false; bubbles = false; mood = "kulgan"; msg = "Junim oppoq bo'ldi! Rahmat."; StartCoroutine(Delay(1.4f, () => { mood = "xursand"; Refresh(); })); }
        else { mood = "yuvinish"; msg = "Yana ozgina qoldi..."; }
        Save(); Refresh();
    }

    // ---- Olma tut ----
    void StartGame(RectTransform layer, TMP_Text scT, TMP_Text tmT)
    {
        gActive = true; gOver = false; gScore = 0; gTime = 20; hisob["oyin"]++; Save();
        Refresh();
    }
    IEnumerator RunGame(RectTransform layer, TMP_Text scT, TMP_Text tmT)
    {
        float spawn = 0f, tick = 0f;
        var live = new List<RectTransform>();
        var speed = new List<float>();
        while (true)
        {
            if (gActive)
            {
                spawn += Time.deltaTime; tick += Time.deltaTime;
                if (tick >= 1f) { tick = 0f; gTime--; if (tmT != null) tmT.text = gTime + "s"; if (gTime <= 0) { gActive = false; gOver = true; coins += gScore * 2; Bump(ref joy, 12); Save(); Refresh(); yield break; } }
                if (spawn >= 0.75f)
                {
                    spawn = 0f;
                    Color[] kc = { Hex("#C8452F"), Hex("#E9A62B"), Hex("#7FA650"), Hex("#1E7A8C") };
                    var it = Circle("it", UnityEngine.Random.Range(20, SW - 80), -70, 60, kc[UnityEngine.Random.Range(0, 4)]).rectTransform;
                    it.SetParent(layer, false);
                    it.anchoredPosition = new Vector2(UnityEngine.Random.Range(20, SW - 80), -0);
                    live.Add(it); speed.Add(UnityEngine.Random.Range(150f, 260f));
                    var go = it.gameObject;
                    Clickable(go, () => { if (go != null) { int gi = live.IndexOf(go.GetComponent<RectTransform>()); if (gi >= 0) { Destroy(go); live.RemoveAt(gi); speed.RemoveAt(gi); gScore++; if (scT != null) scT.text = gScore.ToString(); } } });
                }
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    if (live[i] == null) { live.RemoveAt(i); speed.RemoveAt(i); continue; }
                    var p = live[i].anchoredPosition; p.y -= speed[i] * Time.deltaTime; live[i].anchoredPosition = p;
                    if (p.y < -(SH)) { Destroy(live[i].gameObject); live.RemoveAt(i); speed.RemoveAt(i); }
                }
            }
            yield return null;
        }
    }

    IEnumerator Decay()
    {
        while (true)
        {
            yield return new WaitForSeconds(7f);
            hunger = Mathf.Max(0, hunger - 1);
            joy = Mathf.Max(0, joy - 1);
            energy = mood == "uyquda" ? Mathf.Min(100, energy + 4) : Mathf.Max(0, energy - 1);
            clean = Mathf.Max(0, clean - 1);
            if (screen == "oshxona") Refresh();
        }
    }

    void StartMemory()
    {
        string[] mh = { "O'", "N", "S", "D" };
        string[] mcx = { "#7FA650", "#C8452F", "#1E7A8C", "#E9A62B" };
        mCards.Clear();
        int id = 0;
        for (int k = 0; k < mh.Length; k++)
        {
            mCards.Add(new MCard { harf = mh[k], rang = Hex(mcx[k]), id = id++ });
            mCards.Add(new MCard { harf = mh[k], rang = Hex(mcx[k]), id = id++ });
        }
        for (int i = mCards.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = mCards[i]; mCards[i] = mCards[j]; mCards[j] = t; }
        mOpen.Clear(); mMatched.Clear(); mMoves = 0; mBusy = false;
        hisob["oyin"]++; Save();
        Show("xotira");
    }
    void FlipCard(MCard card)
    {
        if (mBusy || mOpen.Contains(card.id) || mMatched.Contains(card.harf)) return;
        mOpen.Add(card.id);
        if (mOpen.Count < 2) { Refresh(); return; }
        mMoves++;
        MCard first = mCards.Find(c => c.id == mOpen[0]);
        if (first != null && first.harf == card.harf)
        {
            mMatched.Add(card.harf); mOpen.Clear();
            bool win = mMatched.Count == 4;
            coins += 5 + (win ? 15 : 0);
            if (win) Bump(ref joy, 10);
            mood = "kulgan"; msg = win ? "Hammasini topdik! Zo'r!" : "Juftini topdik!";
            Save(); Refresh();
        }
        else { mBusy = true; Refresh(); StartCoroutine(Delay(0.75f, () => { mOpen.Clear(); mBusy = false; Refresh(); })); }
    }

    // ================= PERSONAJ =================
    void MomiqAt(float cx, float feetY, float scale)
    {
        // 230x250 quti, oyoq feetY da (pastki markaz)
        float w = 230f, h = 250f;
        var root = Node("Momiq", buildRoot, cx - w / 2f, feetY - h, w, h);
        var r = root.gameObject.AddComponent<MomiqRig>();
        r.Init(this, scale, mood);
        rig = r;
    }

    public Sprite Part(string name)
    {
        if (partCache.TryGetValue(name, out var s)) return s;
        var tex = Resources.Load<Texture2D>("Momiq/" + name);
        Sprite sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
        partCache[name] = sp;
        return sp;
    }

    Sprite UiSprite(string name)
    {
        Sprite sp;
        if (!uiCache.TryGetValue(name, out sp))
        {
            var tex = Resources.Load<Texture2D>("MomiqUI/" + name);
            sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            uiCache[name] = sp;
        }
        return sp;
    }
    void ImgBg(string name)
    {
        Sprite sp;
        if (!uiCache.TryGetValue(name, out sp))
        {
            var tex = Resources.Load<Texture2D>("MomiqUI/" + name);
            sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            uiCache[name] = sp;
        }
        var rt = Node("bg", buildRoot, 0, 0, SW, SH);
        var img = rt.gameObject.AddComponent<Image>();
        if (sp) img.sprite = sp; else img.color = Hex("#EFE6D9");
    }
    void Zone(float x, float y, float w, float h, Action a)
    {
        var rt = Node("z", buildRoot, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0); img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => { Haptic(); a(); });
    }

    // ================= SCROLL =================
    RectTransform BeginScroll(float topY)
    {
        var vp = Node("vp", screenRoot, 0, topY, SW, SH - topY);
        var vimg = vp.gameObject.AddComponent<Image>(); vimg.color = new Color(1, 1, 1, 0.004f);
        vp.gameObject.AddComponent<RectMask2D>();
        var sr = vp.gameObject.AddComponent<ScrollRect>();
        var content = new GameObject("content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(vp, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 3000);
        sr.content = content; sr.viewport = vp; sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 28f;
        buildRoot = content; scrollContent = content;
        return content;
    }
    void EndScroll(float h)
    {
        if (scrollContent != null) scrollContent.sizeDelta = new Vector2(0, h);
        buildRoot = screenRoot; scrollContent = null;
    }
    void Card(string id, float y, float h, string title, string sub, Color bg, Color fg, System.Action act)
    {
        var c = Panel("cd" + id, 22, y, SW - 44, h, bg);
        Label("cdt" + id, 42, y + h - 54, SW - 88, 28, title, 22, fg, TextAlignmentOptions.Left, true);
        Label("cds" + id, 42, y + h - 26, SW - 88, 16, sub, 12, new Color(fg.r, fg.g, fg.b, 0.72f), TextAlignmentOptions.Left, false);
        Clickable(c.gameObject, act);
    }

    // ================= UI YORDAMCHILAR =================
    RectTransform Node(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    Image Panel(string name, float x, float y, float w, float h, Color col)
    {
        var rt = Node(name, buildRoot, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = roundedSp; img.type = Image.Type.Sliced; img.color = col;
        return img;
    }
    Image Rect2(string name, float x, float y, float w, float h, Color col)
    {
        var rt = Node(name, buildRoot, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>(); img.color = col;
        return img;
    }
    Image Circle(string name, float x, float y, float d, Color col)
    {
        var rt = Node(name, buildRoot, x, y, d, d);
        var img = rt.gameObject.AddComponent<Image>(); img.sprite = circleSp; img.type = Image.Type.Simple; img.color = col;
        return img;
    }
    void BgFlat(Color col) { Rect2("bg", 0, 0, SW, SH, col); }

    TMP_Text Label(string name, float x, float y, float w, float h, string text, float size, Color col, TextAlignmentOptions align, bool bold)
    {
        var rt = Node(name, buildRoot, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.enableWordWrapping = true; t.raycastTarget = false; t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void BigBtn(string text, float x, float y, float w, float h, Color bg, Color fg, float size, Action onClick)
    {
        var img = Panel("btn_" + text, x, y, w, h, bg);
        Label("bl_" + text, x, y + (h - size) / 2f - 2, w, size + 6, text, size, fg, TextAlignmentOptions.Center, true);
        Clickable(img.gameObject, onClick);
    }
    void SmallBtn(string text, float x, float y, float w, float h, Action onClick)
    {
        var img = Panel("sb_" + text, x, y, w, h, new Color(0.07f, 0.19f, 0.23f, 0.05f));
        Label("sbl_" + text, x, y + (h - 14) / 2f, w, 20, text, 14, Hex("#12303B"), TextAlignmentOptions.Center, true);
        Clickable(img.gameObject, onClick);
    }
    void Back(Action onClick, Color stroke)
    {
        var img = Circle("back", 22, 22, 40, new Color(1, 0.98f, 0.94f, stroke == Color.white || stroke.r > 0.9f ? 0.14f : 0.94f));
        Label("bl", 22, 30, 40, 24, "<", 26, stroke, TextAlignmentOptions.Center, true);
        Clickable(img.gameObject, onClick);
    }

    // ================= YANGI EKRANLAR =================
    void HeaderBack(string title, string sub, string backScreen, Color txt)
    {
        Back(() => Show(backScreen), txt);
        Label("hbt", 74, 20, SW - 96, 28, title, 24, txt, TextAlignmentOptions.Left, true);
        if (sub != "") Label("hbs", 74, 50, SW - 96, 16, sub, 12, new Color(txt.r, txt.g, txt.b, 0.6f), TextAlignmentOptions.Left, false);
    }

    void RoomProps(string m)
    {
        float fl = SH - 262f;
        switch (m)
        {
            case "talim":
                Panel("pboard", 24, fl - 250, SW - 48, 150, Hex("#1F6B3A"));
                Label("pb1", 44, fl - 232, 160, 20, "1 2 3 4 5", 15, new Color(1, 1, 1, 0.9f), TextAlignmentOptions.Left, true);
                Label("pb2", SW - 120, fl - 232, 90, 20, "A B C", 15, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Right, true);
                Rect2("pk1", 16, fl - 64, 46, 64, Hex("#E4573F"));
                Rect2("pk2", 66, fl - 52, 36, 52, Hex("#3B9BF0"));
                Circle("pglobe", SW - 76, fl - 62, 60, Hex("#F6ECD8"));
                break;
            case "oshxona":
                Panel("pc", 16, fl - 96, SW - 32, 96, Hex("#E9D7B6"));
                Rect2("pc2", 16, fl - 96, SW - 32, 10, Hex("#B5794A"));
                Panel("pfr", SW - 92, fl - 170, 74, 74, Hex("#F6ECD8"));
                Circle("ppot", 40, fl - 60, 44, Hex("#C43F2C"));
                break;
            case "yotoq":
                Panel("pbed", 16, fl - 90, SW - 120, 90, Hex("#F4EAD8"));
                Rect2("ppil", 30, fl - 78, 80, 34, Color.white);
                Panel("pwin", SW - 96, fl - 210, 80, 96, Hex("#0B1026"));
                Circle("pmoon", SW - 78, fl - 196, 34, Hex("#FFD43C"));
                break;
            case "oyinlar":
                Rect2("pole", 30, fl - 100, 10, 100, Hex("#8A5C31"));
                Rect2("pbar", 30, fl - 100, 120, 10, Hex("#B5794A"));
                Rect2("psw", 60, fl - 56, 60, 10, Hex("#E4573F"));
                Panel("pslide", SW - 110, fl - 90, 90, 14, Hex("#3B9BF0"));
                Circle("pball", 40, fl - 44, 40, Hex("#FFCC00"));
                break;
            case "dokon":
                Rect2("paw", 16, fl - 200, SW - 32, 30, Hex("#16B341"));
                Panel("psh", 16, fl - 96, SW - 32, 96, Hex("#E9D7B6"));
                Rect2("pi1", 30, fl - 82, 44, 30, Hex("#E4573F"));
                Rect2("pi2", 84, fl - 82, 44, 30, Hex("#3B9BF0"));
                Rect2("pi3", 138, fl - 82, 44, 30, Hex("#FFCC00"));
                break;
            case "garderob":
                Panel("pward", 16, fl - 150, 104, 150, Hex("#A9743F"));
                Rect2("pwd", 24, fl - 140, 88, 130, Hex("#EFE2CB"));
                Panel("pmir", SW - 100, fl - 150, 84, 150, Color.white);
                break;
            case "hammom":
                Panel("ptub", 16, fl - 96, SW - 120, 96, Color.white);
                Panel("ptw", 26, fl - 82, SW - 150, 70, Hex("#8FD2F0"));
                Panel("psink", SW - 92, fl - 120, 72, 60, Color.white);
                break;
            case "upgxona":
                Panel("psc1", 20, fl - 90, 100, 66, Hex("#2C7FD4"));
                Panel("psc2", SW - 120, fl - 90, 100, 66, Hex("#5A5187"));
                break;
            default:
                Panel("pshelf", 18, fl - 120, 96, 120, Hex("#A9743F"));
                Rect2("psh1", 26, fl - 108, 80, 24, Hex("#EFE2CB"));
                Rect2("psh2", 26, fl - 74, 80, 24, Hex("#EFE2CB"));
                Rect2("psh3", 26, fl - 40, 80, 24, Hex("#EFE2CB"));
                Panel("ptv", SW - 116, fl - 116, 100, 100, Hex("#F6ECD8"));
                Rect2("ptvs", SW - 108, fl - 108, 84, 66, Hex("#BFE3F5"));
                break;
        }
    }

    void BuildYotoq()
    {
        GradBg("#39456F", "#20294A");
        Rect2("floor", 0, SH - 250, SW, 250, Hex("#4E3C2E"));
        HeaderBack("Yotoqxona", "kuch " + energy + "%", "home", Color.white);
        MapBtnAt(() => Show("menu"), Color.white);
        Circle("moon", SW - 74, 250, 50, Hex("#FFF0BE"));
        MomiqAt(SW / 2f, SH - 250, 1f);
        Panel("sheet", 0, SH - 150, SW, 150, Color.white);
        BigBtn(mood == "uyquda" ? "Uyg'otish" : "Uxlatish", 18, SH - 136, SW - 36, 50, mood == "uyquda" ? Hex("#FFCC00") : Hex("#5C6BA8"), mood == "uyquda" ? Hex("#3A3330") : Color.white, 16,
            () => { if (mood == "uyquda") React("xursand", "Xayrli tong! Kuchim to'ldi.", 1.6f); else React("uyquda", "Alla-yo, alla... shirin tush.", 0); });
        BigBtn("Ertak eshitish", 18, SH - 78, SW - 36, 48, Hex("#E8EAF2"), Hex("#3A3330"), 15,
            () => React("kulgan", "Bir bor ekan, bir yo'q ekan, momiq bir qo'zichoq bor ekan...", 2.6f));
    }

    void BuildKitob()
    {
        ImgBg("kitob");
        Zone(10, 42, 56, 52, () => Show("talim"));
        Zone(22, 100, SW - 44, 120, () => { fan = "matem"; Show("matem"); });
        Zone(22, 232, SW - 44, 120, () => Show("ingliz"));
        Zone(22, 364, SW - 44, 120, () => { fan = "tabiiy"; Show("matem"); });
        Zone(22, 496, SW - 44, 120, () => { fan = "savod"; Show("matem"); });
    }


    void BuildMatem()
    {
        ImgBg("matem");
        Zone(10, 42, 56, 52, () => Show("talim"));
        Zone(0, 130, SW, SH - 130, () => StartMashqLesson(fan));
    }


    void BuildFanJadval()
    {
        ImgBg("fanjadval");
        Zone(10, 42, 56, 52, () => Show("talim"));
    }


    void BuildMashq()
    {
        ImgBg("mashq");
        Zone(10, 42, 56, 52, () => Show("matem"));
    }


    void StartMashqLesson(string f)
    {
        fan = f; mq.Clear();
        for (int i = 0; i < 5; i++) mq.Add(GenQ(f));
        mqIdx = 0; mqResult = "";
        Show("mashq");
    }

    MQ GenQ(string f)
    {
        var q = new MQ(); q.dotCol = Hex("#C8452F");
        if (f == "ingliz")
        {
            var w = inglizSozlar[UnityEngine.Random.Range(0, inglizSozlar.Length)];
            var pool = new System.Collections.Generic.List<string>();
            foreach (var x in inglizSozlar) pool.Add(x.uz);
            int ci; q.opts = MakeStrOpts(w.uz, pool, out ci); q.correct = ci;
            q.prompt = "\"" + w.en + "\" - tarjimasi?";
        }
        else if (f == "savod")
        {
            var h = harflar[UnityEngine.Random.Range(0, harflar.Length)];
            var pool = new System.Collections.Generic.List<string>();
            foreach (var x in harflar) pool.Add(x.s);
            int ci; q.opts = MakeStrOpts(h.s, pool, out ci); q.correct = ci;
            q.prompt = "'" + h.h + "' harfi qaysi so'zda bor?";
        }
        else if (f == "tabiiy")
        {
            var r = ranglar[UnityEngine.Random.Range(0, ranglar.Length)];
            var pool = new System.Collections.Generic.List<string>();
            foreach (var x in ranglar) pool.Add(x.nom);
            int ci; q.opts = MakeStrOpts(r.nom, pool, out ci); q.correct = ci;
            q.prompt = r.nom + " rangni tanlang";
        }
        else
        {
            int sub = UnityEngine.Random.Range(0, 3);
            if (sub == 0)
            {
                int nn = UnityEngine.Random.Range(1, 11); int ci;
                q.opts = MakeNumOpts(nn, 1, 10, out ci); q.correct = ci; q.dots = nn; q.prompt = "Nechta?";
            }
            else if (sub == 1)
            {
                int a = UnityEngine.Random.Range(1, 10), b; do { b = UnityEngine.Random.Range(1, 10); } while (b == a);
                q.opts = new string[] { a.ToString(), b.ToString() }; q.correct = a > b ? 0 : 1; q.prompt = "Qaysi son katta?";
            }
            else
            {
                int st = UnityEngine.Random.Range(1, 7); int miss = st + 1; int ci;
                q.opts = MakeNumOpts(miss, 1, 10, out ci); q.correct = ci; q.prompt = st + ", ?, " + (st + 2) + " - yetishmagan son?";
            }
        }
        return q;
    }

    string[] MakeNumOpts(int correct, int lo, int hi, out int ci)
    {
        var set = new System.Collections.Generic.List<int> { correct };
        int guard = 0;
        while (set.Count < 4 && guard++ < 60) { int r = UnityEngine.Random.Range(lo, hi + 1); if (!set.Contains(r)) set.Add(r); }
        for (int i = set.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = set[i]; set[i] = set[j]; set[j] = t; }
        ci = set.IndexOf(correct);
        string[] o = new string[set.Count];
        for (int i = 0; i < set.Count; i++) o[i] = set[i].ToString();
        return o;
    }
    string[] MakeStrOpts(string correct, System.Collections.Generic.List<string> pool, out int ci)
    {
        var set = new System.Collections.Generic.List<string> { correct };
        int guard = 0;
        while (set.Count < 4 && guard++ < 60) { var r = pool[UnityEngine.Random.Range(0, pool.Count)]; if (!set.Contains(r)) set.Add(r); }
        for (int i = set.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = set[i]; set[i] = set[j]; set[j] = t; }
        ci = set.IndexOf(correct);
        return set.ToArray();
    }

    void AnswerMashq(int i)
    {
        var q = mq[mqIdx];
        if (i == q.correct)
        {
            statTogri++; coins += 2; AddFan(fan, 5); AddXp(3, null);
            if (mqIdx >= mq.Count - 1)
            {
                coins += 10; if (!organgan.Contains("m" + mq.Count)) organgan.Add("dars" + statTogri);
                mood = "kulgan"; msg = "Dars tugadi! +10 tanga"; mqResult = "Dars tugadi!"; mq.Clear(); Save();
                Refresh();
                StartCoroutine(Delay(1.3f, () => Show("matem")));
            }
            else { mqIdx++; mqResult = ""; mood = "kulgan"; Refresh(); }
        }
        else { statXato++; mqResult = "Yana urinib ko'ring"; mood = "xafa"; Refresh(); }
    }

    void BuildHamyon()
    {
        var content = BeginScroll(0);
        var rt = Node("bg", content, 0, 0, SW, 1384);
        rt.gameObject.AddComponent<Image>().sprite = UiSprite("hamyon");
        Zone(22, 372, 168, 56, () => Show("dokon"));
        EndScroll(1384);
        Zone(10, 42, 56, 52, () => Show("home"));
    }



    void BuildStiker()
    {
        ImgBg("stiker");
        Zone(10, 42, 56, 52, () => Show("yutuqlar"));
    }


    void BuildChop()
    {
        ImgBg("chop");
        Zone(10, 42, 56, 52, () => Show("yutuqlar"));
    }


    void BuildVazifalar()
    {
        ImgBg("vazifalar");
        Zone(10, 42, 56, 52, () => Show("home"));
    }


    void BuildRekordlar()
    {
        ImgBg("rekordlar");
        Zone(10, 42, 56, 52, () => Show("home"));
    }


    void BuildAnalitika()
    {
        ImgBg("analitika");
        Zone(10, 42, 56, 52, () => Show("yutuqlar"));
    }


    void BuildDiplom()
    {
        ImgBg("diplom");
        Zone(10, 42, 56, 52, () => Show("home"));
        Zone(24, SH - 90, SW - 48, 60, () => Show("chop"));
    }

    void BuildSertifikat()
    {
        ImgBg("sertifikat");
        Zone(10, 42, 56, 52, () => Show("yutuqlar"));
        Zone(24, SH - 90, SW - 48, 60, () => Show("chop"));
    }

    void CertScreen(string title, string back)
    {
        GradBg("#FFF6E4", "#FFF9EE");
        HeaderBack(title, "Momiq maktabi", back, Hex("#3A3330"));
        var card = Panel("cert", 24, 110, SW - 48, 340, Color.white);
        Rect2("certb", 34, 120, SW - 68, 320, new Color(0.89f, 0.62f, 0.09f, 0.12f));
        Label("cl", 24, 150, SW - 48, 18, "MOMIQ MAKTABI", 12, Hex("#C8452F"), TextAlignmentOptions.Center, true);
        Label("ct", 24, 180, SW - 48, 20, "Ushbu " + title.ToLower() + " egasi", 12, new Color(0.23f, 0.2f, 0.19f, 0.6f), TextAlignmentOptions.Center, false);
        Label("cn", 24, 210, SW - 48, 44, nom, 34, Hex("#3A3330"), TextAlignmentOptions.Center, true);
        Label("cd", 24, 270, SW - 48, 20, daraja + "-daraja muvaffaqiyatli yakunladi", 13, new Color(0.23f, 0.2f, 0.19f, 0.6f), TextAlignmentOptions.Center, false);
        MomiqAt(SW / 2f, 440, 0.5f);
        BigBtn("Chop etish", 24, 466, SW - 48, 52, Hex("#12A83A"), Color.white, 16, () => Show("chop"));
    }

    void BuildTartib()
    {
        ImgBg("tartib");
        Zone(10, 42, 56, 52, () => Show("oyinlar"));
    }

    bool IsPlaced(int n, System.Collections.Generic.List<int> sorted) { return sorted.IndexOf(n) < tartibIdx; }
    void NewTartib()
    {
        tartibSeq = new System.Collections.Generic.List<int>();
        var pool = new System.Collections.Generic.List<int>();
        for (int i = 1; i <= 9; i++) pool.Add(i);
        for (int k = 0; k < 4; k++) { int j = UnityEngine.Random.Range(0, pool.Count); tartibSeq.Add(pool[j]); pool.RemoveAt(j); }
        tartibIdx = 0;
    }
    void TapTartib(int n, System.Collections.Generic.List<int> sorted)
    {
        if (sorted.IndexOf(n) == tartibIdx)
        {
            tartibIdx++;
            if (tartibIdx >= tartibSeq.Count) { coins += 5; statTogri++; React("kulgan", "Barakalla! To'g'ri tartib!", 1.4f); NewTartib(); }
            Refresh();
        }
        else { statXato++; React("xafa", "Kichigidan boshla!", 1.2f); }
    }

    void MapBtn(Action a)
    {
        var b = Circle("mapbtn", 68, 22, 40, new Color(1, 0.98f, 0.94f, 0.94f));
        var g = Node("mapi", buildRoot, 78, 32, 20, 20);
        var gi = g.gameObject.AddComponent<Image>(); gi.sprite = triSp; gi.color = Hex("#3A3330"); gi.raycastTarget = false;
        Clickable(b.gameObject, a);
    }
    void MapBtnAt(Action a, Color stroke)
    {
        var b = Circle("mapbtn2", SW - 58, 22, 40, new Color(1, 1, 1, stroke.r > 0.9f ? 0.16f : 0.94f));
        var g = Node("mapi2", buildRoot, SW - 48, 32, 20, 20);
        var gi = g.gameObject.AddComponent<Image>(); gi.sprite = triSp; gi.color = stroke; gi.raycastTarget = false;
        Clickable(b.gameObject, a);
    }
    void Clickable(GameObject go, Action onClick)
    {
        var img = go.GetComponent<Image>();
        if (img == null) { img = go.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0); }
        img.raycastTarget = true;
        var b = go.GetComponent<Button>(); if (b == null) b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => { Haptic(); onClick(); });
    }

    // ================= SPRITE GENERATSIYA =================
    Sprite GenRounded(int size, int r)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = true;
                int dx = -1, dy = -1;
                if (x < r && y < r) { dx = r - x; dy = r - y; }
                else if (x >= size - r && y < r) { dx = x - (size - r - 1); dy = r - y; }
                else if (x < r && y >= size - r) { dx = r - x; dy = y - (size - r - 1); }
                else if (x >= size - r && y >= size - r) { dx = x - (size - r - 1); dy = y - (size - r - 1); }
                if (dx >= 0) inside = (dx * dx + dy * dy) <= r * r;
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }
    Sprite GenCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) / 2f, rad = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                byte a = (byte)(d <= rad - 1 ? 255 : d <= rad ? Mathf.RoundToInt(255 * (rad - d)) : 0);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ================= MANZARA / GRADIENT YORDAMCHILAR =================
    void GradBg(string top, string bot)
    {
        var rt = Node("bg", buildRoot, 0, 0, SW, SH);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Gradient(top, bot); img.type = Image.Type.Simple;
    }
    Image GradRect(string name, float x, float y, float w, float h, string top, string bot)
    {
        var rt = Node(name, buildRoot, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Gradient(top, bot); img.type = Image.Type.Simple; return img;
    }
    void Sun(float x, float y, float d)
    {
        var rt = Node("sun", buildRoot, x, y, d, d);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = glowSp; img.color = new Color(1f, 0.965f, 0.84f, 0.85f); img.raycastTarget = false;
    }
    void Cloud(float x, float y, float sz)
    {
        Circle("cl", x, y + sz * 0.3f, sz * 0.9f, new Color(1, 1, 1, 0.75f)).raycastTarget = false;
        Circle("cl2", x + sz * 0.55f, y, sz * 0.75f, new Color(1, 1, 1, 0.7f)).raycastTarget = false;
        Circle("cl3", x + sz * 1.05f, y + sz * 0.35f, sz * 0.6f, new Color(1, 1, 1, 0.65f)).raycastTarget = false;
    }
    void Mountain(float x, float y, float w, float h, string col)
    {
        var rt = Node("mt", buildRoot, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = triSp; img.type = Image.Type.Simple; img.color = Hex(col); img.raycastTarget = false;
    }
    void CoinLabel(string nm, float x, float y, int val, float size, Color txt)
    {
        Circle(nm + "_c", x, y, size, Hex("#E9A62B")).raycastTarget = false;
        Label(nm + "_t", x + size + 6, y, 140, size + 4, val.ToString(), size, txt, TextAlignmentOptions.Left, true);
    }
    void GearBtn(float x, float y, float d, Action a)
    {
        var img = Circle("gear", x, y, d, new Color(0.07f, 0.19f, 0.23f, 0.06f));
        var g = Node("gi", buildRoot, x + d * 0.22f, y + d * 0.22f, d * 0.56f, d * 0.56f);
        var gi = g.gameObject.AddComponent<Image>(); gi.sprite = gearSp; gi.color = Hex("#12303B"); gi.raycastTarget = false;
        Clickable(img.gameObject, a);
    }

    Sprite Gradient(string top, string bot)
    {
        string k = top + "|" + bot;
        if (gradCache.TryGetValue(k, out var sp)) return sp;
        int h = 128; var tex = new Texture2D(1, h, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        Color a = Hex(top), b = Hex(bot);
        for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(b, a, y / (float)(h - 1)));
        tex.Apply();
        sp = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
        gradCache[k] = sp; return sp;
    }
    Sprite GenGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size]; float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float aa = Mathf.Clamp01(1f - d); aa *= aa;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(aa * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    Sprite GenTriangle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size]; float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float ty = y / (size - 1f);
                float halfW = (1f - ty) * (size / 2f);
                bool inside = Mathf.Abs(x - c) <= halfW;
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    Sprite GenGear(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size]; float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c; float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float outer = c * 0.82f + Mathf.Cos(ang * 8f) * (c * 0.12f);
                bool solid = dist <= outer && dist > c * 0.34f;
                px[y * size + x] = solid ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ================= SAQLASH =================
    [Serializable]
    class SaveData
    {
        public int coins, hunger, joy, energy, clean, kun, xp, daraja, harfIndex, inglizIndex;
        public bool sound, music, titrash, nazorat, giftTaken, nomSet;
        public string nom;
        public List<string> organgan, olingan, ownedKeys, wearKeys;
        public List<string> fanKeys; public List<int> fanVals;
        public List<string> hisobKeys; public List<int> hisobVals;
    }
    void Save()
    {
        var d = new SaveData
        {
            coins = coins, hunger = hunger, joy = joy, energy = energy, clean = clean, kun = kun,
            xp = xp, daraja = daraja, harfIndex = harfIndex, inglizIndex = inglizIndex,
            sound = sound, music = music, titrash = titrash, nazorat = nazorat, giftTaken = giftTaken, nomSet = nomSet, nom = nom,
            organgan = new List<string>(organgan), olingan = new List<string>(olingan),
            ownedKeys = new List<string>(), wearKeys = new List<string>(),
            fanKeys = new List<string>(fanlar.Keys), fanVals = new List<int>(fanlar.Values),
            hisobKeys = new List<string>(hisob.Keys), hisobVals = new List<int>(hisob.Values),
        };
        foreach (var kv in owned) if (kv.Value) d.ownedKeys.Add(kv.Key);
        foreach (var kv in wear) if (kv.Value) d.wearKeys.Add(kv.Key);
        PlayerPrefs.SetString("momiq_save", JsonUtility.ToJson(d));
        PlayerPrefs.Save();
    }
    void LoadState()
    {
        if (!PlayerPrefs.HasKey("momiq_save")) return;
        try
        {
            var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("momiq_save"));
            if (d == null) return;
            coins = d.coins; hunger = d.hunger; joy = d.joy; energy = d.energy; clean = d.clean; kun = d.kun;
            xp = d.xp; daraja = d.daraja; harfIndex = d.harfIndex; inglizIndex = d.inglizIndex;
            sound = d.sound; music = d.music; titrash = d.titrash; nazorat = d.nazorat; giftTaken = d.giftTaken; nomSet = d.nomSet;
            if (!string.IsNullOrEmpty(d.nom)) nom = d.nom;
            organgan.Clear(); if (d.organgan != null) organgan.AddRange(d.organgan);
            olingan.Clear(); if (d.olingan != null) olingan.AddRange(d.olingan);
            var keys = new List<string>(owned.Keys); foreach (var k in keys) owned[k] = d.ownedKeys != null && d.ownedKeys.Contains(k);
            keys = new List<string>(wear.Keys); foreach (var k in keys) wear[k] = d.wearKeys != null && d.wearKeys.Contains(k);
            if (d.fanKeys != null) for (int i = 0; i < d.fanKeys.Count && i < d.fanVals.Count; i++) fanlar[d.fanKeys[i]] = d.fanVals[i];
            if (d.hisobKeys != null) for (int i = 0; i < d.hisobKeys.Count && i < d.hisobVals.Count; i++) hisob[d.hisobKeys[i]] = d.hisobVals[i];
        }
        catch { }
    }

    // ================= TELEGRAM (WebGL) =================
    void TgInit()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { Application.ExternalEval("try{if(window.Telegram&&Telegram.WebApp){Telegram.WebApp.ready();Telegram.WebApp.expand();}}catch(e){}"); } catch {}
#endif
    }
    void Haptic()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (titrash) { try { Application.ExternalEval("try{if(window.Telegram&&Telegram.WebApp&&Telegram.WebApp.HapticFeedback){Telegram.WebApp.HapticFeedback.impactOccurred('light');}}catch(e){}"); } catch {} }
#endif
    }

    // ---- ranglar ----
    public static Color Hex(string h)
    {
        h = h.Replace("#", "");
        float a = 1f;
        int r = Convert.ToInt32(h.Substring(0, 2), 16);
        int g = Convert.ToInt32(h.Substring(2, 2), 16);
        int b = Convert.ToInt32(h.Substring(4, 2), 16);
        if (h.Length >= 8) a = Convert.ToInt32(h.Substring(6, 2), 16) / 255f;
        return new Color(r / 255f, g / 255f, b / 255f, a);
    }
}

/// <summary>Qatlamli qo'zichoq personaji — har qism alohida Image, kod bilan animatsiya.</summary>
public class MomiqRig : MonoBehaviour
{
    MomiqController ctrl;
    RectTransform group, headGroup;
    RectTransform earL, earR, eyeL, eyeR, mouth, armR, armL;
    string mood = "xursand";
    bool faceMode = false;
    float t;

    static readonly Dictionary<string, string> FaceMap = new Dictionary<string, string>
    {
        { "kulgan", "bosh-21" }, { "xafa", "bosh-24" }, { "uyquda", "bosh-23" },
        { "ovqat", "bosh-13" }, { "hayron", "bosh-12" }, { "yuvinish", "bosh-14" }
    };

    public void Init(MomiqController c, float scale, string m)
    {
        ctrl = c;
        mood = string.IsNullOrEmpty(m) ? "xursand" : m;
        faceMode = FaceMap.ContainsKey(mood);
        var rt = GetComponent<RectTransform>();

        group = Sub(rt);

        Full(group, "leg_l"); Full(group, "leg_r"); Full(group, "body");
        armL = Full(group, "arm_l"); armL.pivot = new Vector2(0.391f, 0.474f);
        armR = Full(group, "arm_r"); armR.pivot = new Vector2(0.638f, 0.593f);

        headGroup = FullRT(group, "head_group");
        headGroup.pivot = new Vector2(0.537f, 0.432f);

        if (!faceMode)
        {
            Full(headGroup, "head");
            earL = Full(headGroup, "ear_l"); earL.pivot = new Vector2(0.292f, 0.625f);
            earR = Full(headGroup, "ear_r"); earR.pivot = new Vector2(0.674f, 0.689f);
            eyeL = Full(headGroup, "eye_l"); eyeL.pivot = new Vector2(0.387f, 0.648f);
            eyeR = Full(headGroup, "eye_r"); eyeR.pivot = new Vector2(0.577f, 0.684f);
            mouth = Full(headGroup, "mouth"); mouth.pivot = new Vector2(0.49f, 0.585f);
        }
        else
        {
            TL(headGroup, 0.11f * 230f, 0f, 0.76f * 230f, 0.56f * 230f, "parts/" + FaceMap[mood]);
            float dw = 0.47f * 230f, dh = dw * 96f / 180f;
            TL(headGroup, 0.25f * 230f, -0.01f * 230f, dw, dh, "parts/doppi");
        }

        transform.localScale = new Vector3(scale, scale, 1f);
    }

    RectTransform Sub(Transform parent)
    {
        var go = new GameObject("group", typeof(RectTransform));
        var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = new Vector2(0.5f, 0f); r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(230, 230);
        return r;
    }
    RectTransform FullRT(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return r;
    }
    RectTransform Full(Transform parent, string part)
    {
        var r = FullRT(parent, part);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = ctrl.Part(part); img.preserveAspect = true; img.raycastTarget = false;
        return r;
    }
    RectTransform TL(Transform parent, float x, float y, float w, float h, string part)
    {
        var go = new GameObject(part, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = ctrl.Part(part); img.preserveAspect = true; img.raycastTarget = false;
        return r;
    }

    public void SetMood(string m, bool hearts, bool bubbles) { }

    void Update()
    {
        t += Time.deltaTime;
        Vector2 pos = Vector2.zero; float sc = 1f, rot = 0f;
        switch (mood)
        {
            case "kulgan": sc = 1f + Mathf.Abs(Mathf.Sin(t * 6f)) * 0.035f; rot = Mathf.Sin(t * 6f) * 2f; pos.y = Mathf.Abs(Mathf.Sin(t * 6f)) * 9f; break;
            case "xafa": pos.y = -7f; sc = 0.98f; break;
            case "uyquda": pos.y = Mathf.Sin(t * 1.4f) * 4f; break;
            case "sakra": pos.y = Mathf.Abs(Mathf.Sin(t * 4f)) * 32f; break;
            case "ovqat": case "yuvinish": pos.x = Mathf.Sin(t * 30f) * 4f; break;
            default: pos.y = Mathf.Sin(t * 1.8f) * 3f; sc = 1f + Mathf.Sin(t * 1.8f) * 0.02f; break;
        }
        if (group) { group.anchoredPosition = pos; group.localScale = new Vector3(sc, sc, 1f); group.localRotation = Quaternion.Euler(0, 0, rot); }

        if (headGroup) headGroup.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.5f) * 1.2f);

        if (armR)
        {
            float wave = (mood == "kulgan" || mood == "sakra") ? (8f + Mathf.Sin(t * 8f) * 14f) : Mathf.Sin(t * 1.6f) * 5f;
            armR.localRotation = Quaternion.Euler(0, 0, wave);
        }
        if (armL) armL.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f + 0.5f) * 4f);

        if (!faceMode)
        {
            if (earL) earL.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.85f) * 5f);
            if (earR) earR.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(t * 1.85f + 0.35f) * 5f);
            float blink = Mathf.Repeat(t, 5.2f) > 4.95f ? 0.06f : 1f;
            if (eyeL) eyeL.localScale = new Vector3(1, blink, 1);
            if (eyeR) eyeR.localScale = new Vector3(1, blink, 1);
            if (mouth)
            {
                float my = (mood == "kulgan" || mood == "ovqat") ? (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 10f))) : 1f;
                mouth.localScale = new Vector3(1, my, 1);
            }
        }
    }
}
