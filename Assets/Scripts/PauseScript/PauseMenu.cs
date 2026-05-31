using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;

    private bool _isPaused      = false;
    private bool _showSaveSlots = false;
    private int  _saveConfirmSlot = -1;

    // ── Layout ────────────────────────────────────────────────────────────────
    private const float PW    = 420f;
    private const float SlotH = 66f;

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _infoSt, _emptySt, _slotSt;
    private GUIStyle _btnSave, _btnOverwrite, _btnBack, _btnDel;
    private bool     _builtSt;

    // ────────────────────────────────────────────────────────────────────────

    void Start() { pauseMenuUI.SetActive(false); }

    void Update()
    {
        if (ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen || TalentScreen.IsOpen
            || BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_showSaveSlots) { _showSaveSlots = false; return; }
            if (_isPaused)      Resume();
            else                Pause();
        }
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        _showSaveSlots = false;
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _isPaused        = false;
    }

    /// <summary>Called by "Save Game" button in pause UI — opens slot selection.</summary>
    public void OpenSaveSlots()
    {
        _showSaveSlots    = true;
        _saveConfirmSlot  = -1;
    }

    public void ExitToMenu()
    {
        GameManager.Instance?.SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        GameManager.Instance?.SaveGame();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ────────────────────────────────────────────────────────────────────────

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        _showSaveSlots   = false;
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        _isPaused        = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ── OnGUI — slot selection overlay ───────────────────────────────────────

    private void OnGUI()
    {
        if (!_isPaused || !_showSaveSlots) return;
        BuildStyles();

        float ph = 52f + SaveSystem.SlotCount * (SlotH + 6f) + 46f;
        float px = (Screen.width  - PW) * .5f;
        float py = (Screen.height - ph) * .5f;

        // Dim
        DR(new Rect(0, 0, Screen.width, Screen.height), new Color(0,0,0,.50f));

        DR(new Rect(px, py, PW, ph), new Color(.06f,.07f,.08f,.98f));
        DR(new Rect(px, py, PW, 3f), new Color(.45f,.72f,.95f));

        GUI.Label(new Rect(px, py + 10f, PW, 30f), "SAVE TO SLOT", _titleSt);
        DR(new Rect(px + 16f, py + 44f, PW - 32f, 1f), new Color(.25f,.35f,.45f));

        float ry = py + 50f;
        for (int i = 0; i < SaveSystem.SlotCount; i++) { DrawSaveRow(px, ry, i); ry += SlotH + 6f; }

        DR(new Rect(px + 16f, ry, PW - 32f, 1f), new Color(.22f,.30f,.38f));
        if (GUI.Button(new Rect(px + PW * .5f - 60f, ry + 10f, 120f, 30f), "← Back", _btnBack))
        {
            _showSaveSlots   = false;
            _saveConfirmSlot = -1;
        }
    }

    private void DrawSaveRow(float px, float ry, int slot)
    {
        SaveSlotMeta meta = SaveSystem.GetSlotMeta(slot);
        bool isCurrent    = slot == SaveSystem.CurrentSlot;

        DR(new Rect(px + 6f, ry + 2f, PW - 12f, SlotH - 4f),
           isCurrent ? new Color(.10f,.14f,.18f) : new Color(.08f,.10f,.12f));

        _slotSt.normal.textColor = isCurrent ? new Color(.6f,.85f,1f) : new Color(.45f,.65f,.88f);
        GUI.Label(new Rect(px + 14f, ry + 8f, 65f, 20f),
                  $"Slot {slot + 1}" + (isCurrent ? " ★" : ""), _slotSt);

        if (meta.hasData)
        {
            _infoSt.normal.textColor = Color.white;
            GUI.Label(new Rect(px + 84f, ry + 6f, PW - 220f, 20f),
                      $"Lv{meta.level}  {meta.money}$", _infoSt);
            _infoSt.normal.textColor = new Color(.55f,.55f,.55f);
            GUI.Label(new Rect(px + 84f, ry + 26f, PW - 220f, 18f), meta.saveDate, _infoSt);

            if (_saveConfirmSlot == slot)
            {
                if (GUI.Button(new Rect(px + PW - 132f, ry + 14f, 118f, 36f), "Overwrite!", _btnOverwrite))
                    DoSave(slot);
            }
            else
            {
                if (GUI.Button(new Rect(px + PW - 132f, ry + 14f, 118f, 36f), "Save Here", _btnSave))
                    _saveConfirmSlot = slot;
            }
        }
        else
        {
            _emptySt.normal.textColor = new Color(.40f,.40f,.40f);
            GUI.Label(new Rect(px + 84f, ry + 18f, PW - 220f, 22f), "— Empty —", _emptySt);

            if (GUI.Button(new Rect(px + PW - 132f, ry + 14f, 118f, 36f), "Save Here", _btnSave))
                DoSave(slot);
        }

        DR(new Rect(px + 14f, ry + SlotH - 2f, PW - 28f, 1f), new Color(.14f,.18f,.22f));
    }

    private void DoSave(int slot)
    {
        SaveSystem.CurrentSlot = slot;
        GameManager.Instance?.SaveGame();
        _showSaveSlots   = false;
        _saveConfirmSlot = -1;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DR(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void BuildStyles()
    {
        if (_builtSt) return; _builtSt = true;

        _titleSt = S(18, FontStyle.Bold,   TextAnchor.MiddleCenter, new Color(.55f,.80f,1f));
        _infoSt  = S(13, FontStyle.Normal, TextAnchor.MiddleLeft,   Color.white);
        _emptySt = S(14, FontStyle.Normal, TextAnchor.MiddleLeft,   new Color(.4f,.4f,.4f));
        _slotSt  = S(14, FontStyle.Bold,   TextAnchor.MiddleLeft,   new Color(.5f,.72f,1f));

        _btnSave      = Btn(new Color(.16f,.38f,.72f), new Color(.22f,.50f,.88f));
        _btnOverwrite = Btn(new Color(.55f,.20f,.12f), new Color(.70f,.26f,.16f));
        _btnBack      = Btn(new Color(.20f,.20f,.24f), new Color(.28f,.28f,.34f));
        _btnDel       = Btn(new Color(.38f,.12f,.12f), new Color(.52f,.16f,.16f));
    }

    private static GUIStyle S(int sz, FontStyle fs, TextAnchor al, Color col)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = sz, fontStyle = fs, alignment = al };
        s.normal.textColor = col; return s;
    }

    private static GUIStyle Btn(Color n, Color h)
    {
        var s = new GUIStyle(GUI.skin.button)
            { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        s.normal.background = s.active.background = Tx(n);
        s.hover.background  = Tx(h);
        s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
        return s;
    }

    private static Texture2D Tx(Color c)
        { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
}
