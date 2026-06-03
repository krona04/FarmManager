using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum PlantingMiniGameType
{
    Timing,
    WaterBalance
}

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerObject = new GameObject("MiniGameManager");
                _instance = managerObject.AddComponent<MiniGameManager>();
            }

            return _instance;
        }
    }

    private const float TimingTrackWidth = 700f;
    private const float TimingMinX = -TimingTrackWidth * 0.5f;
    private const float TimingMaxX = TimingTrackWidth * 0.5f;

    private static MiniGameManager _instance;

    [Header("Shared")]
    [SerializeField] private float maxBoostPercent = 0.35f;

    [Header("Timing Mini Game")]
    [SerializeField] private int timingRounds = 3;
    [SerializeField] private float timingRoundDuration = 1.6f;
    [SerializeField] private float timingMarkerSpeed = 520f;
    [SerializeField] private float timingTargetWidth = 82f;

    [Header("Water Balance Rules")]
    [SerializeField] private float waterGameDuration = 4f;
    [SerializeField] private float waterDecreaseSpeed = 0.28f;
    [SerializeField] private float waterIncreaseSpeed = 0.55f;
    [SerializeField] private float waterOutsidePenaltySpeed = 0.15f;
    [SerializeField] private float waterStartLevel = 0.5f;
    [SerializeField] private float waterTargetMin = 0.4f;
    [SerializeField] private float waterTargetMax = 0.65f;

    [Header("Water Balance UI")]
    [SerializeField] private GameObject waterBalancePanel;
    [SerializeField] private Image waterFillImage;
    [SerializeField] private RectTransform waterMarker;
    [SerializeField] private Image greenZoneImage;
    [SerializeField] private Text waterProgressText;
    [SerializeField] private Image waterProgressFillImage;

    private FarmPlot _targetPlot;
    private PlantingMiniGameType _activeMiniGame;
    private bool _isRunning;

    private int _timingRound;
    private int _timingHits;
    private float _timingRoundTimer;
    private float _timingMarkerX;
    private float _timingTargetX;
    private int _timingDirection = 1;

    private float _waterTimer;
    private float _waterLevel;
    private float _waterSuccessTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        HideWaterUi();
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        switch (_activeMiniGame)
        {
            case PlantingMiniGameType.Timing:
                UpdateTimingMiniGame();
                break;

            case PlantingMiniGameType.WaterBalance:
                UpdateWaterBalanceMiniGame();
                break;
        }
    }

    private void OnGUI()
    {
        if (!_isRunning)
            return;

        GUI.depth = -1000;

        if (_activeMiniGame == PlantingMiniGameType.Timing)
            DrawTimingMiniGame();
        else if (_activeMiniGame == PlantingMiniGameType.WaterBalance)
            DrawWaterBalanceMiniGame();
    }

    public void StartRandomMiniGame(FarmPlot plot)
    {
        if (_isRunning)
            return;

        PlantingMiniGameType miniGameType = Random.value < 0.5f
            ? PlantingMiniGameType.Timing
            : PlantingMiniGameType.WaterBalance;

        StartMiniGame(plot, miniGameType);
    }

    public void StartMiniGame(FarmPlot plot, PlantingMiniGameType miniGameType)
    {
        if (_isRunning || plot == null)
            return;

        _targetPlot = plot;
        _activeMiniGame = miniGameType;
        _isRunning = true;
        FarmPlot.SetPlantingMiniGameActive(true);

        if (miniGameType == PlantingMiniGameType.Timing)
            StartTimingMiniGame();
        else
            StartWaterBalanceMiniGame();
    }

    private void StartTimingMiniGame()
    {
        HideWaterUi();

        _timingRound = 0;
        _timingHits = 0;
        StartTimingRound();
    }

    private void StartTimingRound()
    {
        _timingRound++;
        _timingRoundTimer = timingRoundDuration;
        _timingMarkerX = TimingMinX;
        _timingDirection = 1;
        _timingTargetX = Random.Range(TimingMinX + timingTargetWidth * 0.5f, TimingMaxX - timingTargetWidth * 0.5f);
    }

    private void UpdateTimingMiniGame()
    {
        _timingRoundTimer -= Time.deltaTime;
        _timingMarkerX += timingMarkerSpeed * _timingDirection * Time.deltaTime;

        if (_timingMarkerX >= TimingMaxX)
        {
            _timingMarkerX = TimingMaxX;
            _timingDirection = -1;
        }
        else if (_timingMarkerX <= TimingMinX)
        {
            _timingMarkerX = TimingMinX;
            _timingDirection = 1;
        }

        if (WasConfirmPressedThisFrame())
            FinishTimingRound(IsTimingHit());
        else if (_timingRoundTimer <= 0f)
            FinishTimingRound(false);
    }

    private bool IsTimingHit()
    {
        float halfWidth = timingTargetWidth * 0.5f;
        return _timingMarkerX >= _timingTargetX - halfWidth && _timingMarkerX <= _timingTargetX + halfWidth;
    }

    private void FinishTimingRound(bool hit)
    {
        if (hit)
            _timingHits++;

        if (_timingRound >= timingRounds)
        {
            float score = timingRounds > 0 ? (float)_timingHits / timingRounds : 0f;
            FinishMiniGame(score);
            return;
        }

        StartTimingRound();
    }

    private void StartWaterBalanceMiniGame()
    {
        _waterTimer = 0f;
        _waterLevel = Mathf.Clamp01(waterStartLevel);
        _waterSuccessTime = 0f;

        if (waterBalancePanel != null)
            waterBalancePanel.SetActive(true);

        UpdateWaterUi();
    }

    private void UpdateWaterBalanceMiniGame()
    {
        _waterTimer += Time.deltaTime;

        if (IsWaterButtonHeld())
            _waterLevel += waterIncreaseSpeed * Time.deltaTime;
        else
            _waterLevel -= waterDecreaseSpeed * Time.deltaTime;

        _waterLevel = Mathf.Clamp01(_waterLevel);

        if (IsWaterInTargetZone())
        {
            _waterSuccessTime += Time.deltaTime;
        }
        else
        {
            _waterSuccessTime -= waterOutsidePenaltySpeed * Time.deltaTime;
            _waterSuccessTime = Mathf.Max(0f, _waterSuccessTime);
        }

        UpdateWaterUi();

        if (_waterTimer >= waterGameDuration)
        {
            float score = waterGameDuration > 0f ? _waterSuccessTime / waterGameDuration : 0f;
            FinishMiniGame(Mathf.Clamp01(score));
        }
    }

    private bool IsWaterInTargetZone()
    {
        return _waterLevel >= waterTargetMin && _waterLevel <= waterTargetMax;
    }

    private bool WasConfirmPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
               (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    private bool IsWaterButtonHeld()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (keyboard != null && keyboard.spaceKey.isPressed) ||
               (mouse != null && mouse.leftButton.isPressed);
    }

    private void FinishMiniGame(float score)
    {
        if (_targetPlot != null)
            _targetPlot.ApplyGrowthBoost(score, maxBoostPercent);

        HideWaterUi();

        _targetPlot = null;
        _isRunning = false;
        FarmPlot.SetPlantingMiniGameActive(false);
    }

    private void UpdateWaterUi()
    {
        if (waterFillImage != null)
            waterFillImage.fillAmount = _waterLevel;

        if (waterMarker != null)
            waterMarker.anchorMin = new Vector2(_waterLevel, waterMarker.anchorMin.y);

        if (greenZoneImage != null)
        {
            RectTransform zoneTransform = greenZoneImage.rectTransform;
            zoneTransform.anchorMin = new Vector2(waterTargetMin, zoneTransform.anchorMin.y);
            zoneTransform.anchorMax = new Vector2(waterTargetMax, zoneTransform.anchorMax.y);
        }

        float progress = waterGameDuration > 0f ? Mathf.Clamp01(_waterSuccessTime / waterGameDuration) : 0f;

        if (waterProgressText != null)
            waterProgressText.text = $"Success: {Mathf.RoundToInt(progress * 100f)}%";

        if (waterProgressFillImage != null)
            waterProgressFillImage.fillAmount = progress;
    }

    private void HideWaterUi()
    {
        if (waterBalancePanel != null)
            waterBalancePanel.SetActive(false);
    }

    private void DrawTimingMiniGame()
    {
        float panelWidth = Mathf.Min(Screen.width * 0.62f, 820f);
        float panelHeight = 118f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height * 0.62f;

        DrawPanel(panelX, panelY, panelWidth, panelHeight, "Growth Boost", $"{_timingRound}/{timingRounds}  Hits: {_timingHits}  Press Space");

        float trackWidth = panelWidth - 90f;
        float trackHeight = 24f;
        float trackX = panelX + 45f;
        float trackY = panelY + 68f;

        DrawRect(new Rect(trackX, trackY, trackWidth, trackHeight), new Color(0.18f, 0.19f, 0.17f, 1f));

        float unit = trackWidth / (TimingMaxX - TimingMinX);
        float targetScreenWidth = timingTargetWidth * unit;
        float targetScreenX = trackX + (_timingTargetX - TimingMinX) * unit - targetScreenWidth * 0.5f;
        DrawRect(new Rect(targetScreenX, trackY, targetScreenWidth, trackHeight), new Color(0.34f, 0.82f, 0.36f, 1f));

        float markerScreenX = trackX + (_timingMarkerX - TimingMinX) * unit - 5f;
        DrawRect(new Rect(markerScreenX, trackY - 13f, 10f, trackHeight + 26f), new Color(1f, 0.86f, 0.28f, 1f));
    }

    private void DrawWaterBalanceMiniGame()
    {
        float panelWidth = Mathf.Min(Screen.width * 0.62f, 820f);
        float panelHeight = 138f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height * 0.62f;

        float progress = waterGameDuration > 0f ? Mathf.Clamp01(_waterSuccessTime / waterGameDuration) : 0f;
        DrawPanel(panelX, panelY, panelWidth, panelHeight, "Water Balance", $"Hold Space or Mouse  Success: {Mathf.RoundToInt(progress * 100f)}%");

        float meterWidth = panelWidth - 90f;
        float meterHeight = 24f;
        float meterX = panelX + 45f;
        float meterY = panelY + 72f;

        DrawRect(new Rect(meterX, meterY, meterWidth, meterHeight), new Color(0.14f, 0.18f, 0.2f, 1f));

        float zoneX = meterX + meterWidth * waterTargetMin;
        float zoneWidth = meterWidth * (waterTargetMax - waterTargetMin);
        DrawRect(new Rect(zoneX, meterY, zoneWidth, meterHeight), new Color(0.34f, 0.82f, 0.36f, 1f));

        DrawRect(new Rect(meterX, meterY, meterWidth * _waterLevel, meterHeight), new Color(0.22f, 0.58f, 1f, 0.78f));

        float markerX = meterX + meterWidth * _waterLevel - 5f;
        DrawRect(new Rect(markerX, meterY - 13f, 10f, meterHeight + 26f), new Color(1f, 1f, 1f, 1f));

        DrawRect(new Rect(meterX, panelY + 112f, meterWidth, 10f), new Color(0.18f, 0.19f, 0.17f, 1f));
        DrawRect(new Rect(meterX, panelY + 112f, meterWidth * progress, 10f), new Color(1f, 0.86f, 0.28f, 1f));
    }

    private void DrawPanel(float x, float y, float width, float height, string title, string subtitle)
    {
        DrawRect(new Rect(x, y, width, height), new Color(0.02f, 0.025f, 0.02f, 0.92f));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };
        smallStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x, y + 8f, width, 28f), title, titleStyle);
        GUI.Label(new Rect(x, y + 36f, width, 22f), subtitle, smallStyle);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
