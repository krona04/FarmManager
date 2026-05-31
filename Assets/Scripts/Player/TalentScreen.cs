using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Talent tree opened with [T] key.
/// Upgrades cost upgrade points earned by levelling up (not money).
/// </summary>
public class TalentScreen : MonoBehaviour
{
    public static TalentScreen Instance { get; private set; }
    public static bool          IsOpen  { get; private set; }

    // ── Layout ────────────────────────────────────────────────────────────────
    private const float PW   = 560f;
    private const float RowH = 88f;

    private static readonly TalentType[] Talents =
    {
        TalentType.MovementSpeed,
        TalentType.GrowthSpeed,
        TalentType.SellPrice,
        TalentType.DoubleYieldChance
    };

    private static readonly string[] TalentNames =
    {
        "Movement Speed",
        "Growth Speed",
        "Sell Bonus",
        "Double Yield"
    };

    private static readonly string[] TalentDescs =
    {
        "+5% movement speed per level",
        "-5% crop grow time per level",
        "+5$ per crop sold per level",
        "+5% chance to double harvest per level"
    };

    private static readonly Color[] TalentColors =
    {
        new Color(.45f,.78f,1.00f),
        new Color(.50f,1.00f,.55f),
        new Color(1.00f,.85f,.30f),
        new Color(1.00f,.52f,.88f)
    };

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _ptsSt, _nameSt, _descSt, _lvlSt;
    private GUIStyle _btnUpg, _btnMax, _btnDis, _btnClose;
    private bool     _builtSt;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        bool blocked = ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen
                       || BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive;
        if (blocked) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tKey.wasPressedThisFrame)
        {
            if (IsOpen) Close(); else Open();
            return;
        }

        if (IsOpen && kb.escapeKey.wasPressedThisFrame) Close();
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
        if (!IsOpen) return;
        BuildStyles();

        R(new Rect(0, 0, Screen.width, Screen.height), new Color(0,0,0,.60f));

        float ph = 92f + Talents.Length * RowH + 50f;
        float px = (Screen.width  - PW) * .5f;
        float py = (Screen.height - ph) * .5f;

        R(new Rect(px, py, PW, ph), new Color(.05f,.06f,.08f,.98f));
        R(new Rect(px, py, PW, 3f), new Color(.55f,.78f,1f));

        // Title
        GUI.Label(new Rect(px, py + 10f, PW, 34f), "TALENTS  [T]", _titleSt);

        // Level + XP + Points
        int level = PlayerLevel.Instance?.level ?? 1;
        int xp    = PlayerLevel.Instance?.xp    ?? 0;
        int xpMax = PlayerLevel.Instance?.XpToNextLevel ?? 100;
        int pts   = PlayerLevel.Instance?.upgradePoints ?? 0;

        // Mini XP bar
        float barW = PW - 80f;
        float barX = px + 40f;
        float barY = py + 49f;
        R(new Rect(barX, barY, barW, 14f), new Color(.15f,.17f,.20f));
        R(new Rect(barX, barY, barW * Mathf.Clamp01((float)xp / xpMax), 14f), new Color(.28f,.62f,.28f));

        _ptsSt.alignment = TextAnchor.MiddleLeft;
        _ptsSt.normal.textColor = new Color(.8f,.8f,.6f);
        GUI.Label(new Rect(px + 6f, barY - 2f, 38f, 18f), $"Lv{level}", _ptsSt);

        _ptsSt.alignment = TextAnchor.MiddleCenter;
        _ptsSt.normal.textColor = Color.white;
        GUI.Label(new Rect(barX, barY - 2f, barW, 18f), $"{xp} / {xpMax} XP", _ptsSt);

        _ptsSt.alignment = TextAnchor.MiddleRight;
        _ptsSt.normal.textColor = pts > 0 ? new Color(1f,.85f,.3f) : new Color(.5f,.5f,.5f);
        GUI.Label(new Rect(px, barY - 2f, PW - 6f, 18f), $"⭐ {pts} pts", _ptsSt);

        R(new Rect(px + 20f, py + 70f, PW - 40f, 1f), new Color(.25f,.38f,.50f));

        float ry = py + 76f;
        foreach (var t in Talents) { DrawRow(px, ry, t, pts); ry += RowH; }

        R(new Rect(px + 20f, ry + 4f, PW - 40f, 1f), new Color(.2f,.28f,.38f));
        if (GUI.Button(new Rect(px + PW * .5f - 85f, ry + 12f, 170f, 32f), "Close  [ESC]", _btnClose))
            Close();
    }

    private void DrawRow(float px, float ry, TalentType type, int pts)
    {
        if (TalentManager.Instance == null) return;

        int    idx   = System.Array.IndexOf(Talents, type);
        string name  = TalentNames[idx];
        string desc  = TalentDescs[idx];
        Color  col   = TalentColors[idx];
        int    cur   = TalentManager.Instance.GetCurrentLevel(type);
        int    max   = TalentManager.Instance.GetMaxLevel(type);
        bool   isMax = TalentManager.Instance.IsMaxLevel(type);
        bool   canUp = !isMax && pts >= 1;

        // Color strip
        R(new Rect(px + 10f, ry + 8f, 5f, RowH - 16f), col);

        _nameSt.normal.textColor = col;
        GUI.Label(new Rect(px + 22f, ry + 6f, PW - 170f, 24f), name, _nameSt);

        _descSt.normal.textColor = new Color(.65f,.65f,.65f);
        GUI.Label(new Rect(px + 22f, ry + 30f, PW - 170f, 18f), desc, _descSt);

        // Level bar
        DrawBar(px + 22f, ry + 54f, 180f, 12f, cur, max, col);
        _lvlSt.normal.textColor = isMax ? new Color(1f,.85f,.3f) : new Color(.68f,.68f,.68f);
        GUI.Label(new Rect(px + 208f, ry + 50f, 60f, 20f), isMax ? "MAX" : $"Lv {cur}/{max}", _lvlSt);

        // Button
        GUIStyle st  = isMax ? _btnMax : (canUp ? _btnUpg : _btnDis);
        string label = isMax ? "MAX" : (canUp ? "Upgrade\n(1 pt)" : "No points");

        if (GUI.Button(new Rect(px + PW - 130f, ry + 16f, 112f, 54f), label, st) && canUp)
        {
            PlayerLevel.Instance?.SpendPoint();
            TalentManager.Instance.UpgradeTalent(type);
        }

        R(new Rect(px + 20f, ry + RowH - 2f, PW - 40f, 1f), new Color(.12f,.16f,.22f));
    }

    private static void DrawBar(float x, float y, float w, float h, int cur, int max, Color col)
    {
        R(new Rect(x, y, w, h), new Color(.15f,.17f,.20f));
        if (max > 0 && cur > 0)
            R(new Rect(x, y, w * (float)cur / max, h), new Color(col.r*.65f, col.g*.65f, col.b*.65f));
    }

    private static void R(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void BuildStyles()
    {
        if (_builtSt) return; _builtSt = true;

        _titleSt = St(24, FontStyle.Bold,   TextAnchor.MiddleCenter, new Color(.55f,.80f,1f));
        _ptsSt   = St(13, FontStyle.Bold,   TextAnchor.MiddleCenter, Color.white);
        _nameSt  = St(16, FontStyle.Bold,   TextAnchor.MiddleLeft,   Color.white);
        _descSt  = St(12, FontStyle.Normal, TextAnchor.MiddleLeft,   new Color(.65f,.65f,.65f));
        _lvlSt   = St(13, FontStyle.Bold,   TextAnchor.MiddleLeft,   new Color(.68f,.68f,.68f));

        _btnUpg   = BtnSt(new Color(.18f,.40f,.72f), new Color(.25f,.52f,.88f), Color.white);
        _btnMax   = BtnSt(new Color(.30f,.25f,.08f), new Color(.30f,.25f,.08f), new Color(1f,.85f,.3f));
        _btnDis   = BtnSt(new Color(.18f,.18f,.20f), new Color(.18f,.18f,.20f), new Color(.4f,.4f,.4f));
        _btnClose = BtnSt(new Color(.18f,.18f,.22f), new Color(.28f,.28f,.32f), Color.white);
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
