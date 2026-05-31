using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bottom-center inventory with:
///   – XP / Level bar
///   – Seeds row  (3 slots, selectable with 1/2/3 or scroll)
///   – Harvest row (3 slots, info only)
/// </summary>
public class InventoryHotbar : MonoBehaviour
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private const float SlotW   = 84f;
    private const float SlotH   = 90f;     // seed slot height
    private const float HSlotH  = 62f;     // harvest slot height
    private const float Gap     = 6f;
    private const float PadBot  = 14f;
    private const int   Count   = 3;

    // Total hotbar width
    private static float TotalW => Count * SlotW + (Count - 1) * Gap;

    // ── Crop data ─────────────────────────────────────────────────────────────
    private static readonly CropType[] Crops = { CropType.Carrot, CropType.Potato, CropType.Wheat };
    private static readonly string[]   Names = { "Carrot", "Potato", "Wheat" };

    // Colors: seed swatch colors
    private static readonly Color[] SeedColors =
    {
        new Color(1.00f, 0.55f, 0.15f),
        new Color(0.85f, 0.72f, 0.25f),
        new Color(0.82f, 0.92f, 0.40f)
    };

    // Colors: harvest display colors
    private static readonly Color[] HarvestColors =
    {
        new Color(0.90f, 0.30f, 0.25f),   // carrot red-orange
        new Color(0.65f, 0.50f, 0.18f),   // potato brown
        new Color(0.75f, 0.82f, 0.22f)    // wheat yellow-green
    };

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _bigNum, _smallLbl, _keyLbl, _xpLbl, _hintLbl;
    private bool     _built;

    // ────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (AnyMenuOpen()) return;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) Select(0);
            else if (kb.digit2Key.wasPressedThisFrame) Select(1);
            else if (kb.digit3Key.wasPressedThisFrame) Select(2);
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float s = mouse.scroll.ReadValue().y;
            if (s != 0f)
            {
                int cur = IndexOf(GameManager.Instance?.selectedCrop ?? CropType.Carrot);
                Select((cur + (s > 0f ? -1 : 1) + Count) % Count);
            }
        }
    }

    private void Select(int i)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.selectedCrop = Crops[i];
        GameManager.Instance.UpdateUI();
    }

    private static int IndexOf(CropType c)
    {
        for (int i = 0; i < Crops.Length; i++) if (Crops[i] == c) return i;
        return 0;
    }

    private static bool AnyMenuOpen() =>
        ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen || TalentScreen.IsOpen ||
        BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive;

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen || TalentScreen.IsOpen || BuildManager.IsBuildMenuOpen) return;
        if (GameManager.Instance == null) return;
        EnsureStyles();

        int   sel  = IndexOf(GameManager.Instance.selectedCrop);
        float sx   = (Screen.width - TotalW) * .5f;

        // ── Calculate layout from bottom up ──────────────────────────────────
        float harvestY = Screen.height - HSlotH - PadBot;
        float seedY    = harvestY - SlotH - Gap;
        float xpBarY   = seedY - 28f - 6f;     // 28px XP bar, 6px gap below seeds

        // ── [I] hint (top-right above hotbar, outside of menus) ──────────────
        DrawShopHint(xpBarY);

        // ── XP bar ───────────────────────────────────────────────────────────
        DrawXpBar(sx, xpBarY);

        // ── Seed slots ───────────────────────────────────────────────────────
        for (int i = 0; i < Count; i++)
            DrawSeedSlot(sx + i * (SlotW + Gap), seedY, i, i == sel);

        // ── Harvest slots ─────────────────────────────────────────────────────
        for (int i = 0; i < Count; i++)
            DrawHarvestSlot(sx + i * (SlotW + Gap), harvestY, i);
    }

    // ── XP bar ───────────────────────────────────────────────────────────────

    private void DrawXpBar(float sx, float y)
    {
        float w = TotalW;
        float h = 22f;

        // Background panel
        R(new Rect(sx - 4f, y, w + 8f, h), new Color(.06f,.06f,.06f,.85f));

        if (PlayerLevel.Instance == null) return;

        int level = PlayerLevel.Instance.level;
        int xp    = PlayerLevel.Instance.xp;
        int xpMax = PlayerLevel.Instance.XpToNextLevel;
        int pts   = PlayerLevel.Instance.upgradePoints;

        // XP fill
        float fill = xpMax > 0 ? (float)xp / xpMax : 0f;
        R(new Rect(sx - 4f, y, (w + 8f) * fill, h), new Color(.25f,.60f,.25f,.9f));

        // Level label left
        _xpLbl.normal.textColor = new Color(1f,.92f,.5f);
        _xpLbl.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(sx, y, 60f, h), $"Lv.{level}", _xpLbl);

        // XP center
        _xpLbl.normal.textColor = Color.white;
        _xpLbl.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(sx + 60f, y, w - 120f, h), $"{xp} / {xpMax} XP", _xpLbl);

        // Points right
        _xpLbl.normal.textColor = pts > 0 ? new Color(1f,.85f,.3f) : new Color(.55f,.55f,.55f);
        _xpLbl.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(sx + w - 64f, y, 60f, h), $"⭐ {pts}pts", _xpLbl);
    }

    // ── Seed slot ─────────────────────────────────────────────────────────────

    private void DrawSeedSlot(float x, float y, int i, bool sel)
    {
        // Border
        R(new Rect(x - 3f, y - 3f, SlotW + 6f, SlotH + 6f),
          sel ? new Color(1f,1f,.35f) : new Color(.22f,.22f,.22f,.7f));

        // BG
        R(new Rect(x, y, SlotW, SlotH),
          sel ? new Color(.14f,.15f,.09f,.95f) : new Color(.07f,.07f,.07f,.82f));

        // Color swatch (seed icon area)
        float swH = SlotH * .40f;
        Color c = SeedColors[i];
        R(new Rect(x + 4f, y + 4f, SlotW - 8f, swH), new Color(c.r, c.g, c.b, sel ? 1f : .60f));

        // Seed icon label
        _smallLbl.normal.textColor = new Color(c.r*.7f, c.g*.7f, c.b*.7f, .9f);
        _smallLbl.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(x, y + 4f, SlotW, swH), "SEED", _smallLbl);

        // Count
        int seeds = GameManager.Instance.GetSeedCount(Crops[i]);
        _bigNum.normal.textColor = seeds > 0
            ? (sel ? Color.white : new Color(.85f,.85f,.85f))
            : new Color(.45f,.45f,.45f);
        GUI.Label(new Rect(x, y + swH + 2f, SlotW, 26f), seeds.ToString(), _bigNum);

        // Name
        _smallLbl.alignment = TextAnchor.UpperCenter;
        _smallLbl.normal.textColor = sel ? new Color(1f,1f,.5f) : new Color(.62f,.62f,.62f);
        GUI.Label(new Rect(x, y + swH + 27f, SlotW, 16f), Names[i], _smallLbl);

        // Hotkey badge
        _keyLbl.normal.textColor = new Color(.45f,.45f,.45f,.9f);
        GUI.Label(new Rect(x + SlotW - 20f, y + SlotH - 20f, 18f, 18f), $"{i+1}", _keyLbl);
    }

    // ── Harvest slot ──────────────────────────────────────────────────────────

    private void DrawHarvestSlot(float x, float y, int i)
    {
        Color c = HarvestColors[i];

        // Thin colored top bar
        R(new Rect(x, y, SlotW, 3f), new Color(c.r, c.g, c.b, .7f));

        // BG
        R(new Rect(x, y + 3f, SlotW, HSlotH - 3f), new Color(.07f,.05f,.05f,.82f));

        // Harvest count
        int harvest = HarvestOf(Crops[i]);
        _bigNum.normal.textColor = harvest > 0
            ? new Color(c.r, c.g, c.b, 1f)
            : new Color(.38f,.38f,.38f);
        GUI.Label(new Rect(x, y + 6f, SlotW, 26f), harvest.ToString(), _bigNum);

        // Label
        _smallLbl.alignment = TextAnchor.UpperCenter;
        _smallLbl.normal.textColor = new Color(.60f,.60f,.60f);
        GUI.Label(new Rect(x, y + 34f, SlotW, 16f), Names[i], _smallLbl);

        // "Harvest" sub-label
        _smallLbl.normal.textColor = new Color(c.r*.6f, c.g*.6f, c.b*.6f, .8f);
        GUI.Label(new Rect(x, y + 47f, SlotW, 14f), "harvest", _smallLbl);
    }

    // ── Shop hint ─────────────────────────────────────────────────────────────

    private void DrawShopHint(float hotbarTopY)
    {
        _hintLbl.normal.textColor = new Color(.60f,.60f,.60f,.75f);
        float hx = Screen.width * .5f + TotalW * .5f + 10f;
        float hy = hotbarTopY + 4f;
        GUI.Label(new Rect(hx, hy,      160f, 18f), "[I]  Seed Shop",   _hintLbl);
        GUI.Label(new Rect(hx, hy + 18f, 160f, 18f), "[T]  Talents",    _hintLbl);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int HarvestOf(CropType c)
    {
        if (GameManager.Instance == null) return 0;
        return c switch
        {
            CropType.Carrot => GameManager.Instance.carrots,
            CropType.Potato => GameManager.Instance.potatoes,
            CropType.Wheat  => GameManager.Instance.wheat,
            _               => 0
        };
    }

    private static void R(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void EnsureStyles()
    {
        if (_built) return; _built = true;

        _bigNum = new GUIStyle(GUI.skin.label)
            { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        _smallLbl = new GUIStyle(GUI.skin.label)
            { fontSize = 11, alignment = TextAnchor.UpperCenter };

        _keyLbl = new GUIStyle(GUI.skin.label)
            { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _keyLbl.normal.textColor = new Color(.45f,.45f,.45f,.9f);

        _xpLbl = new GUIStyle(GUI.skin.label)
            { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        _hintLbl = new GUIStyle(GUI.skin.label)
            { fontSize = 12, alignment = TextAnchor.MiddleLeft };
    }
}
