using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bottom-center hotbar with 3 seed slots.
/// Keys 1/2/3 select directly; scroll wheel cycles.
/// Syncs with GameManager.selectedCrop (works alongside Q key).
/// </summary>
public class InventoryHotbar : MonoBehaviour
{
    private const float SlotSize   = 82f;
    private const float SlotGap    = 8f;
    private const float BottomPad  = 16f;
    private const int   SlotCount  = 3;

    private static readonly CropType[] Slots  = { CropType.Carrot, CropType.Potato, CropType.Wheat };
    private static readonly string[]   Names  = { "Carrot", "Potato", "Wheat" };
    private static readonly Color[]    Colors =
    {
        new Color(1.0f, 0.55f, 0.15f),
        new Color(0.85f, 0.72f, 0.25f),
        new Color(0.82f, 0.92f, 0.40f)
    };

    private GUIStyle _cntSt, _nameSt, _keySt;
    private bool     _built;

    // ────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (ShopMenu.IsShopOpen || BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive) return;

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
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                int cur  = IndexOf(GameManager.Instance?.selectedCrop ?? CropType.Carrot);
                Select((cur + (scroll > 0f ? -1 : 1) + SlotCount) % SlotCount);
            }
        }
    }

    private void Select(int i)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.selectedCrop = Slots[i];
        GameManager.Instance.UpdateUI();
    }

    private static int IndexOf(CropType c)
    {
        for (int i = 0; i < Slots.Length; i++) if (Slots[i] == c) return i;
        return 0;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (ShopMenu.IsShopOpen || BuildManager.IsBuildMenuOpen) return;
        if (GameManager.Instance == null) return;
        EnsureStyles();

        int   sel   = IndexOf(GameManager.Instance.selectedCrop);
        float totalW = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
        float sx = (Screen.width - totalW) * .5f;
        float sy = Screen.height - SlotSize - BottomPad;

        for (int i = 0; i < SlotCount; i++)
            DrawSlot(sx + i * (SlotSize + SlotGap), sy, i, i == sel);
    }

    private void DrawSlot(float x, float y, int i, bool sel)
    {
        // Border
        R(new Rect(x - 3f, y - 3f, SlotSize + 6f, SlotSize + 6f),
          sel ? new Color(1f, 1f, .35f) : new Color(.28f,.28f,.28f,.7f));

        // Background
        R(new Rect(x, y, SlotSize, SlotSize),
          sel ? new Color(.15f,.16f,.10f,.95f) : new Color(.08f,.08f,.08f,.80f));

        // Color swatch
        float sw = SlotSize * .44f;
        Color co = Colors[i];
        R(new Rect(x + 4f, y + 4f, SlotSize - 8f, sw),
          new Color(co.r, co.g, co.b, sel ? 1f : .65f));

        // Count
        int count = GameManager.Instance.GetSeedCount(Slots[i]);
        _cntSt.normal.textColor = count > 0
            ? (sel ? Color.white : new Color(.85f,.85f,.85f))
            : new Color(.5f,.5f,.5f);
        GUI.Label(new Rect(x, y + sw + 2f,  SlotSize, 22f), count.ToString(), _cntSt);

        // Name
        _nameSt.normal.textColor = sel ? new Color(1f, 1f, .5f) : new Color(.68f,.68f,.68f);
        GUI.Label(new Rect(x, y + sw + 22f, SlotSize, 18f), Names[i], _nameSt);

        // Hotkey
        GUI.Label(new Rect(x + SlotSize - 22f, y + SlotSize - 20f, 18f, 18f), $"{i+1}", _keySt);
    }

    private static void R(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void EnsureStyles()
    {
        if (_built) return; _built = true;

        _cntSt = new GUIStyle(GUI.skin.label)
            { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        _nameSt = new GUIStyle(GUI.skin.label)
            { fontSize = 11, alignment = TextAnchor.UpperCenter };

        _keySt = new GUIStyle(GUI.skin.label)
            { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _keySt.normal.textColor = new Color(.5f,.5f,.5f,.8f);
    }
}
