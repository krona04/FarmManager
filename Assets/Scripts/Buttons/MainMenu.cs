using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller.
/// Handles save-slot selection and settings panel.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root Canvas of the main menu.")]
    public Canvas mainMenuCanvas;

    [Tooltip("Exact name of the game scene in Build Settings.")]
    public string gameSceneName = "MainScene";

    // ── Panel state ───────────────────────────────────────────────────────────
    private enum Panel { None, Slots, Settings }
    private Panel _panel         = Panel.None;
    private int   _deleteConfirm = -1;

    // ── Settings temp values (local copies while panel is open) ───────────────
    private float _tmpMusic;
    private float _tmpSfx;
    private float _tmpSens;

    // ── Layout constants ──────────────────────────────────────────────────────
    private const float SLOT_PW = 540f;
    private const float SLOT_SH = 82f;
    private const float SET_PW  = 590f;
    private const float SET_PH  = 516f;  // traced: header(58)+3×sect(96)+6×row(264)+3×div(42)+footer(56)
    private const float PAD     = 18f;

    private static readonly string[] QualityLabels = { "Low", "Medium", "High", "Ultra" };

    // ── Farm warm palette ─────────────────────────────────────────────────────
    // Background overlay — warm dark earth
    private static readonly Color C_Overlay    = new Color(0.08f, 0.04f, 0.01f, 0.65f);
    // Panel border — dark wood
    private static readonly Color C_Border     = new Color(0.24f, 0.13f, 0.04f, 1.00f);
    // Panel face — warm caramel wood
    private static readonly Color C_Panel      = new Color(0.69f, 0.49f, 0.27f, 1.00f);
    // Panel header strip — darker wood
    private static readonly Color C_Header     = new Color(0.26f, 0.14f, 0.05f, 0.65f);
    // Section bar — deep mahogany
    private static readonly Color C_Section    = new Color(0.28f, 0.14f, 0.04f, 1.00f);
    // Gold accent line
    private static readonly Color C_Gold       = new Color(0.90f, 0.70f, 0.22f, 1.00f);
    // Dim gold (dividers / subtle lines)
    private static readonly Color C_GoldDim    = new Color(0.58f, 0.42f, 0.12f, 1.00f);
    // Divider bottom line
    private static readonly Color C_Divider    = new Color(0.32f, 0.18f, 0.06f, 1.00f);
    // Slot row background
    private static readonly Color C_SlotBg     = new Color(0.24f, 0.13f, 0.04f, 0.65f);
    // Slider track background
    private static readonly Color C_TrackBg    = new Color(0.20f, 0.11f, 0.04f, 1.00f);

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _infoSt, _emptyInfoSt, _slotNameSt;
    private GUIStyle _btnLoad, _btnNew, _btnDel, _btnDelConf, _btnBack;
    private GUIStyle _sectionSt, _rowLabelSt, _valSt;
    private GUIStyle _btnOn, _btnOff;
    private GUIStyle _sliderBg, _sliderThumb;
    private bool     _builtSt;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        SaveSystem.MigrateLegacySave();
        SettingsManager.ApplyAll();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Public button callbacks (assigned in Inspector) ───────────────────────

    public void PlayGame()
    {
        _panel         = Panel.Slots;
        _deleteConfirm = -1;
    }

    public void OpenSettings()
    {
        _panel    = Panel.Settings;
        _tmpMusic = SettingsManager.MusicVolume;
        _tmpSfx   = SettingsManager.SfxVolume;
        _tmpSens  = SettingsManager.MouseSensitivity;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Internal navigation ───────────────────────────────────────────────────

    private void LoadSlot(int slot)
    {
        SaveSystem.CurrentSlot = slot;
        SceneManager.LoadScene(gameSceneName);
    }

    private void NewGameSlot(int slot)
    {
        SaveSystem.DeleteSlot(slot);
        SaveSystem.CurrentSlot = slot;
        SceneManager.LoadScene(gameSceneName);
    }

    private void BackToMenu()
    {
        _panel         = Panel.None;
        _deleteConfirm = -1;
        SettingsManager.Save();
    }

    // ── OnGUI dispatcher ──────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (_panel == Panel.None) return;
        BuildStyles();

        if (_panel == Panel.Settings) DrawSettings();
        else                          DrawSlots();
    }

    // =========================================================================
    //  SAVE SLOT PANEL
    // =========================================================================

    private void DrawSlots()
    {
        float ph = 60f + SaveSystem.SlotCount * (SLOT_SH + 8f) + 54f;
        float px = (Screen.width  - SLOT_PW) * .5f;
        float py = (Screen.height - ph)       * .5f;

        DR(new Rect(0, 0, Screen.width, Screen.height), C_Overlay);

        // Outer border
        DR(new Rect(px - 4f, py - 4f, SLOT_PW + 8f, ph + 8f), C_Border);
        // Panel face
        DR(new Rect(px, py, SLOT_PW, ph), C_Panel);
        // Header tint
        DR(new Rect(px, py, SLOT_PW, 54f), C_Header);
        // Gold top bar
        DR(new Rect(px, py, SLOT_PW, 4f), C_Gold);

        GUI.Label(new Rect(px, py + 8f, SLOT_PW, 36f), "SELECT SAVE SLOT", _titleSt);
        Divline(px + PAD, py + 50f, SLOT_PW - PAD * 2f);

        float ry = py + 58f;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            DrawSlotRow(px, ry, i);
            ry += SLOT_SH + 8f;
        }

        Divline(px + PAD, ry, SLOT_PW - PAD * 2f);

        if (GUI.Button(new Rect(px + SLOT_PW * .5f - 76f, ry + 10f, 152f, 36f), "← Back", _btnBack))
            BackToMenu();
    }

    private void DrawSlotRow(float px, float ry, int slot)
    {
        SaveSlotMeta meta = SaveSystem.GetSlotMeta(slot);

        DR(new Rect(px + 8f, ry + 2f, SLOT_PW - 16f, SLOT_SH - 4f), C_SlotBg);
        // Coloured left tab: gold if has data, dim if empty
        DR(new Rect(px + 8f, ry + 2f, 4f, SLOT_SH - 4f), meta.hasData ? C_Gold : C_GoldDim);

        GUI.Label(new Rect(px + 18f, ry + 8f, 76f, 24f), $"Slot {slot + 1}", _slotNameSt);

        if (meta.hasData)
        {
            _infoSt.normal.textColor = new Color(0.93f, 0.86f, 0.68f);
            GUI.Label(new Rect(px + 100f, ry + 8f,  SLOT_PW - 290f, 22f),
                      $"Level {meta.level}   |   ${meta.money}", _infoSt);
            _infoSt.normal.textColor = new Color(0.62f, 0.52f, 0.36f);
            GUI.Label(new Rect(px + 100f, ry + 32f, SLOT_PW - 290f, 20f), meta.saveDate, _infoSt);

            if (GUI.Button(new Rect(px + SLOT_PW - 238f, ry + 20f, 90f, 36f), "Load", _btnLoad))
            {
                _deleteConfirm = -1;
                LoadSlot(slot);
            }

            if (_deleteConfirm == slot)
            {
                if (GUI.Button(new Rect(px + SLOT_PW - 142f, ry + 20f, 126f, 36f), "Confirm!", _btnDelConf))
                {
                    SaveSystem.DeleteSlot(slot);
                    _deleteConfirm = -1;
                }
            }
            else
            {
                if (GUI.Button(new Rect(px + SLOT_PW - 142f, ry + 20f, 126f, 36f), "Delete", _btnDel))
                    _deleteConfirm = slot;
            }
        }
        else
        {
            _emptyInfoSt.normal.textColor = new Color(0.58f, 0.48f, 0.32f);
            GUI.Label(new Rect(px + 100f, ry + 24f, SLOT_PW - 290f, 24f), "— Empty —", _emptyInfoSt);

            if (GUI.Button(new Rect(px + SLOT_PW - 142f, ry + 20f, 126f, 36f), "New Game", _btnNew))
            {
                _deleteConfirm = -1;
                NewGameSlot(slot);
            }
        }

        DR(new Rect(px + PAD, ry + SLOT_SH - 2f, SLOT_PW - PAD * 2f, 1f), C_Divider);
    }

    // =========================================================================
    //  SETTINGS PANEL
    // =========================================================================

    private void DrawSettings()
    {
        float px = (Screen.width  - SET_PW) * .5f;
        float py = (Screen.height - SET_PH)  * .5f;

        DR(new Rect(0, 0, Screen.width, Screen.height), C_Overlay);

        // Outer border
        DR(new Rect(px - 4f, py - 4f, SET_PW + 8f, SET_PH + 8f), C_Border);
        // Panel face
        DR(new Rect(px, py, SET_PW, SET_PH), C_Panel);
        // Header tint
        DR(new Rect(px, py, SET_PW, 52f), C_Header);
        // Gold top bar
        DR(new Rect(px, py, SET_PW, 4f), C_Gold);

        GUI.Label(new Rect(px, py + 8f, SET_PW, 34f), "SETTINGS", _titleSt);
        Divline(px + PAD, py + 50f, SET_PW - PAD * 2f);

        float y = py + 58f;

        // ── AUDIO ─────────────────────────────────────────────────────────────
        y = DrawSection(px, y, "AUDIO");

        float m2 = DrawSlider(px, y, "Music Volume", _tmpMusic, 0f, 1f,
                               $"{Mathf.RoundToInt(_tmpMusic * 100)}%");
        if (!Mathf.Approximately(m2, _tmpMusic))
        { _tmpMusic = m2; SettingsManager.MusicVolume = _tmpMusic; }
        y += 44f;

        float s2 = DrawSlider(px, y, "Sound Effects", _tmpSfx, 0f, 1f,
                               $"{Mathf.RoundToInt(_tmpSfx * 100)}%");
        if (!Mathf.Approximately(s2, _tmpSfx))
        { _tmpSfx = s2; SettingsManager.SfxVolume = _tmpSfx; }
        y += 44f;

        y = DrawDivider(px, y);

        // ── GRAPHICS ──────────────────────────────────────────────────────────
        y = DrawSection(px, y, "GRAPHICS");

        GUI.Label(new Rect(px + PAD + 4f, y + 7f, 200f, 28f), "Fullscreen", _rowLabelSt);
        bool fs = SettingsManager.Fullscreen;
        if (GUI.Button(new Rect(px + 222f, y + 3f, 78f, 32f), "ON",  fs  ? _btnOn : _btnOff)) SettingsManager.Fullscreen = true;
        if (GUI.Button(new Rect(px + 306f, y + 3f, 78f, 32f), "OFF", !fs ? _btnOn : _btnOff)) SettingsManager.Fullscreen = false;
        y += 44f;

        GUI.Label(new Rect(px + PAD + 4f, y + 7f, 200f, 28f), "Graphics Quality", _rowLabelSt);
        int qCur   = SettingsManager.QualityIndex;
        int qCount = QualitySettings.names.Length;
        for (int i = 0; i < QualityLabels.Length; i++)
        {
            int qIdx   = Mathf.Clamp(Mathf.RoundToInt(i * (qCount - 1f) / (QualityLabels.Length - 1f)),
                                     0, qCount - 1);
            bool active = qCur == qIdx;
            if (GUI.Button(new Rect(px + 222f + i * 88f, y + 3f, 84f, 32f),
                           QualityLabels[i], active ? _btnOn : _btnOff))
                SettingsManager.QualityIndex = qIdx;
        }
        y += 44f;

        y = DrawDivider(px, y);

        // ── CONTROLS ──────────────────────────────────────────────────────────
        y = DrawSection(px, y, "CONTROLS");

        float n2 = DrawSlider(px, y, "Mouse Sensitivity", _tmpSens, 0.5f, 30f,
                               $"{_tmpSens:F1}");
        if (!Mathf.Approximately(n2, _tmpSens))
        { _tmpSens = n2; SettingsManager.MouseSensitivity = _tmpSens; }
        y += 44f;

        GUI.Label(new Rect(px + PAD + 4f, y + 7f, 200f, 28f), "Shuffle Playlist", _rowLabelSt);
        bool shuf = SettingsManager.ShufflePlaylist;
        if (GUI.Button(new Rect(px + 222f, y + 3f, 78f, 32f), "ON",  shuf  ? _btnOn : _btnOff)) SettingsManager.ShufflePlaylist = true;
        if (GUI.Button(new Rect(px + 306f, y + 3f, 78f, 32f), "OFF", !shuf ? _btnOn : _btnOff)) SettingsManager.ShufflePlaylist = false;
        y += 44f;

        y = DrawDivider(px, y);

        if (GUI.Button(new Rect(px + SET_PW * .5f - 80f, y + 8f, 160f, 36f), "← Back", _btnBack))
            BackToMenu();
    }

    // ── Panel drawing helpers ─────────────────────────────────────────────────

    private float DrawSection(float px, float y, string label)
    {
        DR(new Rect(px + 8f, y, SET_PW - 16f, 28f), C_Section);
        DR(new Rect(px + 8f, y, SET_PW - 16f, 2f),  C_GoldDim);
        GUI.Label(new Rect(px + PAD + 4f, y + 3f, SET_PW, 22f), label, _sectionSt);
        return y + 32f;
    }

    private float DrawDivider(float px, float y)
    {
        DR(new Rect(px + PAD, y,      SET_PW - PAD * 2f, 1f), C_GoldDim);
        DR(new Rect(px + PAD, y + 2f, SET_PW - PAD * 2f, 1f), C_Divider);
        return y + 14f;
    }

    private void Divline(float x, float y, float w)
    {
        DR(new Rect(x, y,      w, 1f), C_GoldDim);
        DR(new Rect(x, y + 1f, w, 1f), C_Divider);
    }

    private float DrawSlider(float px, float y, string label, float val, float min, float max, string valText)
    {
        GUI.Label(new Rect(px + PAD + 4f, y + 8f, 200f, 26f), label, _rowLabelSt);

        // Draw custom track
        Rect track = new Rect(px + 224f, y + 15f, SET_PW - 224f - PAD - 62f, 12f);
        DR(track, C_TrackBg);
        float fillW = Mathf.Clamp01((val - min) / Mathf.Max(0.001f, max - min)) * track.width;
        DR(new Rect(track.x, track.y, fillW, track.height), C_Gold);

        // Invisible interactive slider on top of custom track
        float newVal = GUI.HorizontalSlider(
            new Rect(track.x - 7f, track.y - 6f, track.width + 14f, track.height + 12f),
            val, min, max, _sliderBg, _sliderThumb);

        GUI.Label(new Rect(px + SET_PW - PAD - 60f, y + 6f, 58f, 28f), valText, _valSt);

        return newVal;
    }

    // ── Style building ────────────────────────────────────────────────────────

    private void BuildStyles()
    {
        if (_builtSt) return; _builtSt = true;

        // Shared text colours
        Color cream  = new Color(0.98f, 0.91f, 0.66f);
        Color parch  = new Color(0.90f, 0.82f, 0.60f);
        Color dimBrn = new Color(0.60f, 0.50f, 0.34f);
        Color goldTx = new Color(0.98f, 0.84f, 0.38f);

        _titleSt     = St(22, FontStyle.Bold,   TextAnchor.MiddleCenter, cream);
        _infoSt      = St(14, FontStyle.Normal, TextAnchor.MiddleLeft,   parch);
        _emptyInfoSt = St(15, FontStyle.Normal, TextAnchor.MiddleLeft,   dimBrn);
        _slotNameSt  = St(15, FontStyle.Bold,   TextAnchor.MiddleLeft,   goldTx);

        // Slot buttons
        _btnLoad    = BtnSt(new Color(0.52f, 0.36f, 0.12f), new Color(0.65f, 0.48f, 0.18f), cream);
        _btnNew     = BtnSt(new Color(0.22f, 0.46f, 0.12f), new Color(0.30f, 0.58f, 0.18f), cream);
        _btnDel     = BtnSt(new Color(0.50f, 0.16f, 0.10f), new Color(0.62f, 0.22f, 0.14f), cream);
        _btnDelConf = BtnSt(new Color(0.66f, 0.18f, 0.12f), new Color(0.80f, 0.24f, 0.16f), cream);
        _btnBack    = BtnSt(new Color(0.28f, 0.15f, 0.05f), new Color(0.40f, 0.24f, 0.10f), cream);

        // Settings labels
        _sectionSt  = St(13, FontStyle.Bold,   TextAnchor.MiddleLeft,  new Color(0.96f, 0.80f, 0.38f));
        _rowLabelSt = St(14, FontStyle.Normal, TextAnchor.MiddleLeft,  parch);
        _valSt      = St(13, FontStyle.Bold,   TextAnchor.MiddleRight, goldTx);

        // Toggle buttons
        _btnOn  = BtnSt(new Color(0.24f, 0.50f, 0.12f), new Color(0.32f, 0.62f, 0.18f), cream);
        _btnOff = BtnSt(new Color(0.30f, 0.18f, 0.06f), new Color(0.42f, 0.26f, 0.10f), dimBrn);

        // Slider — transparent track (we draw our own), gold thumb
        _sliderBg = new GUIStyle(GUI.skin.horizontalSlider);
        _sliderBg.normal.background =
        _sliderBg.hover.background  =
        _sliderBg.active.background = Tx(new Color(0, 0, 0, 0));

        _sliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
        _sliderThumb.normal.background = Tx(new Color(0.96f, 0.78f, 0.28f));
        _sliderThumb.hover.background  = Tx(new Color(1.00f, 0.91f, 0.52f));
        _sliderThumb.active.background = Tx(new Color(1.00f, 0.96f, 0.65f));
        _sliderThumb.fixedWidth        = 12f;
        _sliderThumb.fixedHeight       = 20f;
    }

    // ── Low-level helpers ─────────────────────────────────────────────────────

    private static GUIStyle St(int sz, FontStyle fs, TextAnchor al, Color col)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = sz, fontStyle = fs, alignment = al };
        s.normal.textColor = col;
        return s;
    }

    private static GUIStyle BtnSt(Color n, Color h, Color textCol)
    {
        var s = new GUIStyle(GUI.skin.button)
            { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        s.normal.background = s.active.background = Tx(n);
        s.hover.background  = Tx(h);
        s.normal.textColor = s.hover.textColor = s.active.textColor = textCol;
        return s;
    }

    private static void DR(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private static Texture2D Tx(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
