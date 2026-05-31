using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller.
/// "Play Game" button opens an OnGUI save-slot selection panel.
/// Selecting a slot sets SaveSystem.CurrentSlot and loads the game scene.
/// </summary>
public class MainMenu : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────────────────
    private bool _showSlots   = false;
    private int  _deleteConfirm = -1;   // slot index awaiting delete confirmation

    // ── Slot panel layout ─────────────────────────────────────────────────────
    private const float PW     = 520f;
    private const float SlotH  = 80f;
    private const float PadX   = 20f;

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _infoSt, _emptyInfoSt, _slotNameSt;
    private GUIStyle _btnLoad, _btnNew, _btnDel, _btnDelConf, _btnBack;
    private bool     _builtSt;

    // ────────────────────────────────────────────────────────────────────────

    // Called by "Play Game" Unity UI button
    public void PlayGame()
    {
        _showSlots    = true;
        _deleteConfirm = -1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // Called by "Quit" Unity UI button
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ────────────────────────────────────────────────────────────────────────

    private void LoadSlot(int slot)
    {
        SaveSystem.CurrentSlot = slot;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void NewGameSlot(int slot)
    {
        SaveSystem.DeleteSlot(slot);
        SaveSystem.CurrentSlot = slot;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!_showSlots) return;
        BuildStyles();

        // Dim background
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, .55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;

        float ph = 60f + SaveSystem.SlotCount * (SlotH + 8f) + 52f;
        float px = (Screen.width  - PW) * .5f;
        float py = (Screen.height - ph) * .5f;

        DrawR(new Rect(px, py, PW, ph),    new Color(.06f,.07f,.08f,.98f));
        DrawR(new Rect(px, py, PW, 3f),    new Color(.45f,.72f,.95f));
        GUI.Label(new Rect(px, py + 10f, PW, 34f), "SELECT SAVE SLOT", _titleSt);
        DrawR(new Rect(px + PadX, py + 48f, PW - PadX * 2f, 1f), new Color(.25f,.35f,.45f));

        float ry = py + 56f;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            DrawSlotRow(px, ry, i);
            ry += SlotH + 8f;
        }

        DrawR(new Rect(px + PadX, ry, PW - PadX * 2f, 1f), new Color(.22f,.30f,.38f));
        if (GUI.Button(new Rect(px + PW * .5f - 70f, ry + 10f, 140f, 32f), "← Back", _btnBack))
        {
            _showSlots     = false;
            _deleteConfirm = -1;
        }
    }

    private void DrawSlotRow(float px, float ry, int slot)
    {
        SaveSlotMeta meta = SaveSystem.GetSlotMeta(slot);

        // Row bg
        DrawR(new Rect(px + 8f, ry + 2f, PW - 16f, SlotH - 4f), new Color(.09f,.11f,.14f));

        // Slot label
        _slotNameSt.normal.textColor = new Color(.55f,.75f,1f);
        GUI.Label(new Rect(px + 16f, ry + 8f, 70f, 24f), $"Slot {slot + 1}", _slotNameSt);

        if (meta.hasData)
        {
            // Info
            _infoSt.normal.textColor = Color.white;
            GUI.Label(new Rect(px + 90f, ry + 8f, PW - 280f, 22f),
                      $"Level {meta.level}   |   {meta.money}$", _infoSt);
            _infoSt.normal.textColor = new Color(.6f,.6f,.6f);
            GUI.Label(new Rect(px + 90f, ry + 32f, PW - 280f, 20f), meta.saveDate, _infoSt);

            // Load button
            if (GUI.Button(new Rect(px + PW - 226f, ry + 18f, 90f, 36f), "Load", _btnLoad))
            {
                _deleteConfirm = -1;
                LoadSlot(slot);
            }

            // Delete / Confirm
            if (_deleteConfirm == slot)
            {
                if (GUI.Button(new Rect(px + PW - 130f, ry + 18f, 114f, 36f), "Confirm Delete", _btnDelConf))
                {
                    SaveSystem.DeleteSlot(slot);
                    _deleteConfirm = -1;
                }
            }
            else
            {
                if (GUI.Button(new Rect(px + PW - 130f, ry + 18f, 114f, 36f), "Delete", _btnDel))
                    _deleteConfirm = slot;
            }
        }
        else
        {
            // Empty slot
            _emptyInfoSt.normal.textColor = new Color(.45f,.45f,.45f);
            GUI.Label(new Rect(px + 90f, ry + 22f, PW - 280f, 24f), "— Empty —", _emptyInfoSt);

            if (GUI.Button(new Rect(px + PW - 130f, ry + 18f, 114f, 36f), "New Game", _btnNew))
            {
                _deleteConfirm = -1;
                NewGameSlot(slot);
            }
        }

        DrawR(new Rect(px + PadX, ry + SlotH - 2f, PW - PadX * 2f, 1f), new Color(.14f,.18f,.22f));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawR(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void BuildStyles()
    {
        if (_builtSt) return; _builtSt = true;

        _titleSt = St(22, FontStyle.Bold,   TextAnchor.MiddleCenter, new Color(.55f,.80f,1f));
        _infoSt  = St(14, FontStyle.Normal, TextAnchor.MiddleLeft,   Color.white);
        _emptyInfoSt = St(15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(.45f,.45f,.45f));
        _slotNameSt  = St(15, FontStyle.Bold,   TextAnchor.MiddleLeft, new Color(.55f,.75f,1f));

        _btnLoad    = BtnSt(new Color(.15f,.38f,.72f), new Color(.20f,.50f,.88f), Color.white);
        _btnNew     = BtnSt(new Color(.16f,.40f,.18f), new Color(.22f,.54f,.24f), Color.white);
        _btnDel     = BtnSt(new Color(.35f,.12f,.12f), new Color(.50f,.16f,.16f), Color.white);
        _btnDelConf = BtnSt(new Color(.60f,.18f,.18f), new Color(.75f,.22f,.22f), Color.white);
        _btnBack    = BtnSt(new Color(.20f,.20f,.24f), new Color(.28f,.28f,.34f), Color.white);
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
