using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Kiosk shop — structural farm upgrades bought with MONEY.
/// Open: interact with kiosk [E].  Close: [ESC] or Close button.
/// </summary>
public class FarmUpgradeShop : MonoBehaviour
{
    public static FarmUpgradeShop Instance { get; private set; }
    public static bool            IsOpen   { get; private set; }

    // Inspector: index of the FarmPlot item in BuildManager.buildableItems[]
    [Header("Build Integration")]
    public int farmPlotBuildItemIndex = 0;

    // ── Layout ────────────────────────────────────────────────────────────────
    private const float PW   = 580f;
    private const float RowH = 92f;

    // ── Notification ──────────────────────────────────────────────────────────
    private string _notifMsg   = "";
    private float  _notifTimer = 0f;
    private const float NotifDur = 3f;

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _moneySt, _nameSt, _descSt, _effectSt;
    private GUIStyle _btnBuy, _btnMax, _btnDis, _btnClose;
    private bool     _builtSt;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_notifTimer > 0f) _notifTimer -= Time.unscaledDeltaTime;
        if (!IsOpen) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    // ── Public ────────────────────────────────────────────────────────────────

    public void Open()
    {
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0f;
    }

    public void Close()
    {
        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        Time.timeScale   = 1f;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (PauseMenu.IsPaused) return;

        // Notification (visible even when shop is closed)
        if (_notifTimer > 0f)
        {
            float a = Mathf.Clamp01(_notifTimer);
            var ns = new GUIStyle(GUI.skin.label)
                { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            ns.normal.textColor = new Color(1f, .85f, .3f, a);
            GUI.Label(new Rect(0, Screen.height * .32f, Screen.width, 30f), _notifMsg, ns);
        }

        if (!IsOpen) return;
        BuildStyles();

        R(new Rect(0, 0, Screen.width, Screen.height), new Color(0,0,0,.60f));

        float ph = 82f + 4 * RowH + 50f;
        float px = (Screen.width  - PW) * .5f;
        float py = (Screen.height - ph) * .5f;

        R(new Rect(px, py, PW, ph),  new Color(.06f,.07f,.05f,.98f));
        R(new Rect(px, py, PW, 3f),  new Color(.55f,.80f,.35f));

        int money = GameManager.Instance != null ? GameManager.Instance.money : 0;
        GUI.Label(new Rect(px, py + 10f, PW, 34f), "FARM UPGRADES", _titleSt);
        GUI.Label(new Rect(px, py + 46f, PW, 24f), $"Money:  {money}$", _moneySt);

        R(new Rect(px + 20f, py + 74f, PW - 40f, 1f), new Color(.28f,.42f,.22f));

        float ry = py + 80f;

        // 1. Extra Farm Plot
        if (FarmUpgradeManager.Instance != null)
        {
            var fm = FarmUpgradeManager.Instance;
            DrawRow(px, ry, "Extra Farm Plot",
                $"Unlocks 1 new farm plot — place it in Build Mode [Tab]",
                $"Owned: {fm.extraPlotsOwned}/{FarmUpgradeManager.MaxExtraPlots}  |  Tokens ready: {fm.freePlotTokens}",
                fm.extraPlotsOwned < FarmUpgradeManager.MaxExtraPlots ? fm.GetExtraPlotCost() : 0,
                fm.extraPlotsOwned >= FarmUpgradeManager.MaxExtraPlots,
                money,
                new Color(.45f,.80f,.45f),
                () =>
                {
                    if (fm.BuyExtraPlot())
                        Notify("New farm plot ready!  Open Build Menu [Tab] to place it.");
                });
            ry += RowH;

            // 2. Growth Accelerator
            DrawRow(px, ry, "Growth Accelerator",
                "Crops grow 8% faster per level",
                $"Level {fm.growthBoosterLevel}/{FarmUpgradeManager.MaxGrowthBooster}" +
                (fm.growthBoosterLevel > 0 ? $"  |  -{fm.growthBoosterLevel * 8}% grow time" : ""),
                fm.growthBoosterLevel < FarmUpgradeManager.MaxGrowthBooster ? fm.GetGrowthCost() : 0,
                fm.growthBoosterLevel >= FarmUpgradeManager.MaxGrowthBooster,
                money,
                new Color(.45f,.75f,1f),
                () => fm.BuyGrowthBooster());
            ry += RowH;

            // 3. Harvest Booster
            DrawRow(px, ry, "Harvest Booster",
                "Each plot yields +1 extra crop per level",
                $"Level {fm.harvestBoosterLevel}/{FarmUpgradeManager.MaxHarvestBooster}" +
                (fm.harvestBoosterLevel > 0 ? $"  |  +{fm.harvestBoosterLevel} per plot" : ""),
                fm.harvestBoosterLevel < FarmUpgradeManager.MaxHarvestBooster ? fm.GetHarvestCost() : 0,
                fm.harvestBoosterLevel >= FarmUpgradeManager.MaxHarvestBooster,
                money,
                new Color(1f,.70f,.35f),
                () => fm.BuyHarvestBooster());
            ry += RowH;

            // 4. Trade Network
            DrawRow(px, ry, "Trade Network",
                "Sell price +5$ per crop per level",
                $"Level {fm.tradeNetworkLevel}/{FarmUpgradeManager.MaxTradeNetwork}" +
                (fm.tradeNetworkLevel > 0 ? $"  |  +{fm.tradeNetworkLevel * 5}$ per unit" : ""),
                fm.tradeNetworkLevel < FarmUpgradeManager.MaxTradeNetwork ? fm.GetTradeCost() : 0,
                fm.tradeNetworkLevel >= FarmUpgradeManager.MaxTradeNetwork,
                money,
                new Color(1f,.85f,.30f),
                () => fm.BuyTradeNetwork());
            ry += RowH;
        }

        R(new Rect(px + 20f, ry + 4f, PW - 40f, 1f), new Color(.2f,.3f,.2f));
        if (GUI.Button(new Rect(px + PW * .5f - 85f, ry + 12f, 170f, 32f), "Close  [ESC]", _btnClose))
            Close();
    }

    private void DrawRow(float px, float ry, string name, string desc, string effect,
                         int cost, bool isMax, int money, Color col,
                         System.Action onBuy)
    {
        BuildStyles();
        bool canAfford = !isMax && money >= cost;

        R(new Rect(px + 10f, ry + 8f, 5f, RowH - 16f), col);

        _nameSt.normal.textColor = col;
        GUI.Label(new Rect(px + 22f, ry + 6f,  PW - 160f, 24f), name, _nameSt);

        _descSt.normal.textColor = new Color(.65f,.65f,.65f);
        GUI.Label(new Rect(px + 22f, ry + 30f, PW - 160f, 18f), desc, _descSt);

        _effectSt.normal.textColor = new Color(.85f,.85f,.60f);
        GUI.Label(new Rect(px + 22f, ry + 50f, PW - 160f, 18f), effect, _effectSt);

        GUIStyle btnSt = isMax ? _btnMax : (canAfford ? _btnBuy : _btnDis);
        string   label = isMax ? "MAX" : (canAfford ? $"Buy\n{cost}$" : cost > 0 ? $"Need {cost}$" : "MAX");

        if (GUI.Button(new Rect(px + PW - 128f, ry + 18f, 110f, 54f), label, btnSt) && canAfford)
            onBuy?.Invoke();

        R(new Rect(px + 20f, ry + RowH - 2f, PW - 40f, 1f), new Color(.14f,.20f,.12f));
    }

    private void Notify(string msg) { _notifMsg = msg; _notifTimer = NotifDur; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void R(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void BuildStyles()
    {
        if (_builtSt) return; _builtSt = true;

        _titleSt  = St(24, FontStyle.Bold,   TextAnchor.MiddleCenter, new Color(.55f,.90f,.45f));
        _moneySt  = St(15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(.88f,.88f,.70f));
        _nameSt   = St(16, FontStyle.Bold,   TextAnchor.MiddleLeft,   Color.white);
        _descSt   = St(12, FontStyle.Normal, TextAnchor.MiddleLeft,   new Color(.65f,.65f,.65f));
        _effectSt = St(12, FontStyle.Bold,   TextAnchor.MiddleLeft,   new Color(.85f,.85f,.60f));

        _btnBuy   = BtnSt(new Color(.18f,.44f,.18f), new Color(.24f,.58f,.24f), Color.white);
        _btnMax   = BtnSt(new Color(.28f,.24f,.08f), new Color(.28f,.24f,.08f), new Color(1f,.85f,.3f));
        _btnDis   = BtnSt(new Color(.18f,.18f,.18f), new Color(.18f,.18f,.18f), new Color(.4f,.4f,.4f));
        _btnClose = BtnSt(new Color(.18f,.18f,.20f), new Color(.28f,.28f,.32f), Color.white);
    }

    private static GUIStyle St(int sz, FontStyle fs, TextAnchor al, Color col)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = sz, fontStyle = fs, alignment = al };
        s.normal.textColor = col; return s;
    }

    private static GUIStyle BtnSt(Color n, Color h, Color text)
    {
        var s = new GUIStyle(GUI.skin.button)
            { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        s.normal.background = s.active.background = Tx(n);
        s.hover.background  = Tx(h);
        s.normal.textColor = s.hover.textColor = s.active.textColor = text;
        return s;
    }

    private static Texture2D Tx(Color c)
        { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
}
