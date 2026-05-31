using UnityEngine;
using UnityEngine.InputSystem;

public class ShopMenu : MonoBehaviour
{
    public static ShopMenu Instance   { get; private set; }
    public static bool     IsShopOpen { get; private set; }

    // ── Layout ────────────────────────────────────────────────────────────────
    private const float PW   = 560f;
    private const float PH   = 510f;
    private const float RowH = 82f;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum Tab { Buy, Sell }
    private Tab    _tab;
    private string _shopName = "SHOP";

    private static readonly CropType[] Crops = { CropType.Carrot, CropType.Potato, CropType.Wheat };

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _infoSt, _rowSt;
    private GUIStyle _tabOn, _tabOff;
    private GUIStyle _btnBuy, _btnSell, _btnDis, _btnClose;
    private bool     _built;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive || FarmUpgradeShop.IsOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // [I] toggles the seed shop
        if (kb.iKey.wasPressedThisFrame)
        {
            if (IsShopOpen) Close();
            else            Open();
            return;
        }

        if (IsShopOpen && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    // ── Public ────────────────────────────────────────────────────────────────

    public void Open(string name = "SEED SHOP")
    {
        _shopName = name; _tab = Tab.Buy;
        IsShopOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0f;
    }

    public void Close()
    {
        IsShopOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        Time.timeScale   = 1f;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!IsShopOpen) return;
        EnsureStyles();

        // Dim overlay
        Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, .6f));

        float px = (Screen.width  - PW) * .5f;
        float py = (Screen.height - PH) * .5f;

        Rect(new Rect(px, py, PW, PH),   new Color(.07f, .09f, .07f, .98f));
        Rect(new Rect(px, py, PW, 3f),    new Color(.35f, .72f, .35f, 1f));

        GUI.Label(new Rect(px, py + 10f, PW, 36f),
                  _shopName, _titleSt);

        int money = GameManager.Instance != null ? GameManager.Instance.money : 0;
        GUI.Label(new Rect(px, py + 46f, PW, 24f), $"Money:  {money}$", _infoSt);

        // ── Tabs
        float tabW = 140f, tabH = 32f, tabY = py + 74f;
        if (GUI.Button(new Rect(px + PW * .5f - tabW - 4f, tabY, tabW, tabH),
                       "Buy Seeds",     _tab == Tab.Buy  ? _tabOn : _tabOff)) _tab = Tab.Buy;
        if (GUI.Button(new Rect(px + PW * .5f + 4f,        tabY, tabW, tabH),
                       "Sell Harvest",  _tab == Tab.Sell ? _tabOn : _tabOff)) _tab = Tab.Sell;

        Rect(new Rect(px + 20f, tabY + tabH + 4f, PW - 40f, 1f), new Color(.28f,.42f,.28f));

        float cy = tabY + tabH + 10f;
        if (_tab == Tab.Buy)  DrawBuyTab(px, cy, money);
        else                   DrawSellTab(px, cy);

        // Close
        float closeY = py + PH - 46f;
        Rect(new Rect(px + 20f, closeY - 6f, PW - 40f, 1f), new Color(.22f,.32f,.22f));
        if (Btn(new Rect(px + PW * .5f - 90f, closeY, 180f, 34f), "Close  [ESC]", _btnClose))
            Close();
    }

    // ── Buy tab ───────────────────────────────────────────────────────────────

    private void DrawBuyTab(float px, float ry, int money)
    {
        foreach (var crop in Crops) { DrawBuyRow(px, ry, crop, money); ry += RowH; }
    }

    private void DrawBuyRow(float px, float ry, CropType crop, int money)
    {
        var data = GameManager.Instance?.GetCropData(crop);
        if (data == null) return;
        int owned = GameManager.Instance.GetSeedCount(crop);

        GUI.Label(new Rect(px + 20f, ry + 6f, PW - 40f, 24f),
                  $"{data.cropName} Seeds  —  {data.seedPrice}$ each  |  Owned: {owned}", _rowSt);

        float bw = 108f, bh = 32f, by = ry + 38f, gap = 12f;
        float total = 3 * bw + 2 * gap;
        float bx = px + (PW - total) * .5f;

        BuyBtn(bx,            by, bw, bh, 1,  data, crop, money);
        BuyBtn(bx + bw + gap, by, bw, bh, 5,  data, crop, money);
        BuyBtn(bx + 2*(bw+gap), by, bw, bh, 10, data, crop, money);

        Rect(new Rect(px + 20f, ry + RowH - 2f, PW - 40f, 1f), new Color(.16f,.22f,.16f));
    }

    private void BuyBtn(float x, float y, float w, float h, int amt, CropData data, CropType crop, int money)
    {
        int cost = data.seedPrice * amt;
        bool can = money >= cost;
        if (Btn(new Rect(x, y, w, h), $"Buy {amt}\n({cost}$)", can ? _btnBuy : _btnDis) && can)
            GameManager.Instance?.BuySeed(crop, amt);
    }

    // ── Sell tab ──────────────────────────────────────────────────────────────

    private void DrawSellTab(float px, float ry)
    {
        if (GameManager.Instance == null) return;
        int grandTotal = 0;

        foreach (var crop in Crops)
        {
            grandTotal += DrawSellRow(px, ry, crop);
            ry += RowH;
        }

        Rect(new Rect(px + 20f, ry + 2f, PW - 40f, 1f), new Color(.22f,.32f,.22f));
        bool hasAnything = grandTotal > 0;
        string allLabel = hasAnything ? $"Sell Everything  (+{grandTotal}$)" : "Nothing to sell";
        if (Btn(new Rect(px + PW * .5f - 120f, ry + 10f, 240f, 36f),
                allLabel, hasAnything ? _btnSell : _btnDis) && hasAnything)
            GameManager.Instance.SellAllHarvest();
    }

    // returns sell value of this crop row
    private int DrawSellRow(float px, float ry, CropType crop)
    {
        var data = GameManager.Instance?.GetCropData(crop);
        if (data == null) return 0;

        int count = HarvestOf(crop);
        int bonus = (TalentManager.Instance != null)
            ? Mathf.RoundToInt(TalentManager.Instance.GetTalentModifier(TalentType.SellPrice))
            : 0;
        int perUnit = data.sellPrice + bonus;
        int value   = count * perUnit;

        string info = count > 0
            ? $"{data.cropName}:  {count}  ×  {perUnit}$  =  {value}$"
            : $"{data.cropName}:  none";
        GUI.Label(new Rect(px + 20f, ry + 6f, PW - 180f, 24f), info, _rowSt);

        if (count > 0 && Btn(new Rect(px + PW - 160f, ry + 30f, 140f, 32f),
                              $"Sell  (+{value}$)", _btnSell))
            SellOne(crop, count, value);

        Rect(new Rect(px + 20f, ry + RowH - 2f, PW - 40f, 1f), new Color(.16f,.22f,.16f));
        return value;
    }

    private static int HarvestOf(CropType c) =>
        GameManager.Instance == null ? 0 : c switch
        {
            CropType.Carrot => GameManager.Instance.carrots,
            CropType.Potato => GameManager.Instance.potatoes,
            CropType.Wheat  => GameManager.Instance.wheat,
            _               => 0
        };

    private static void SellOne(CropType crop, int count, int value)
    {
        if (GameManager.Instance == null) return;
        switch (crop)
        {
            case CropType.Carrot: GameManager.Instance.carrots  = 0; break;
            case CropType.Potato: GameManager.Instance.potatoes = 0; break;
            case CropType.Wheat:  GameManager.Instance.wheat    = 0; break;
        }
        GameManager.Instance.money += value;
        GameManager.Instance.UpdateUI();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool Btn(Rect r, string label, GUIStyle s)
    {
        bool en = s != null;
        GUI.enabled = en;
        bool c = GUI.Button(r, label, s ?? GUI.skin.button);
        GUI.enabled = true;
        return c;
    }

    private static void Rect(Rect r, Color c)
    {
        Color p = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = p;
    }

    private void EnsureStyles()
    {
        if (_built) return; _built = true;

        _titleSt = Style(26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.75f,1f,.75f));
        _infoSt  = Style(15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(.88f,.88f,.70f));
        _rowSt   = Style(15, FontStyle.Bold,   TextAnchor.MiddleLeft,   Color.white);

        _tabOff = BtnStyle(new Color(.12f,.16f,.12f), new Color(.60f,.60f,.60f), 14);
        _tabOn  = BtnStyle(new Color(.18f,.36f,.18f), new Color(.75f,1f,.75f),   14, true);

        _btnBuy   = BtnStyle(new Color(.16f,.44f,.20f), Color.white, 13);
        _btnSell  = BtnStyle(new Color(.52f,.38f,.08f), Color.white, 13);
        _btnClose = BtnStyle(new Color(.20f,.20f,.22f), Color.white, 13);
        _btnDis   = BtnStyle(new Color(.20f,.20f,.20f), new Color(.45f,.45f,.45f), 13);
    }

    private static GUIStyle Style(int size, FontStyle fs, TextAnchor align, Color col)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, alignment = align };
        s.normal.textColor = col; return s;
    }

    private static GUIStyle BtnStyle(Color bg, Color text, int size, bool bold = false)
    {
        var s = new GUIStyle(GUI.skin.button)
            { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
              alignment = TextAnchor.MiddleCenter };
        s.normal.background = s.hover.background = s.active.background = Tex(bg);
        s.normal.textColor  = s.hover.textColor  = s.active.textColor  = text;
        return s;
    }

    private static Texture2D Tex(Color c)
        { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
}
