using UnityEngine;

/// <summary>
/// XP and level system. Each level-up grants 1 upgrade point
/// that can be spent in FarmUpgradeShop (talent tree).
/// </summary>
public class PlayerLevel : MonoBehaviour
{
    public static PlayerLevel Instance { get; private set; }

    [Header("State")]
    public int level          = 1;
    public int xp             = 0;
    public int upgradePoints  = 0;

    // ── Level-up notification ────────────────────────────────────────────────
    private float  _levelUpTimer;
    private int    _lastLevelShown;
    private const float NotifDur = 3f;

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _levelUpStyle;
    private bool     _builtStyle;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_levelUpTimer > 0f) _levelUpTimer -= Time.unscaledDeltaTime;
    }

    // ── XP / Levelling ────────────────────────────────────────────────────────

    /// <summary>XP required to reach the NEXT level from the current one.</summary>
    public int XpToNextLevel => 100 + (level - 1) * 50;   // 100, 150, 200, 250 …

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        xp += amount;

        while (xp >= XpToNextLevel)
        {
            xp    -= XpToNextLevel;
            level++;
            upgradePoints++;
            _levelUpTimer    = NotifDur;
            _lastLevelShown  = level;
            Debug.Log($"[PlayerLevel] Level up → {level}! Points: {upgradePoints}");
        }
    }

    /// <summary>Returns true and spends 1 upgrade point; false if none available.</summary>
    public bool SpendPoint()
    {
        if (upgradePoints <= 0) return false;
        upgradePoints--;
        return true;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (PauseMenu.IsPaused || _levelUpTimer <= 0f) return;
        BuildStyle();

        float a = Mathf.Clamp01(_levelUpTimer);
        _levelUpStyle.normal.textColor = new Color(1f, .92f, .3f, a);

        string msg = $"⬆  LEVEL UP!  Now level {_lastLevelShown}  |  +1 Upgrade Point";
        GUI.Label(new Rect(0, Screen.height * .38f, Screen.width, 44f), msg, _levelUpStyle);
    }

    private void BuildStyle()
    {
        if (_builtStyle) return; _builtStyle = true;
        _levelUpStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
