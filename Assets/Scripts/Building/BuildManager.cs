using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance      { get; private set; }
    public static bool         IsBuildModeActive { get; private set; }
    public static bool         IsBuildMenuOpen   { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Buildable Items")]
    public BuildableItem[] buildableItems;

    [Header("Placement")]
    public float     maxPlaceDistance = 12f;
    public float     gridSize         = 1f;
    public bool      snapToGrid       = true;
    public LayerMask groundMask       = ~0;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private int        _sel;
    private GameObject _ghost;
    private Renderer[] _ghostRend;
    private float      _ghostRotY;
    private bool       _canPlace;
    private Material   _matOk, _matNo;

    private readonly List<PlacedBuilding> _placed = new();

    private struct PlacedBuilding
    {
        public int        idx;
        public GameObject go;
        public FarmPlot   plot;
    }

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _titleSt, _infoSt, _nameSt, _nameDisSt, _btnSt, _btnDisSt, _hudSt;
    private bool     _built;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateMats();
    }

    private void Update()
    {
        if (ShopMenu.IsShopOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Tab: toggle build menu
        if (kb.tabKey.wasPressedThisFrame)
        {
            if      (IsBuildModeActive) { ExitBuildMode(); OpenMenu(); }
            else if (IsBuildMenuOpen)   CloseMenu();
            else                        OpenMenu();
            return;
        }

        if (IsBuildMenuOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame) CloseMenu();
            return;
        }

        if (IsBuildModeActive) { UpdateGhost(); HandleBuildInput(); }
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    private void OpenMenu()
    {
        IsBuildMenuOpen  = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void CloseMenu()
    {
        IsBuildMenuOpen  = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void EnterBuildMode(int idx)
    {
        _sel = idx; _ghostRotY = 0f;
        IsBuildModeActive = true;
        IsBuildMenuOpen   = false;
        Cursor.lockState  = CursorLockMode.Locked;
        Cursor.visible    = false;
        SpawnGhost();
    }

    public void ExitBuildMode()
    {
        IsBuildModeActive = false;
        KillGhost();
    }

    // ── Ghost ─────────────────────────────────────────────────────────────────

    private void SpawnGhost()
    {
        KillGhost();
        if (buildableItems == null || _sel >= buildableItems.Length) return;
        var item = buildableItems[_sel];
        if (item.prefab == null) return;

        _ghost = Instantiate(item.prefab);
        _ghost.SetActive(false);

        foreach (var c  in _ghost.GetComponentsInChildren<Collider>())  c.enabled  = false;
        foreach (var fp in _ghost.GetComponentsInChildren<FarmPlot>())  fp.enabled = false;
        _ghostRend = _ghost.GetComponentsInChildren<Renderer>();
    }

    private void KillGhost()
    {
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        _ghostRend = null;
    }

    private void UpdateGhost()
    {
        if (_ghost == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, groundMask))
        {
            Vector3 pos = hit.point;
            if (snapToGrid)
                pos = new Vector3(Mathf.Round(pos.x / gridSize) * gridSize,
                                  pos.y,
                                  Mathf.Round(pos.z / gridSize) * gridSize);

            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _ghostRotY, 0f));

            _canPlace = GameManager.Instance != null
                        && GameManager.Instance.money >= buildableItems[_sel].cost;

            var mat = _canPlace ? _matOk : _matNo;
            foreach (var r in _ghostRend) r.material = mat;
            _ghost.SetActive(true);
        }
        else
        {
            _ghost.SetActive(false);
            _canPlace = false;
        }
    }

    // ── Build input ───────────────────────────────────────────────────────────

    private void HandleBuildInput()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            if (kb.escapeKey.wasPressedThisFrame) { ExitBuildMode(); return; }
            if (kb.rKey.wasPressedThisFrame) _ghostRotY = (_ghostRotY + 90f) % 360f;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame
            && _canPlace && _ghost != null && _ghost.activeSelf)
            Place();
    }

    private void Place()
    {
        var item = buildableItems[_sel];
        if (GameManager.Instance == null) return;

        // Farm plots can be placed for free if the player has tokens from the kiosk
        bool usedToken = item.isFarmPlot && (FarmUpgradeManager.Instance?.UseFreePlotToken() ?? false);

        if (!usedToken)
        {
            if (GameManager.Instance.money < item.cost) return;
            GameManager.Instance.money -= item.cost;
            GameManager.Instance.UpdateUI();
        }

        var go  = Instantiate(item.prefab, _ghost.transform.position, _ghost.transform.rotation);
        var pb  = new PlacedBuilding { idx = _sel, go = go };

        if (item.isFarmPlot)
        {
            var fp = go.GetComponent<FarmPlot>() ?? go.GetComponentInChildren<FarmPlot>();
            if (fp != null)
            {
                pb.plot = fp;
                GameManager.Instance.RegisterFarmPlot(fp);
            }
        }

        _placed.Add(pb);
        Debug.Log($"[Build] Placed {item.itemName}");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public List<BuildingSaveData> GetSaveData()
    {
        var list = new List<BuildingSaveData>();
        foreach (var pb in _placed)
        {
            if (pb.go == null) continue;
            var d = new BuildingSaveData
            {
                itemIndex = pb.idx,
                px = pb.go.transform.position.x,
                py = pb.go.transform.position.y,
                pz = pb.go.transform.position.z,
                rotY = pb.go.transform.eulerAngles.y
            };
            if (pb.plot != null) d.plotState = pb.plot.GetSaveData();
            list.Add(d);
        }
        return list;
    }

    public void RestoreBuildings(List<BuildingSaveData> saved)
    {
        if (saved == null || buildableItems == null) return;
        foreach (var d in saved)
        {
            if (d.itemIndex < 0 || d.itemIndex >= buildableItems.Length) continue;
            var item = buildableItems[d.itemIndex];
            if (item.prefab == null) continue;

            var go  = Instantiate(item.prefab,
                                  new Vector3(d.px, d.py, d.pz),
                                  Quaternion.Euler(0f, d.rotY, 0f));
            var pb  = new PlacedBuilding { idx = d.itemIndex, go = go };

            if (item.isFarmPlot)
            {
                var fp = go.GetComponent<FarmPlot>() ?? go.GetComponentInChildren<FarmPlot>();
                if (fp != null)
                {
                    pb.plot = fp;
                    GameManager.Instance?.RegisterFarmPlot(fp);
                    fp.LoadFromSaveData(d.plotState);
                }
            }
            _placed.Add(pb);
        }
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EnsureStyles();
        if (IsBuildMenuOpen)  DrawMenu();
        if (IsBuildModeActive) DrawHUD();

        if (!IsBuildModeActive && !IsBuildMenuOpen && !ShopMenu.IsShopOpen)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.LowerRight };
            s.normal.textColor = new Color(.65f,.65f,.65f,.7f);
            GUI.Label(new Rect(Screen.width - 210f, Screen.height - 34f, 200f, 24f), "[Tab]  Build Menu", s);
        }
    }

    private void DrawMenu()
    {
        if (buildableItems == null || buildableItems.Length == 0) return;
        int   money = GameManager.Instance != null ? GameManager.Instance.money : 0;
        float iH = 64f, pw = 400f;
        float ph = 72f + buildableItems.Length * iH + 48f;
        float px = (Screen.width  - pw) * .5f;
        float py = (Screen.height - ph) * .5f;

        R(new Rect(px, py, pw, ph), new Color(.05f,.07f,.10f,.97f));
        R(new Rect(px, py, pw, 3f), new Color(.35f,.65f,.95f));

        GUI.Label(new Rect(px, py + 10f, pw, 34f), "BUILD MENU", _titleSt);
        GUI.Label(new Rect(px, py + 44f, pw, 22f), $"Money: {money}$", _infoSt);

        float ry = py + 68f;
        for (int i = 0; i < buildableItems.Length; i++) { DrawItem(px, ry, pw, iH, i, money); ry += iH; }

        R(new Rect(px + 20f, ry + 4f, pw - 40f, 1f), new Color(.2f,.28f,.38f));
        if (MBtn(new Rect(px + pw * .5f - 70f, ry + 12f, 140f, 30f), "Close [ESC]", true))
            CloseMenu();
    }

    private void DrawItem(float px, float ry, float pw, float ih, int i, int money)
    {
        var item = buildableItems[i]; bool can = money >= item.cost;
        R(new Rect(px + 8f, ry + 2f, pw - 16f, ih - 4f), new Color(.08f,.11f,.15f));
        R(new Rect(px + 16f, ry + 13f, 38f, 38f), can ? new Color(.35f,.70f,1f) : new Color(.3f,.3f,.35f));
        GUI.Label(new Rect(px + 64f, ry + 8f,  pw - 160f, 24f), item.itemName, can ? _nameSt : _nameDisSt);
        GUI.Label(new Rect(px + 64f, ry + 32f, pw - 160f, 20f), $"Cost: {item.cost}$",   _infoSt);
        if (MBtn(new Rect(px + pw - 118f, ry + 16f, 100f, 32f), "Build", can))
            EnterBuildMode(i);
        R(new Rect(px + 20f, ry + ih - 2f, pw - 40f, 1f), new Color(.12f,.16f,.22f));
    }

    private void DrawHUD()
    {
        var item   = buildableItems[_sel];
        int money  = GameManager.Instance != null ? GameManager.Instance.money : 0;
        int tokens = (item.isFarmPlot && FarmUpgradeManager.Instance != null)
                     ? FarmUpgradeManager.Instance.freePlotTokens : 0;
        bool can   = tokens > 0 || money >= item.cost;

        string costStr = tokens > 0 ? $"FREE (token ×{tokens})" : $"{item.cost}$";

        R(new Rect(0f, Screen.height - 50f, Screen.width, 50f), new Color(0,0,0,.62f));
        _hudSt.normal.textColor = can ? new Color(.55f,1f,.55f) : new Color(1f,.4f,.4f);
        string msg = can
            ? $"Building: {item.itemName}  ({costStr})   [LMB] Place   [R] Rotate   [Tab] Menu   [ESC] Cancel"
            : $"Building: {item.itemName}  ({item.cost}$)   Not enough money!   [ESC] Cancel";
        GUI.Label(new Rect(0f, Screen.height - 46f, Screen.width, 42f), msg, _hudSt);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool MBtn(Rect r, string label, bool en)
    {
        GUI.enabled = en;
        bool c = GUI.Button(r, label, en ? _btnSt : _btnDisSt);
        GUI.enabled = true;
        return c;
    }

    private static void R(Rect r, Color c)
        { Color p = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = p; }

    private void CreateMats()
    {
        _matOk = TransMat(new Color(.25f,1f,.25f,.38f));
        _matNo = TransMat(new Color(1f,.2f,.2f,.38f));
    }

    private static Material TransMat(Color c)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Mode", 3);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        return m;
    }

    private void EnsureStyles()
    {
        if (_built) return; _built = true;

        _titleSt = St(24, FontStyle.Bold,   TextAnchor.MiddleCenter, new Color(.55f,.80f,1f));
        _infoSt  = St(14, FontStyle.Normal, TextAnchor.MiddleLeft,   new Color(.75f,.75f,.75f));
        _nameSt  = St(16, FontStyle.Bold,   TextAnchor.MiddleLeft,   Color.white);
        _nameDisSt = St(16, FontStyle.Bold, TextAnchor.MiddleLeft,   new Color(.45f,.45f,.45f));
        _hudSt   = St(16, FontStyle.Bold,   TextAnchor.MiddleCenter, Color.white);

        _btnSt  = BtnSt(new Color(.18f,.40f,.72f), new Color(.25f,.52f,.88f));
        _btnDisSt = BtnSt(new Color(.22f,.22f,.24f), new Color(.22f,.22f,.24f));
        _btnDisSt.normal.textColor = new Color(.45f,.45f,.45f);
    }

    private static GUIStyle St(int sz, FontStyle fs, TextAnchor al, Color col)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = sz, fontStyle = fs, alignment = al };
        s.normal.textColor = col; return s;
    }

    private static GUIStyle BtnSt(Color n, Color h)
    {
        var s = new GUIStyle(GUI.skin.button)
            { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        s.normal.background = s.active.background = Tx(n);
        s.hover.background  = Tx(h);
        s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
        return s;
    }

    private static Texture2D Tx(Color c) { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
}
