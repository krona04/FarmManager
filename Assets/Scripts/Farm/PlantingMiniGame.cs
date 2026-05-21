using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlantingMiniGame : MonoBehaviour
{
    private const float TrackWidth = 700f;
    private const float MinTargetX = -TrackWidth * 0.5f;
    private const float MaxTargetX = TrackWidth * 0.5f;

    private static PlantingMiniGame _instance;

    [Header("Rules")]
    [SerializeField] private int roundsToPlay = 3;
    [SerializeField] private float roundDuration = 1.6f;
    [SerializeField] private float markerSpeed = 520f;
    [SerializeField] private float targetWidth = 82f;
    [SerializeField] private float growthBoostPerHit = 4f;
    [SerializeField] private float growthBoostOnMiss = 1f;

    private FarmPlot _targetPlot;
    private Canvas _canvas;
    private GameObject _root;
    private RectTransform _marker;
    private RectTransform _targetZone;
    private Text _titleText;
    private Text _roundText;
    private Text _resultText;

    private int _currentRound;
    private int _hits;
    private float _roundTimer;
    private float _targetX;
    private float _markerX;
    private int _moveDirection = 1;
    private bool _isPlaying;

    public static void StartForPlot(FarmPlot plot)
    {
        if (plot == null)
            return;

        EnsureInstance();
        HideLegacyMiniGameUi();

        _instance.Begin(plot);
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject miniGameObject = new GameObject("Planting Mini Game");
        _instance = miniGameObject.AddComponent<PlantingMiniGame>();
    }

    private static void HideLegacyMiniGameUi()
    {
        HideLegacyObject("MiniGameUI");
        HideLegacyObject("MiniGamePanel");
    }

    private static void HideLegacyObject(string objectName)
    {
        GameObject legacyObject = GameObject.Find(objectName);

        if (legacyObject == null)
            return;

        legacyObject.SetActive(false);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUi();
        Hide();
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        _roundTimer -= Time.unscaledDeltaTime;
        MoveMarker();

        if (WasConfirmPressed())
            FinishRound(IsMarkerInsideTarget());
        else if (_roundTimer <= 0f)
            FinishRound(false);
    }

    private void OnGUI()
    {
        if (!_isPlaying)
            return;

        GUI.depth = -1000;

        float panelWidth = Mathf.Min(Screen.width * 0.62f, 820f);
        float panelHeight = 118f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height * 0.62f;
        Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

        DrawRect(panelRect, new Color(0.02f, 0.025f, 0.02f, 0.92f));

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

        GUI.Label(new Rect(panelX, panelY + 8f, panelWidth, 28f), "Growth Boost", titleStyle);
        GUI.Label(new Rect(panelX, panelY + 34f, panelWidth, 22f), $"{_currentRound}/{roundsToPlay}  Hits: {_hits}  Press Space", smallStyle);

        float trackWidth = panelWidth - 90f;
        float trackHeight = 24f;
        float trackX = panelX + 45f;
        float trackY = panelY + 68f;
        Rect trackRect = new Rect(trackX, trackY, trackWidth, trackHeight);

        DrawRect(trackRect, new Color(0.18f, 0.19f, 0.17f, 1f));

        float unit = trackWidth / (MaxTargetX - MinTargetX);
        float targetScreenWidth = targetWidth * unit;
        float targetScreenX = trackX + (_targetX - MinTargetX) * unit - targetScreenWidth * 0.5f;
        DrawRect(new Rect(targetScreenX, trackY, targetScreenWidth, trackHeight), new Color(0.34f, 0.82f, 0.36f, 1f));

        float markerScreenX = trackX + (_markerX - MinTargetX) * unit - 5f;
        DrawRect(new Rect(markerScreenX, trackY - 13f, 10f, trackHeight + 26f), new Color(1f, 0.86f, 0.28f, 1f));
    }

    private void Begin(FarmPlot plot)
    {
        if (_root == null)
            CreateUi();

        _targetPlot = plot;
        _currentRound = 0;
        _hits = 0;
        _isPlaying = true;

        Show();
        StartRound();

        Debug.Log($"Planting mini game started for {plot.gameObject.name}.");
    }

    private void StartRound()
    {
        _currentRound++;
        _roundTimer = roundDuration;
        _markerX = MinTargetX;
        _moveDirection = 1;
        _targetX = Random.Range(MinTargetX + targetWidth * 0.5f, MaxTargetX - targetWidth * 0.5f);

        _marker.anchoredPosition = new Vector2(_markerX, 0f);
        _targetZone.anchoredPosition = new Vector2(_targetX, 0f);
        _targetZone.sizeDelta = new Vector2(targetWidth, _targetZone.sizeDelta.y);

        _titleText.text = "Growth Boost";
        _roundText.text = $"{_currentRound}/{roundsToPlay}  Hits: {_hits}";
        _resultText.text = "Space";
    }

    private void MoveMarker()
    {
        _markerX += markerSpeed * _moveDirection * Time.unscaledDeltaTime;

        if (_markerX >= MaxTargetX)
        {
            _markerX = MaxTargetX;
            _moveDirection = -1;
        }
        else if (_markerX <= MinTargetX)
        {
            _markerX = MinTargetX;
            _moveDirection = 1;
        }

        _marker.anchoredPosition = new Vector2(_markerX, 0f);
    }

    private bool WasConfirmPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
               (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    private bool IsMarkerInsideTarget()
    {
        float halfWidth = targetWidth * 0.5f;
        return _markerX >= _targetX - halfWidth && _markerX <= _targetX + halfWidth;
    }

    private void FinishRound(bool hit)
    {
        if (hit)
            _hits++;

        _resultText.text = hit ? "Great!" : "Missed!";

        if (_currentRound >= roundsToPlay)
        {
            FinishGame();
            return;
        }

        StartRound();
    }

    private void FinishGame()
    {
        float boost = _hits * growthBoostPerHit;

        if (_hits == 0)
            boost = growthBoostOnMiss;

        if (_targetPlot != null)
            _targetPlot.AddGrowthTime(boost);

        Debug.Log($"Planting mini game finished. Hits: {_hits}/{roundsToPlay}. Growth boost: {boost:0.0}s.");

        _targetPlot = null;
        _isPlaying = false;
        Hide();
    }

    private void CreateUi()
    {
        _canvas = new GameObject("Planting Mini Game Canvas").AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;
        CanvasScaler scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvas.gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(_canvas.gameObject);

        _root = new GameObject("QTE Root", typeof(RectTransform));
        _root.transform.SetParent(_canvas.transform, false);
        RectTransform rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(860f, 170f);
        rootRect.anchoredPosition = new Vector2(0f, -180f);

        _titleText = CreateText("Title", _root.transform, 24, TextAnchor.MiddleCenter);
        SetRect(_titleText.rectTransform, new Vector2(0f, 62f), new Vector2(820f, 30f));

        _roundText = CreateText("Round", _root.transform, 18, TextAnchor.MiddleCenter);
        SetRect(_roundText.rectTransform, new Vector2(0f, 34f), new Vector2(820f, 26f));

        GameObject trackBackplate = CreatePanel("Track Backplate", _root.transform, new Color(0.02f, 0.025f, 0.02f, 0.92f));
        SetRect(trackBackplate.GetComponent<RectTransform>(), new Vector2(0f, -10f), new Vector2(800f, 70f));

        GameObject track = CreatePanel("Track", trackBackplate.transform, new Color(0.18f, 0.19f, 0.17f, 1f));
        SetRect(track.GetComponent<RectTransform>(), Vector2.zero, new Vector2(TrackWidth, 28f));

        GameObject target = CreatePanel("Target Zone", track.transform, new Color(0.34f, 0.82f, 0.36f, 0.95f));
        _targetZone = target.GetComponent<RectTransform>();
        SetRect(_targetZone, Vector2.zero, new Vector2(targetWidth, 28f));

        GameObject marker = CreatePanel("Marker", track.transform, new Color(1f, 0.86f, 0.28f, 1f));
        _marker = marker.GetComponent<RectTransform>();
        SetRect(_marker, Vector2.zero, new Vector2(16f, 58f));

        _resultText = CreateText("Result", _root.transform, 16, TextAnchor.MiddleCenter);
        SetRect(_resultText.rectTransform, new Vector2(0f, -64f), new Vector2(820f, 26f));
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        return text;
    }

    private void SetRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private void Show()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
