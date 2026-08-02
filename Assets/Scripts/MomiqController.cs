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
        GradBg("#BFE3F5", "#94BE6A");
        Sun(-50, -60, 200);
        Cloud(24, 64, 50);
        Mountain(-60, 96, 230, 86, "#8FAEB8");
        Mountain(SW - 150, 104, 210, 74, "#9BB8C0");

        // yuqori panel
        var coin = Panel("coin", 16, 52, 104, 42, Hex("#FFF3C2"));
        Circle("coinc", 21, 57, 30, Hex("#E29B18"));
        Label("coinssa", 21, 64, 30, 14, "SSA", 8, Hex("#8A5C00"), TextAlignmentOptions.Center, true);
        Label("coinv", 56, 60, 60, 22, coins.ToString(), 16, Hex("#8A6600"), TextAlignmentOptions.Left, true);
        Clickable(coin.gameObject, () => Show("hamyon"));
        var rec = Circle("rec", SW - 106, 52, 42, new Color(1, 1, 1, 0.94f));
        Circle("recm", SW - 106 + 11, 63, 20, Hex("#E9A62B"));
        Clickable(rec.gameObject, () => Show("yutuqlar"));
        GearBtn(SW - 58, 52, 42, () => Show("sozlamalar"));

        // sarlavha
        Panel("chip", SW / 2f - 82, 104, 164, 30, new Color(1, 1, 1, 0.9f));
        Label("salom", SW / 2f - 82, 111, 164, 16, (bola != "" ? "SALOM, " + bola.ToUpper() : "SALOM, DO'STIM"), 11, Hex("#0B7A28"), TextAlignmentOptions.Center, true);
        Label("mtitle", 0, 136, SW, 42, "Momiq maskani", 34, Color.white, TextAlignmentOptions.Center, true);

        // xarita binolari
        float mapTop = 172f, mapH = SH - 172f - 152f;
        string[] bn = { "Momiq uyi", "Sinfxona", "Oshxona", "Yotoqxona", "O'yin maydoni", "Do'kon", "Garderob", "Yuvinish", "UPG xonasi" };
        string[] bek = { "home", "talim", "oshxona", "yotoq", "oyinlar", "dokon", "garderob", "hammom", "upgxona" };
        float[] bx = { 26, 74, 74, 26, 26, 74, 74, 26, 50 };
        float[] by = { 1, 1, 24, 24, 46, 46, 68, 68, 90 };
        string[] bfon = { "#FFE3B0", "#CFE8FB", "#FFEFC7", "#DDE3F2", "#DFF7DF", "#FFF3CE", "#F6E4F0", "#D6EEF7", "#E4E2F6" };
        string[] bbel = { "#E4573F", "#2C7FD4", "#FFB800", "#5C6BA8", "#17C41C", "#E29B18", "#B06BA8", "#3B9BF0", "#6B5CC4" };
        string[] bsoy = { "#B58200", "#1F5FA8", "#B58200", "#3F4C82", "#0B7A28", "#8A6600", "#8A4F84", "#1F5FA8", "#463A9E" };
        for (int i = 0; i < 9; i++)
        {
            float cx = bx[i] / 100f * SW;
            float cy = mapTop + by[i] / 100f * mapH;
            float x = cx - 38;
            var card = Panel("b" + i, x, cy, 76, 76, Hex(bfon[i]));
            var roof = Node("r" + i, buildRoot, x, cy, 76, 26);
            var ri = roof.gameObject.AddComponent<Image>(); ri.sprite = triSp; ri.color = Hex(bbel[i]); ri.raycastTarget = false;
            Circle("bg" + i, x - 4, cy - 6, 22, Color.white);
            Label("bnr" + i, x - 4, cy - 1, 22, 14, (i + 1).ToString(), 10, Hex(bsoy[i]), TextAlignmentOptions.Center, true);
            Panel("bl" + i, cx - 46, cy + 80, 92, 22, new Color(1, 1, 1, 0.94f));
            Label("blt" + i, cx - 46, cy + 84, 92, 16, bn[i], 11, Hex("#3A3330"), TextAlignmentOptions.Center, true);
            string ek = bek[i];
            Clickable(card.gameObject, () => MenuGo(ek));
        }

        MomiqAt(0.26f * SW, mapTop + 0.01f * mapH + 100f, 0.42f);

        // pastki panel
        Panel("bb", 16, SH - 72, SW - 32, 60, new Color(1, 0.99f, 0.96f, 0.96f));
        Circle("lvl", 26, SH - 64, 44, Hex("#EDF7EE"));
        Label("lvlv", 26, SH - 55, 44, 20, daraja.ToString(), 16, Hex("#0B7A28"), TextAlignmentOptions.Center, true);
        Label("nm", 82, SH - 66, SW - 210, 16, nom, 13, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Rect2("xpbg", 82, SH - 48, SW - 210, 5, new Color(0.23f, 0.2f, 0.19f, 0.12f));
        Rect2("xp", 82, SH - 48, (SW - 210) * Mathf.Clamp01(xp / 100f), 5, Hex("#12A83A"));
        BigBtn("O'ynash", SW - 118, SH - 62, 100, 44, Hex("#12A83A"), Color.white, 16, () => { if (nomSet) Show("home"); else Show("ism"); });
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
        GradBg("#DDEAEC", "#FBF3E4");
        Label("k", 0, 96, SW, 18, "TANISHUV", 12, Hex("#C8452F"), TextAlignmentOptions.Center, true);
        Label("q", 34, 118, SW - 68, 80, "Qo'zichog'imizga qanday ism qo'yamiz?", 26, Hex("#12303B"), TextAlignmentOptions.Center, true);
        MomiqAt(SW / 2f, 370, 1.06f);

        string[] names = { "Momiq", "Oqquloq", "Bo'ljon", "Jonivor" };
        for (int i = 0; i < names.Length; i++)
        {
            string nm = names[i];
            float x = 26 + (i % 2) * ((SW - 52) / 2f + 12);
            float y = 534 + (i / 2) * 74;
            bool sel = nom == nm;
            var b = Panel("nm" + i, x, y, (SW - 52 - 12) / 2f, 62, sel ? Hex("#C8452F") : Hex("#FFF8EA"));
            Label("t" + i, x, y + 20, (SW - 52 - 12) / 2f, 24, nm, 18, sel ? Hex("#FFF6E6") : Hex("#12303B"), TextAlignmentOptions.Center, true);
            Clickable(b.gameObject, () => { nom = nm; Refresh(); });
        }
        BigBtn("Davom etish", 26, 704, SW - 52, 66, Hex("#C8452F"), Hex("#FFF6E6"), 22, () => { nomSet = true; msg = "Mening ismim " + nom + ". Kel, o'ynaymiz!"; Save(); Show("home"); });
    }

    void BuildHome()
    {
        // xona fon (makon bo'yicha)
        string top, bot, floor; bool dark = false;
        switch (makon)
        {
            case "talim": top = "#FBEFD8"; bot = "#CFAB78"; floor = "#B08355"; break;
            case "oshxona": top = "#FFF6E4"; bot = "#C09A6C"; floor = "#C09A6C"; break;
            case "yotoq": top = "#39456F"; bot = "#20294A"; floor = "#4E3C2E"; dark = true; break;
            case "oyinlar": top = "#8FD8F5"; bot = "#8FBE5C"; floor = "#6B9942"; break;
            case "dokon": top = "#EAF6EC"; bot = "#AC8558"; floor = "#AC8558"; break;
            case "garderob": top = "#F3EAFA"; bot = "#AC8558"; floor = "#AC8558"; break;
            case "hammom": top = "#E6F5FB"; bot = "#96BACD"; floor = "#A9CBDC"; break;
            case "upgxona": top = "#2A2A46"; bot = "#2E2942"; floor = "#332E4A"; dark = true; break;
            default: top = "#FFF1DC"; bot = "#C09A6C"; floor = "#C09A6C"; break;
        }
        GradBg(top, bot);
        Rect2("floor", 0, SH - 262, SW, 262, Hex(floor));
        Rect2("floorline", 0, SH - 262, SW, 14, Hex(dark ? "#57402C" : "#A9743F"));
        RoomProps(makon);

        // soya + Momiq (bosilsa erkalash)
        Circle("mshadow", SW / 2f - 118, SH - 214, 236, new Color(0.18f, 0.27f, 0.11f, 0.32f));
        MomiqAt(SW / 2f, SH - 202, 1f);
        if (rig != null) Clickable(rig.gameObject, () => { Bump(ref joy, 6); hearts = true; AddXp(6, "erkalash"); React("kulgan", "Ie-he-he, yoqimli!", 1.5f); });

        // yuqori panel
        var panel = Panel("top", 18, 52, SW - 36, 152, new Color(1, 0.99f, 0.96f, 0.96f));
        var coin = Panel("coin", 26, 60, 96, 36, Hex("#FFF3C2"));
        Circle("coinc", 30, 63, 28, Hex("#E29B18"));
        Label("coinv", 62, 66, 60, 22, coins.ToString(), 15, Hex("#8A6600"), TextAlignmentOptions.Left, true);
        Clickable(coin.gameObject, () => Show("hamyon"));
        Label("nm", 132, 60, SW - 220, 16, nom, 15, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Label("yosh", 132, 78, SW - 220, 12, "5-6 YOSH", 9, new Color(0.21f, 0.2f, 0.19f, 0.42f), TextAlignmentOptions.Left, true);
        Rect2("xpbg", 132, 92, SW - 220, 5, new Color(0.21f, 0.2f, 0.19f, 0.11f));
        Rect2("xp", 132, 92, (SW - 220) * Mathf.Clamp01(xp / 100f), 5, Hex("#28D62C"));
        var tunb = Circle("tunb", SW - 62, 60, 36, mood == "uyquda" ? Hex("#2A3358") : Hex("#FFE9A8"));
        Circle("tuni", SW - 62 + 9, 69, 17, mood == "uyquda" ? Hex("#FBFBFB") : Hex("#FFB800"));
        Clickable(tunb.gameObject, () => { if (mood == "uyquda") React("xursand", "Xayrli tong!", 1.5f); else React("uyquda", "Alla-yo... uxlayapman.", 0); });

        // holat chipi
        Panel("chip", 26, 104, SW - 52, 30, new Color(0.07f, 0.66f, 0.23f, 0.1f));
        Circle("chipd", 34, 111, 10, Hex("#12A83A"));
        Label("chipn", 50, 108, 150, 20, JoyNomi(makon), 12, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Label("chiph", SW - 210, 108, 176, 20, JoyHolat(makon), 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Right, true);

        // 4 parvarish bari
        string[] pn = { "QORIN", "KAYFIYAT", "QUVVAT", "TOZALIK" };
        int[] pv = { hunger, joy, energy, clean };
        string[] pc = { "#E29B18", "#E4573F", "#12A83A", "#3B9BF0" };
        for (int i = 0; i < 4; i++)
        {
            float bw = (SW - 52 - 18) / 4f;
            float x = 26 + i * (bw + 6);
            Panel("pp" + i, x, 142, bw, 52, new Color(0.23f, 0.2f, 0.19f, 0.05f));
            Circle("pd" + i, x + 7, 150, 9, Hex(pc[i]));
            Label("pl" + i, x + 20, 150, bw - 22, 10, pn[i], 7, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
            Rect2("pbg" + i, x + 7, 170, bw - 14, 5, new Color(0.23f, 0.2f, 0.19f, 0.12f));
            Rect2("pb" + i, x + 7, 170, (bw - 14) * Mathf.Clamp01(pv[i] / 100f), 5, Hex(pc[i]));
        }

        // gap qutisi
        Panel("bubble", SW / 2f - 132, 250, 264, 62, Color.white);
        Label("msg", SW / 2f - 122, 258, 244, 46, msg, 15, Hex("#3A3330"), TextAlignmentOptions.Center, true);

        // pastki varaq
        Panel("sheet", 0, SH - 168, SW, 168, Color.white);
        float by = SH - 150;
        if (makon != "home")
        {
            BigBtn(JoyTugma(makon), 18, by, SW - 36, 50, makon == "talim" ? Hex("#0B7A28") : Hex("#E29B18"), makon == "talim" ? Color.white : Hex("#3A3330"), 16, () => JoyOchish(makon));
            by += 59;
        }
        else
        {
            BigBtn("O'rganamiz", 18, by, SW - 36, 50, Hex("#0B7A28"), Color.white, 16, () => { makon = "talim"; Show("home"); });
            by += 59;
        }
        // Vazifa + Xarita
        var q = Panel("q", 18, by, 66, 48, new Color(0.16f, 0.84f, 0.17f, 0.1f));
        Label("qv", 18, by + 8, 66, 16, "0/3", 13, Hex("#0B7A28"), TextAlignmentOptions.Center, true);
        Label("ql", 18, by + 26, 66, 12, "VAZIFA", 7, new Color(0.23f, 0.2f, 0.19f, 0.45f), TextAlignmentOptions.Center, true);
        Clickable(q.gameObject, () => Show("yutuqlar"));
        var mapb = Panel("mapb", 92, by, SW - 110, 48, Hex("#F3F4F0"));
        Label("mapl", 92, by + 14, SW - 110, 20, "Xarita", 15, Hex("#3A3330"), TextAlignmentOptions.Center, true);
        Clickable(mapb.gameObject, () => Show("menu"));

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
        BgFlat(Hex("#FBF3E4"));
        Back(() => Show("home"), Hex("#12303B"));
        Label("t", 70, 24, 200, 30, "Sozlamalar", 26, Hex("#12303B"), TextAlignmentOptions.Left, true);
        string[] nm = { "Ovoz effektlari", "Musiqa", "Titrash", "Vaqt cheklovi" };
        string[] iz = { "Ma'rash, kulgi, qadam tovushi", "Yumshoq o'zbek kuyi", "Tegilganda telefon titraydi", "Kuniga 20 daqiqa" };
        Func<int, bool> get = i => i == 0 ? sound : i == 1 ? music : i == 2 ? titrash : nazorat;
        for (int i = 0; i < 4; i++)
        {
            float y = 110 + i * 78;
            var row = Panel("s" + i, 22, y, SW - 44, 66, Hex("#FFFAF0"));
            Label("sn" + i, 38, y + 12, 220, 20, nm[i], 17, Hex("#12303B"), TextAlignmentOptions.Left, true);
            Label("si" + i, 38, y + 36, 260, 16, iz[i], 11, new Color(0.07f, 0.19f, 0.23f, 0.5f), TextAlignmentOptions.Left, false);
            bool on = get(i);
            var track = Panel("tr" + i, SW - 44 - 52 - 4, y + 18, 52, 30, on ? Hex("#7FA650") : new Color(0.07f, 0.19f, 0.23f, 0.2f));
            var knob = Circle("kn" + i, (SW - 44 - 52 - 4) + (on ? 25 : 3), y + 21, 24, Color.white);
            int idx = i;
            Clickable(row.gameObject, () => { ToggleSetting(idx); Refresh(); });
        }
        BigBtn("Bosh menyuga", 22, SH - 90, SW - 44, 52, Hex("#FFF8EA"), Hex("#12303B"), 15, () => Show("menu"));
    }

    void BuildTalim()
    {
        GradBg("#E4EEE6", "#FFF9EE");
        Back(() => Show("home"), Hex("#3A3330"));
        var mb = Circle("map", 68, 22, 40, new Color(1, 0.98f, 0.94f, 0.94f));
        Label("mapi", 68, 30, 40, 24, "M", 16, Hex("#3A3330"), TextAlignmentOptions.Center, true);
        Clickable(mb.gameObject, () => Show("menu"));
        Label("t", 116, 20, SW - 140, 28, "O'rganamiz", 26, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Label("ts", 116, 50, SW - 140, 16, nom + " bilan · " + organgan.Count + " ta o'rganildi", 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Left, false);

        BeginScroll(80);
        Label("yg", 22, 4, 200, 16, "YOSH GURUHI", 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        Panel("ygc", 22, 24, 90, 44, Hex("#12A83A"));
        Label("ygt", 22, 32, 90, 18, "5-6 yosh", 14, Color.white, TextAlignmentOptions.Center, true);
        Label("ygl", 22, 50, 90, 12, "TANLANDI", 8, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center, true);
        Panel("ygi", 122, 24, SW - 144, 44, new Color(0.16f, 0.84f, 0.17f, 0.08f));
        Label("ygit", 132, 30, SW - 164, 32, "Hozircha 5-6 yosh darslari tayyor. Qolganlari qo'shiladi.", 10, new Color(0.23f, 0.2f, 0.19f, 0.6f), TextAlignmentOptions.Left, false);

        Label("asf", 22, 84, 260, 16, "ASOSIY FANLAR · 4 TA KITOB", 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        Card("m", 106, 150, "Quvnoq matematika", "Mashq daftari · 0 dan 10 gacha", Hex("#3A3330"), Color.white, () => { fan = "matem"; Show("matem"); });
        Card("e", 266, 150, "Ingliz tili", "So'z va tarjimasi", Hex("#3B9BF0"), Color.white, () => Show("ingliz"));
        Card("tb", 426, 150, "Tabiiy fan", "Tabiat va atrof-olam", Hex("#28D62C"), Color.white, () => { fan = "tabiiy"; Show("matem"); });
        Card("sv", 586, 150, "Savodxonlik", "Harf, bo'g'in va so'z", Hex("#FFB800"), Hex("#3A3330"), () => { fan = "savod"; Show("matem"); });
        var rr = Panel("rr", 22, 746, SW - 44, 60, Color.white);
        Label("rrt", 42, 758, SW - 90, 20, "Fanlar reytingi", 16, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Label("rrs", 42, 780, SW - 90, 14, "Har fanda kim oldinda", 11, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, false);
        Clickable(rr.gameObject, () => Show("fanjadval"));

        Label("qm", 22, 822, 260, 16, "QO'SHIMCHA MASHQLAR", 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        Card("hf", 844, 150, "Harflar", "Harfni bosing — Momiq aytadi", Color.white, Hex("#3A3330"), () => Show("harf"));
        Card("rg", 1004, 150, "Ranglar", "To'g'ri rangni toping", Color.white, Hex("#3A3330"), () => NewColor());
        Card("mo", 1164, 150, "Mini-o'yinlar", "Xotira, olma tutish", Hex("#F3E8F1"), Hex("#3A3330"), () => Show("oyinlar"));
        EndScroll(1340);
    }

    void BuildSanoq()
    {
        BgFlat(Hex("#0E2731"));
        Rect2("light", 0, 300, SW, SH - 300, Hex("#FBF3E4"));
        Back(() => Show("talim"), Hex("#FFF6E6"));
        Label("t", 0, 24, SW, 28, "Nechta olma?", 22, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);

        var box = Panel("box", 22, 108, SW - 44, 170, new Color(1, 0.96f, 0.9f, 0.08f));
        for (int i = 0; i < son; i++)
        {
            float x = 22 + 30 + (i % 4) * 68;
            float y = 130 + (i / 4) * 64;
            Circle("ap" + i, x, y, 54, Hex("#B23F2E"));
        }
        for (int i = 0; i < sanoqVariant.Count; i++)
        {
            float bw = (SW - 44 - 24) / 3f;
            float x = 22 + i * (bw + 12);
            int n = sanoqVariant[i];
            var b = Panel("v" + i, x, 310, bw, 96, Hex("#FFF8EA"));
            Label("vt" + i, x, 332, bw, 50, n.ToString(), 44, Hex("#12303B"), TextAlignmentOptions.Center, true);
            Clickable(b.gameObject, () => AnswerCount(n));
        }
        if (natija != "") Label("nat", 0, 420, SW, 26, natija, 20, Hex("#7FA650"), TextAlignmentOptions.Center, true);
        MomiqAt(SW / 2f, 700, 0.86f);
    }

    void BuildRang()
    {
        GradBg("#E4EEE6", "#FBF3E4");
        Back(() => Show("talim"), Hex("#12303B"));
        Label("t", 0, 24, SW, 28, "Ranglar", 22, Hex("#12303B"), TextAlignmentOptions.Center, true);
        var q = Panel("q", 22, 102, SW - 44, 80, Hex("#FFFAF0"));
        Label("qs", 22, 112, SW - 44, 16, "Momiq so'radi:", 13, new Color(0.07f, 0.19f, 0.23f, 0.55f), TextAlignmentOptions.Center, false);
        Label("qt", 22, 132, SW - 44, 34, target + " rangni top", 28, Hex("#12303B"), TextAlignmentOptions.Center, true);
        for (int i = 0; i < ranglar.Length; i++)
        {
            float cw = (SW - 44 - 14) / 2f;
            float x = 22 + (i % 2) * (cw + 14);
            float y = 226 + (i / 2) * 134;
            var r = ranglar[i];
            var b = Panel("r" + i, x, y, cw, 110, r.kod);
            Label("rn" + i, x + 12, y + 84, cw - 24, 20, r.nom, 16, Hex("#FFF6E6"), TextAlignmentOptions.Left, true);
            Clickable(b.gameObject, () => AnswerColor(r.nom));
        }
        if (natija != "") Label("nat", 0, 494, SW, 26, natija, 20, Hex("#7FA650"), TextAlignmentOptions.Center, true);
        MomiqAt(SW / 2f, 730, 0.8f);
    }

    void BuildIngliz()
    {
        GradBg("#E4EEE6", "#FBF3E4");
        Back(() => Show("talim"), Hex("#12303B"));
        Label("t", 0, 24, SW, 28, "Ingliz tili", 22, Hex("#12303B"), TextAlignmentOptions.Center, true);
        var card = Panel("card", 22, 100, SW - 44, 200, Hex("#1E7A8C"));
        Label("en", 46, 134, SW - 100, 50, inglizSozlar[inglizIndex].en, 46, Hex("#FFF6E6"), TextAlignmentOptions.Left, true);
        Label("uz", 46, 196, SW - 100, 30, inglizSozlar[inglizIndex].uz, 22, new Color(1, 0.96f, 0.9f, 0.75f), TextAlignmentOptions.Left, true);
        MomiqAt(SW - 70, 300, 0.6f);
        for (int i = 0; i < inglizSozlar.Length; i++)
        {
            float cw = (SW - 44 - 12) / 2f;
            float x = 22 + (i % 2) * (cw + 12);
            float y = 324 + (i / 2) * 76;
            bool sel = inglizIndex == i;
            var b = Panel("w" + i, x, y, cw, 64, sel ? Hex("#1E7A8C") : Hex("#FFF8EA"));
            Label("we" + i, x, y + 12, cw, 22, inglizSozlar[i].en, 18, sel ? Hex("#FFF6E6") : Hex("#12303B"), TextAlignmentOptions.Center, true);
            Label("wu" + i, x, y + 38, cw, 14, inglizSozlar[i].uz, 11, new Color(sel ? 1 : 0.07f, sel ? 0.96f : 0.19f, sel ? 0.9f : 0.23f, 0.7f), TextAlignmentOptions.Center, false);
            int idx = i;
            Clickable(b.gameObject, () => PickWord(idx));
        }
    }

    void BuildHarf()
    {
        GradBg("#E4EEE6", "#FBF3E4");
        Back(() => Show("talim"), Hex("#12303B"));
        Label("t", 0, 24, SW, 28, "Harflar", 22, Hex("#12303B"), TextAlignmentOptions.Center, true);
        Label("cnt", SW - 90, 26, 70, 24, organgan.Count + "/12", 14, Hex("#7FA650"), TextAlignmentOptions.Right, true);
        var card = Panel("card", 22, 100, SW - 44, 230, Hex("#FFFAF0"));
        Label("big", 42, 110, 160, 150, harflar[harfIndex].h, 120, harflar[harfIndex].r, TextAlignmentOptions.Left, true);
        Label("soz", 42, 300, SW - 100, 30, harflar[harfIndex].s, 28, Hex("#12303B"), TextAlignmentOptions.Left, true);
        MomiqAt(SW - 80, 330, 0.62f);
        for (int i = 0; i < harflar.Length; i++)
        {
            float cw = (SW - 44 - 30) / 4f;
            float x = 22 + (i % 4) * (cw + 10);
            float y = 354 + (i / 4) * 76;
            bool sel = harfIndex == i;
            bool learned = organgan.Contains(harflar[i].h);
            var b = Panel("h" + i, x, y, cw, 66, sel ? harflar[i].r : Hex("#FFF8EA"));
            Label("ht" + i, x, y + 16, cw, 34, harflar[i].h, 30, sel ? Hex("#FFF6E6") : (learned ? Hex("#7FA650") : Hex("#12303B")), TextAlignmentOptions.Center, true);
            int idx = i;
            Clickable(b.gameObject, () => PickLetter(idx));
        }
    }

    void BuildHub()
    {
        GradBg("#DDEAEC", "#FBF3E4");
        Back(() => Show("home"), Hex("#12303B"));
        Label("t", 70, 20, 260, 28, "Mini-o'yinlar", 26, Hex("#12303B"), TextAlignmentOptions.Left, true);
        Label("s", 70, 50, 260, 16, "Tanga yig'ib kiyim sotib olamiz", 12, new Color(0.07f, 0.19f, 0.23f, 0.55f), TextAlignmentOptions.Left, false);

        var g1 = Panel("g1", 22, 120, SW - 44, 172, Hex("#7FA650"));
        Label("g1t", 42, 240, SW - 88, 26, "Olma tut", 24, Hex("#FFF6E6"), TextAlignmentOptions.Left, true);
        Label("g1s", 42, 266, SW - 88, 16, "20 soniya · har olma 2 tanga", 12, new Color(1, 0.96f, 0.9f, 0.8f), TextAlignmentOptions.Left, false);
        Clickable(g1.gameObject, () => Show("oyin"));

        var g2 = Panel("g2", 22, 306, SW - 44, 172, Hex("#12303B"));
        Label("g2t", 42, 426, SW - 88, 26, "Juftini top", 24, Hex("#FFF6E6"), TextAlignmentOptions.Left, true);
        Label("g2s", 42, 452, SW - 88, 16, "Xotira o'yini · juft topilsa 5 tanga", 12, new Color(1, 0.96f, 0.9f, 0.7f), TextAlignmentOptions.Left, false);
        Clickable(g2.gameObject, () => StartMemory());

        BigBtn("Yutuqlar", 22, 494, SW - 44, 56, Hex("#12303B"), Hex("#FFF6E6"), 16, () => Show("yutuqlar"));
        BigBtn(giftTaken ? "Bugungi sovg'a olindi" : "Kunlik sovg'a: 30 tanga", 22, 562, SW - 44, 56,
            giftTaken ? new Color(0.07f, 0.19f, 0.23f, 0.08f) : Hex("#E9A62B"),
            giftTaken ? new Color(0.07f, 0.19f, 0.23f, 0.45f) : Hex("#3B2A1E"), 16,
            () => { if (!giftTaken) { giftTaken = true; coins += 30; msg = "Sovg'a uchun rahmat!"; Save(); Refresh(); } });
    }

    void BuildGame()
    {
        if (gameCo != null) { StopCoroutine(gameCo); gameCo = null; }
        GradBg("#9AD2DC", "#A2C371");
        Rect2("grass", 0, SH - 300, SW, 300, Hex("#7FA650"));
        Back(() => Show("home"), Hex("#12303B"));
        Label("t", 0, 24, SW, 28, "Olma tut!", 22, Hex("#12303B"), TextAlignmentOptions.Center, true);
        var sc = Panel("sc", SW - 130, 24, 56, 38, Hex("#FFF8EA"));
        var scT = Label("scT", SW - 130, 32, 56, 24, gScore.ToString(), 15, Hex("#C8452F"), TextAlignmentOptions.Center, true);
        var tm = Panel("tm", SW - 68, 24, 46, 38, Hex("#12303B"));
        var tmT = Label("tmT", SW - 68, 32, 46, 24, gTime + "s", 15, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);

        MomiqAt(SW / 2f, SH - 30, 0.8f);

        var itemsLayer = Node("items", buildRoot, 0, 100, SW, SH - 100);
        gameCo = StartCoroutine(RunGame(itemsLayer, scT, tmT));

        if (!gActive)
        {
            var ov = Rect2("idle", 0, 0, SW, SH, new Color(0.07f, 0.19f, 0.23f, 0.55f));
            Label("gt", 0, 320, SW, 40, gOver ? "Zo'r! " + gScore + " ta tutdik" : "Olma tut!", 30, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);
            Label("gd", 45, 372, SW - 90, 60, "Tushayotgan olmalarni bosib Momiqqa tutib bering. Har biri 2 tanga.", 14, new Color(1, 0.96f, 0.9f, 0.8f), TextAlignmentOptions.Center, false);
            BigBtn(gOver ? "Yana o'ynash" : "Boshlash", SW / 2f - 90, 452, 180, 58, Hex("#E9A62B"), Hex("#3B2A1E"), 20, () => StartGame(itemsLayer, scT, tmT));
        }
    }

    void BuildKitchen()
    {
        GradBg("#FFF6E4", "#FCE8C8");
        Rect2("floor", 0, 426, SW, SH - 426, Hex("#C7A276"));
        Rect2("floorline", 0, 412, SW, 14, Hex("#8A5C31"));
        Back(() => Show("home"), Hex("#3A3330"));
        MapBtn(() => Show("menu"));
        Label("t", 116, 20, SW - 140, 28, "Oshxona", 26, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        string ht = hunger > 70 ? "qorni to'q" : (hunger > 35 ? "bir oz och" : "juda och");
        Label("ts", 116, 50, SW - 140, 16, "Momiqning qorni: " + ht, 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Left, false);
        MomiqAt(SW / 2f, 400, 0.86f);
        BeginScroll(406);
        Label("q", 22, 4, 260, 16, "NIMA BERAMIZ?", 11, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        string[] fn = { "Issiq non", "Yaydab o't", "Iliq sut" };
        string[] fi = { "Tandirdan yangi", "Tog' yonbag'ridan", "Katta kosada" };
        string[] fh = { "N", "O'", "S" };
        Color[] fc = { Hex("#C8452F"), Hex("#28D62C"), Hex("#3B9BF0") };
        int[] fq = { 18, 12, 22 };
        for (int i = 0; i < 3; i++)
        {
            float y = 30 + i * 76;
            var row = Panel("f" + i, 0, y, SW - 44, 64, Color.white);
            var ic = Panel("fic" + i, 14, y + 6, 52, 52, fc[i]);
            Label("fh" + i, 14, y + 20, 52, 24, fh[i], 20, Color.white, TextAlignmentOptions.Center, true);
            Label("fn" + i, 78, y + 12, SW - 170, 22, fn[i], 18, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            Label("fi" + i, 78, y + 36, SW - 170, 14, fi[i], 11, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Left, false);
            Label("fp" + i, SW - 100, y + 20, 56, 20, "+" + fq[i], 14, Hex("#28D62C"), TextAlignmentOptions.Right, true);
            int q = fq[i]; string nm = fn[i];
            Clickable(row.gameObject, () => { Bump(ref hunger, q); Bump(ref joy, 4); AddXp(8, "ovqat"); React("ovqat", nm + " juda mazza! Rahmat!", 1.8f); });
        }
        EndScroll(260);
    }

    void BuildWardrobe()
    {
        GradBg("#F3EAFA", "#E7DBF4");
        Rect2("floor", 0, 446, SW, SH - 446, Hex("#C7A276"));
        Rect2("floorline", 0, 432, SW, 14, Hex("#8A5C31"));
        Back(() => Show("home"), Hex("#3A3330"));
        MapBtn(() => Show("menu"));
        Label("t", 116, 20, SW - 140, 28, "Kiyintirish", 26, Hex("#3A3330"), TextAlignmentOptions.Left, true);
        Label("ts", 116, 50, SW - 140, 16, "Bosib kiydiring yoki yechiring", 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Left, false);
        BeginScroll(140);
        var prev = Panel("prev", 0, 4, SW - 44, 268, Color.white);
        MomiqAt(SW / 2f, 268, 1f);
        string[] ck = { "dopi", "chopon", "sharf", "kozoynak" };
        string[] cn = { "Do'ppi", "Chopon", "Belbog'", "Ko'zoynak" };
        Color[] cr = { Hex("#17414F"), Hex("#1E7A8C"), Hex("#C8452F"), Hex("#12303B") };
        for (int i = 0; i < 4; i++)
        {
            float cw = (SW - 44 - 12) / 2f;
            float x = (i % 2) * (cw + 12);
            float y = 286 + (i / 2) * 116;
            bool own = owned.ContainsKey(ck[i]) && owned[ck[i]];
            bool onw = wear.ContainsKey(ck[i]) && wear[ck[i]];
            var card = Panel("c" + i, x, y, cw, 104, onw ? Color.white : Hex("#F6F0E2"));
            Panel("ci" + i, x + 14, y + 12, cw - 28, 52, cr[i]);
            Label("cn" + i, x + 14, y + 68, cw - 28, 20, cn[i], 17, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            Label("ch" + i, x + 14, y + 86, cw - 28, 14, own ? (onw ? "Kiyilgan" : "Yechilgan") : "Qulflangan", 11, own ? (onw ? Hex("#12A83A") : new Color(0.23f, 0.2f, 0.19f, 0.45f)) : Hex("#C8452F"), TextAlignmentOptions.Left, false);
            string key = ck[i]; string nm = cn[i];
            Clickable(card.gameObject, () => {
                if (!(owned.ContainsKey(key) && owned[key])) { Show("dokon"); return; }
                wear[key] = !wear[key]; hearts = true; React("kulgan", wear[key] ? "Menga mos keldi!" : "Yengil bo'ldi!", 1.4f); Save();
            });
        }
        BigBtn("Do'konga — yangi kiyimlar", 0, 520, SW - 44, 52, Hex("#3A3330"), Color.white, 16, () => Show("dokon"));
        EndScroll(590);
    }

    void BuildShop()
    {
        BgFlat(Hex("#FBF3E4"));
        Rect2("hdr", 0, 0, SW, 180, Hex("#12303B"));
        Back(() => Show("home"), Hex("#FFF6E6"));
        Label("t", 70, 24, 200, 30, "Do'kon", 26, Hex("#FFF6E6"), TextAlignmentOptions.Left, true);
        Label("c", SW - 130, 30, 108, 24, "" + coins, 16, Hex("#FFF6E6"), TextAlignmentOptions.Right, true);
        Label("note", 22, 100, SW - 44, 40, "Tanga o'yinlarda va parvarishda yig'iladi. Haqiqiy pul talab qilinmaydi.", 13, new Color(1, 0.96f, 0.9f, 0.85f), TextAlignmentOptions.Left, false);

        string[] key = { "chopon", "sharf", "kozoynak", "gilam" };
        string[] nm = { "Adras chopon", "Qizil belbog'", "Ko'zoynak", "Yangi gilam" };
        string[] iz = { "Sovuq kunlar uchun", "Bayramona", "Yozgi kunlar", "Hovlini indigo qiladi" };
        Color[] cc = { Hex("#1E7A8C"), Hex("#C8452F"), Hex("#12303B"), Hex("#1E7A8C") };
        int[] narx = { 60, 40, 55, 120 };
        for (int i = 0; i < 4; i++)
        {
            float y = 210 + i * 70;
            var row = Panel("s" + i, 22, y, SW - 44, 60, Hex("#FFFAF0"));
            Panel("sc" + i, 36, y + 3, 54, 54, cc[i]);
            Label("sn" + i, 104, y + 8, SW - 220, 22, nm[i], 18, Hex("#12303B"), TextAlignmentOptions.Left, true);
            Label("si" + i, 104, y + 32, SW - 220, 14, iz[i], 11, new Color(0.07f, 0.19f, 0.23f, 0.55f), TextAlignmentOptions.Left, false);
            bool own = owned[key[i]];
            bool afford = coins >= narx[i];
            var btn = Panel("sb" + i, SW - 120, y + 9, 96, 42, own ? new Color(0.5f, 0.65f, 0.31f, 0.18f) : (afford ? Hex("#E9A62B") : new Color(0.07f, 0.19f, 0.23f, 0.1f)));
            Label("sbt" + i, SW - 120, y + 20, 96, 20, own ? "Olingan" : narx[i] + " tanga", 13, own ? Hex("#4C6A2C") : (afford ? Hex("#3B2A1E") : new Color(0.07f, 0.19f, 0.23f, 0.4f)), TextAlignmentOptions.Center, true);
            int idx = i;
            Clickable(btn.gameObject, () => { if (!owned[key[idx]] && coins >= narx[idx]) { owned[key[idx]] = true; if (wear.ContainsKey(key[idx])) wear[key[idx]] = true; coins -= narx[idx]; msg = nm[idx] + " menga yoqdi!"; Save(); Refresh(); } });
        }
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
        BgFlat(Hex("#0E2731"));
        Back(() => Show("oyinlar"), Hex("#FFF6E6"));
        Label("t", 0, 24, SW, 28, "Juftini top", 22, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);
        Label("m1", SW - 132, 26, 52, 24, mMatched.Count + "/4", 14, Hex("#E9A62B"), TextAlignmentOptions.Center, true);
        Label("m2", SW - 76, 26, 52, 24, mMoves.ToString(), 14, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);
        for (int i = 0; i < mCards.Count; i++)
        {
            var c = mCards[i];
            bool open = mOpen.Contains(c.id) || mMatched.Contains(c.harf);
            float cw = (SW - 52 - 36) / 4f;
            float x = 26 + (i % 4) * (cw + 12);
            float y = 110 + (i / 4) * (104 + 12);
            var card = Panel("mc" + i, x, y, cw, 104, open ? c.rang : Hex("#173948"));
            if (open) Label("mt" + i, x, y + 34, cw, 40, c.harf, 26, Hex("#FFF6E6"), TextAlignmentOptions.Center, true);
            else Clickable(card.gameObject, () => FlipCard(c));
        }
        MomiqAt(SW / 2f, SH - 96, 0.72f);
        Label("hint", 0, SH - 44, SW, 20, "Bir xil harflarni juftlab toping", 13, new Color(1, 0.96f, 0.9f, 0.6f), TextAlignmentOptions.Center, false);
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
        GradBg("#E4EEE6", "#FFF9EE");
        HeaderBack("Kitoblar", "Fan kitoblari", "talim", Hex("#3A3330"));
        MapBtnAt(() => Show("menu"), Hex("#3A3330"));
        BeginScroll(80);
        string[] bn = { "Quvnoq matematika 1", "Ingliz tili", "Tabiiy fan", "Savodxonlik" };
        string[] bf = { "matem", "ingliz", "tabiiy", "savod" };
        Color[] bc = { Hex("#3A3330"), Hex("#3B9BF0"), Hex("#28D62C"), Hex("#FFB800") };
        for (int i = 0; i < 4; i++)
        {
            string f = bf[i];
            Card(i.ToString(), 4 + i * 132, 118, bn[i], "80 ta dars", bc[i], i == 3 ? Hex("#3A3330") : Color.white,
                () => { if (f == "ingliz") Show("ingliz"); else { fan = f; Show("matem"); } });
        }
        EndScroll(540);
    }

    void BuildMatem()
    {
        string fnom = fan == "tabiiy" ? "Tabiiy fan" : fan == "savod" ? "Savodxonlik" : fan == "ingliz" ? "Ingliz tili" : "Quvnoq matematika";
        Color hc = fan == "tabiiy" ? Hex("#28D62C") : fan == "savod" ? Hex("#FFB800") : fan == "ingliz" ? Hex("#3B9BF0") : Hex("#3A3330");
        GradBg("#EAF1EA", "#FFF9EE");
        HeaderBack(fnom, "80 ta dars", "talim", Hex("#3A3330"));
        MapBtnAt(() => Show("menu"), Hex("#3A3330"));
        MomiqAt(SW - 66, 176, 0.5f);
        BeginScroll(80);
        Label("dl", 22, 4, 200, 16, "DARSLAR", 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        for (int i = 0; i < 8; i++)
        {
            float y = 26 + i * 64;
            var row = Panel("d" + i, 0, y, SW - 44, 54, Color.white);
            var ic = Circle("di" + i, 12, y + 7, 40, hc);
            Label("dn" + i, 12, y + 18, 40, 20, (i + 1).ToString(), 16, Color.white, TextAlignmentOptions.Center, true);
            Label("dt" + i, 66, y + 16, SW - 160, 22, "Dars " + (i + 1), 17, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            Clickable(row.gameObject, () => StartMashqLesson(fan));
        }
        EndScroll(8 * 64 + 30);
    }

    void BuildFanJadval()
    {
        GradBg("#EAF1EA", "#FFF9EE");
        HeaderBack("Fanlar reytingi", "Har fanda o'zlashtirish", "talim", Hex("#3A3330"));
        string[] fk = { "matem", "ingliz", "tabiiy", "savod" };
        string[] fn = { "Matematika", "Ingliz tili", "Tabiiy fan", "Savodxonlik" };
        Color[] fc = { Hex("#C8452F"), Hex("#3B9BF0"), Hex("#28D62C"), Hex("#FFB800") };
        for (int i = 0; i < 4; i++)
        {
            float y = 100 + i * 88;
            Panel("f" + i, 22, y, SW - 44, 74, Color.white);
            Circle("fi" + i, 34, y + 16, 42, fc[i]);
            Label("fn" + i, 88, y + 14, SW - 160, 20, fn[i], 17, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            int v = fanlar.ContainsKey(fk[i]) ? fanlar[fk[i]] : 0;
            Rect2("fbg" + i, 88, y + 44, SW - 160, 8, new Color(0.23f, 0.2f, 0.19f, 0.12f));
            Rect2("fb" + i, 88, y + 44, (SW - 160) * Mathf.Clamp01(v / 100f), 8, fc[i]);
            Label("fv" + i, SW - 66, y + 22, 44, 20, v + "%", 15, fc[i], TextAlignmentOptions.Right, true);
        }
    }

    void BuildMashq()
    {
        GradBg("#E4EEE6", "#FFF9EE");
        if (mq.Count == 0) { StartMashqLesson(fan); return; }
        string fnom = fan == "tabiiy" ? "Tabiiy fan" : fan == "savod" ? "Savodxonlik" : fan == "ingliz" ? "Ingliz tili" : "Matematika";
        HeaderBack(fnom, "Mashq " + (mqIdx + 1) + "/" + mq.Count, "matem", Hex("#3A3330"));
        var q = mq[mqIdx];
        MomiqAt(SW - 66, 150, 0.5f);
        Panel("qp", 22, 92, SW - 44, 84, Color.white);
        Label("qt", 34, 108, SW - 68, 56, q.prompt, 19, Hex("#3A3330"), TextAlignmentOptions.Center, true);
        if (q.dots > 0)
        {
            for (int i = 0; i < q.dots; i++)
            {
                float x = 40 + (i % 5) * 62;
                float y = 200 + (i / 5) * 62;
                Circle("ap" + i, x, y, 52, q.dotCol);
            }
        }
        int n = q.opts.Length;
        for (int i = 0; i < n; i++)
        {
            float cw = (SW - 44 - 12) / 2f;
            float x = 22 + (i % 2) * (cw + 12);
            float y = 340 + (i / 2) * 92;
            var b = Panel("o" + i, x, y, cw, 80, Hex("#FFF8EA"));
            Label("ol" + i, x, y + 20, cw, 44, q.opts[i], 34, Hex("#3A3330"), TextAlignmentOptions.Center, true);
            int idx = i;
            Clickable(b.gameObject, () => AnswerMashq(idx));
        }
        if (mqResult != "") Label("mr", 0, 500, SW, 26, mqResult, 20, Hex("#12A83A"), TextAlignmentOptions.Center, true);
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
        GradBg("#12303B", "#0E2731");
        HeaderBack("SSA hamyon", "Tanga va sovg'alar", "home", Color.white);
        var card = Panel("bal", 22, 96, SW - 44, 96, Hex("#173948"));
        Circle("bc", 40, 118, 52, Hex("#E29B18"));
        Label("bl", 40, 130, 52, 24, "SSA", 12, Hex("#8A5C00"), TextAlignmentOptions.Center, true);
        Label("bv", 104, 118, SW - 160, 34, coins.ToString(), 32, Color.white, TextAlignmentOptions.Left, true);
        Label("bt", 104, 156, SW - 160, 16, "SSA coin", 12, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Left, false);
        BeginScroll(210);
        Label("h", 22, 4, 260, 16, "QANDAY YIG'ILADI", 10, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Left, true);
        string[] wn = { "Darslarni bajarish", "Momiqni parvarish qilish", "Mini-o'yinlar", "Kunlik bonus" };
        string[] wv = { "+5", "+3", "+2", "+30" };
        for (int i = 0; i < 4; i++)
        {
            float y = 26 + i * 62;
            Panel("w" + i, 0, y, SW - 44, 52, new Color(1, 1, 1, 0.08f));
            Label("wn" + i, 16, y + 16, SW - 120, 20, wn[i], 15, Color.white, TextAlignmentOptions.Left, true);
            Label("wv" + i, SW - 100, y + 16, 56, 20, wv[i], 16, Hex("#28D62C"), TextAlignmentOptions.Right, true);
        }
        EndScroll(280);
    }

    void BuildStiker()
    {
        GradBg("#FFF6E4", "#FFF9EE");
        HeaderBack("Stikerlar", stikerCount + " ta yig'ilgan", "yutuqlar", Hex("#3A3330"));
        BeginScroll(80);
        for (int i = 0; i < 12; i++)
        {
            float cw = (SW - 44 - 24) / 3f;
            float x = (i % 3) * (cw + 12);
            float y = 4 + (i / 3) * (cw + 12);
            bool got = i < stikerCount;
            var c = Panel("s" + i, x, y, cw, cw, got ? Hex("#FFE3B0") : new Color(0.23f, 0.2f, 0.19f, 0.06f));
            if (got) Circle("sc" + i, x + cw / 2f - 22, y + cw / 2f - 26, 44, Hex("#E4573F"));
            else Label("sl" + i, x, y + cw / 2f - 12, cw, 24, "?", 22, new Color(0.23f, 0.2f, 0.19f, 0.3f), TextAlignmentOptions.Center, true);
        }
        EndScroll(4 * ((SW - 68) / 3f + 12) + 20);
    }

    void BuildChop()
    {
        GradBg("#EAF1EA", "#FFF9EE");
        HeaderBack("Chop etish", "Diplom va natijalar", "yutuqlar", Hex("#3A3330"));
        var card = Panel("cert", 30, 110, SW - 60, 300, Color.white);
        Label("ct", 30, 150, SW - 60, 20, "MOMIQ MAKTABI", 12, Hex("#C8452F"), TextAlignmentOptions.Center, true);
        Label("cn", 30, 200, SW - 60, 40, nom, 34, Hex("#3A3330"), TextAlignmentOptions.Center, true);
        Label("cd", 30, 260, SW - 60, 20, daraja + "-daraja · " + organgan.Count + " dars", 14, new Color(0.23f, 0.2f, 0.19f, 0.6f), TextAlignmentOptions.Center, false);
        MomiqAt(SW / 2f, 400, 0.5f);
        BigBtn("PDF sifatida chop etish", 30, 440, SW - 60, 54, Hex("#12A83A"), Color.white, 16, () => React("kulgan", "Diplom tayyor!", 1.6f));
    }

    void BuildVazifalar()
    {
        GradBg("#EAF6EC", "#FFF9EE");
        HeaderBack("Kunlik vazifalar", "Har kuni yangilanadi", "home", Hex("#3A3330"));
        string[] qn = { "3 ta dars bajar", "Momiqni ovqatlantir", "1 mini-o'yin o'yna" };
        int[] qc = { organgan.Count, hisob.ContainsKey("ovqat") ? hisob["ovqat"] : 0, hisob.ContainsKey("oyin") ? hisob["oyin"] : 0 };
        int[] qk = { 3, 1, 1 };
        for (int i = 0; i < 3; i++)
        {
            float y = 100 + i * 92;
            int cur = Mathf.Min(qc[i], qk[i]);
            bool done = qc[i] >= qk[i];
            Panel("q" + i, 22, y, SW - 44, 78, Color.white);
            Label("qn" + i, 38, y + 14, SW - 130, 20, qn[i], 16, Hex("#3A3330"), TextAlignmentOptions.Left, true);
            Label("qp" + i, 38, y + 38, 120, 16, cur + "/" + qk[i], 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Left, false);
            var b = Panel("qb" + i, SW - 116, y + 20, 94, 38, done ? Hex("#12A83A") : new Color(0.23f, 0.2f, 0.19f, 0.08f));
            Label("qbt" + i, SW - 116, y + 30, 94, 18, done ? "+10" : "...", 14, done ? Color.white : new Color(0.23f, 0.2f, 0.19f, 0.4f), TextAlignmentOptions.Center, true);
        }
    }

    void BuildRekordlar()
    {
        GradBg("#12303B", "#0E2731");
        HeaderBack("Rekordlar", "Eng yaxshilar", "home", Color.white);
        string[] rn = { "Diyor", "Malika", nom, "Jasur", "Ozoda" };
        int[] rp = { 320, 280, coins, 210, 180 };
        // saralash
        var order = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4 };
        order.Sort((a, b) => rp[b] - rp[a]);
        for (int i = 0; i < order.Count; i++)
        {
            int idx = order[i];
            float y = 100 + i * 68;
            bool me = idx == 2;
            Panel("r" + i, 22, y, SW - 44, 56, me ? Hex("#1E7A8C") : new Color(1, 1, 1, 0.08f));
            Label("rr" + i, 34, y + 16, 30, 24, (i + 1).ToString(), 18, me ? Color.white : Hex("#E9A62B"), TextAlignmentOptions.Center, true);
            Circle("rc" + i, 70, y + 12, 32, me ? Color.white : new Color(1, 1, 1, 0.2f));
            Label("rn" + i, 112, y + 16, SW - 200, 24, rn[idx], 16, Color.white, TextAlignmentOptions.Left, true);
            Label("rp" + i, SW - 100, y + 16, 56, 24, rp[idx].ToString(), 16, Hex("#E9A62B"), TextAlignmentOptions.Right, true);
        }
    }

    void BuildAnalitika()
    {
        GradBg("#EAF1EA", "#FFF9EE");
        HeaderBack("Tahlil", "Natijalar", "home", Hex("#3A3330"));
        var t = Panel("tg", 22, 100, (SW - 56) / 2f, 80, Color.white);
        Label("tgv", 22, 116, (SW - 56) / 2f, 30, statTogri.ToString(), 28, Hex("#12A83A"), TextAlignmentOptions.Center, true);
        Label("tgl", 22, 150, (SW - 56) / 2f, 16, "To'g'ri", 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Center, false);
        float x2 = 22 + (SW - 56) / 2f + 12;
        Panel("tx", x2, 100, (SW - 56) / 2f, 80, Color.white);
        Label("txv", x2, 116, (SW - 56) / 2f, 30, statXato.ToString(), 28, Hex("#E4573F"), TextAlignmentOptions.Center, true);
        Label("txl", x2, 150, (SW - 56) / 2f, 16, "Xato", 12, new Color(0.23f, 0.2f, 0.19f, 0.55f), TextAlignmentOptions.Center, false);
        Label("wl", 22, 200, 200, 16, "SHU HAFTA", 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Left, true);
        string[] dn = { "D", "S", "C", "P", "J", "S", "Y" };
        int[] hv = { 3, 5, 2, 6, 4, 1, 3 };
        for (int i = 0; i < 7; i++)
        {
            float cw = (SW - 44) / 7f;
            float x = 22 + i * cw;
            float h = 20 + hv[i] * 14;
            Rect2("hb" + i, x + 4, 340 - h, cw - 8, h, Hex("#28D62C"));
            Label("hd" + i, x, 344, cw, 16, dn[i], 10, new Color(0.23f, 0.2f, 0.19f, 0.5f), TextAlignmentOptions.Center, true);
        }
    }

    void BuildDiplom() { CertScreen("Diplom", "home"); }
    void BuildSertifikat() { CertScreen("Sertifikat", "yutuqlar"); }
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
        GradBg("#DDEAEC", "#FFF9EE");
        HeaderBack("Tartiblash", "Kichikdan kattaga", "oyinlar", Hex("#3A3330"));
        if (tartibSeq.Count == 0) NewTartib();
        MomiqAt(SW / 2f, 300, 0.7f);
        Label("hint", 0, 320, SW, 20, "Sonlarni kichikdan kattaga bosing (" + (tartibIdx + 1) + "/" + tartibSeq.Count + ")", 13, new Color(0.23f, 0.2f, 0.19f, 0.6f), TextAlignmentOptions.Center, false);
        var sorted = new System.Collections.Generic.List<int>(tartibSeq); sorted.Sort();
        for (int i = 0; i < tartibSeq.Count; i++)
        {
            float bw = (SW - 44 - (tartibSeq.Count - 1) * 12) / tartibSeq.Count;
            float x = 22 + i * (bw + 12);
            int n = tartibSeq[i];
            bool done = tartibIdx > sorted.IndexOf(n) && IsPlaced(n, sorted);
            var b = Panel("tt" + i, x, 380, bw, 96, done ? Hex("#28D62C") : Color.white);
            Label("ttl" + i, x, 402, bw, 50, n.ToString(), 40, done ? Color.white : Hex("#3A3330"), TextAlignmentOptions.Center, true);
            Clickable(b.gameObject, () => TapTartib(n, sorted));
        }
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
