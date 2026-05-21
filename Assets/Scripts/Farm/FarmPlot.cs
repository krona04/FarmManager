using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlotState
{
    Empty,
    Growing,
    ReadyToHarvest
}

public class FarmPlot : MonoBehaviour
{
    private const float QteTrackWidth = 700f;
    private const float QteMinX = -QteTrackWidth * 0.5f;
    private const float QteMaxX = QteTrackWidth * 0.5f;

    public static bool IsPlantingQteActive { get; private set; }

    [Header("Plot Info")]
    public PlotState currentState = PlotState.Empty;
    public CropType plantedCrop = CropType.None;

    [Header("Visual")]
    [SerializeField] private Renderer soilRenderer;

    [Tooltip("Parent object that contains PlantPoint_1, PlantPoint_2, etc.")]
    [SerializeField] private Transform plantPointsParent;

    private Transform[] _plantPoints;
    private readonly List<GameObject> _currentPlantObjects = new();

    private float _growTimer = 0f;
    private CropData _currentCropData;

    private bool _qteActive;
    private int _qteRound;
    private int _qteHits;
    private float _qteRoundTimer;
    private float _qteMarkerX;
    private float _qteTargetX;
    private int _qteDirection = 1;

    [Header("Planting QTE")]
    [SerializeField] private int qteRounds = 3;
    [SerializeField] private float qteRoundDuration = 1.6f;
    [SerializeField] private float qteMarkerSpeed = 520f;
    [SerializeField] private float qteTargetWidth = 82f;
    [SerializeField] private float qteGrowthBoostPerHit = 4f;
    [SerializeField] private float qteGrowthBoostOnMiss = 1f;

    private static readonly Color ColorEmpty = new Color(0.55f, 0.37f, 0.22f);

    private void Start()
    {
        if (soilRenderer == null)
            soilRenderer = GetComponentInChildren<Renderer>();

        CachePlantPoints();

        if (soilRenderer == null)
            Debug.LogError($"{gameObject.name}: Soil Renderer is not assigned!");

        if (_plantPoints == null || _plantPoints.Length == 0)
            Debug.LogError($"{gameObject.name}: Plant Points are not assigned!");

        UpdateVisual();
    }

    private void OnDisable()
    {
        if (_qteActive)
            IsPlantingQteActive = false;
    }

    private void Update()
    {
        if (currentState != PlotState.Growing)
            return;

        if (_currentCropData == null)
            return;

        _growTimer += Time.deltaTime;
        UpdatePlantingQte();

        UpdatePlantGrowth();

        if (_growTimer >= _currentCropData.growTime)
        {
            currentState = PlotState.ReadyToHarvest;
            _growTimer = _currentCropData.growTime;

            UpdatePlantGrowth();
            UpdateVisual();

            Debug.Log($"{gameObject.name}: {plantedCrop} is ready!");
        }
    }

    private void OnGUI()
    {
        if (!_qteActive)
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
        GUI.Label(new Rect(panelX, panelY + 34f, panelWidth, 22f), $"{_qteRound}/{qteRounds}  Hits: {_qteHits}  Press Space", smallStyle);

        float trackWidth = panelWidth - 90f;
        float trackHeight = 24f;
        float trackX = panelX + 45f;
        float trackY = panelY + 68f;

        DrawRect(new Rect(trackX, trackY, trackWidth, trackHeight), new Color(0.18f, 0.19f, 0.17f, 1f));

        float unit = trackWidth / (QteMaxX - QteMinX);
        float targetScreenWidth = qteTargetWidth * unit;
        float targetScreenX = trackX + (_qteTargetX - QteMinX) * unit - targetScreenWidth * 0.5f;
        DrawRect(new Rect(targetScreenX, trackY, targetScreenWidth, trackHeight), new Color(0.34f, 0.82f, 0.36f, 1f));

        float markerScreenX = trackX + (_qteMarkerX - QteMinX) * unit - 5f;
        DrawRect(new Rect(markerScreenX, trackY - 13f, 10f, trackHeight + 26f), new Color(1f, 0.86f, 0.28f, 1f));
    }

    private void CachePlantPoints()
    {
        if (plantPointsParent == null)
            return;

        _plantPoints = new Transform[plantPointsParent.childCount];

        for (int i = 0; i < plantPointsParent.childCount; i++)
        {
            _plantPoints[i] = plantPointsParent.GetChild(i);
        }
    }

    public void Interact()
    {
        switch (currentState)
        {
            case PlotState.Empty:
                TryPlant();
                break;

            case PlotState.Growing:
                Debug.Log($"{plantedCrop} is still growing.");
                break;

            case PlotState.ReadyToHarvest:
                Harvest();
                break;
        }
    }

    private void TryPlant()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        CropType selectedCrop = GameManager.Instance.selectedCrop;
        CropData cropData = GameManager.Instance.GetCropData(selectedCrop);

        if (cropData == null)
        {
            Debug.LogError($"Crop data for {selectedCrop} not found!");
            return;
        }

        bool hasSeed = GameManager.Instance.TryUseSeed(selectedCrop);

        if (!hasSeed)
        {
            Debug.Log($"No seeds for {selectedCrop}!");
            return;
        }

        plantedCrop = selectedCrop;
        _currentCropData = cropData;

        currentState = PlotState.Growing;
        _growTimer = 0f;

        SpawnPlants();
        UpdatePlantGrowth();
        UpdateVisual();

        BeginPlantingQte();

        Debug.Log($"{gameObject.name}: planted {plantedCrop}.");
    }

    private void BeginPlantingQte()
    {
        _qteActive = true;
        IsPlantingQteActive = true;
        _qteRound = 0;
        _qteHits = 0;

        StartQteRound();

        Debug.Log($"{gameObject.name}: planting QTE started.");
    }

    private void StartQteRound()
    {
        _qteRound++;
        _qteRoundTimer = qteRoundDuration;
        _qteMarkerX = QteMinX;
        _qteDirection = 1;
        _qteTargetX = Random.Range(QteMinX + qteTargetWidth * 0.5f, QteMaxX - qteTargetWidth * 0.5f);
    }

    private void UpdatePlantingQte()
    {
        if (!_qteActive)
            return;

        _qteRoundTimer -= Time.deltaTime;
        _qteMarkerX += qteMarkerSpeed * _qteDirection * Time.deltaTime;

        if (_qteMarkerX >= QteMaxX)
        {
            _qteMarkerX = QteMaxX;
            _qteDirection = -1;
        }
        else if (_qteMarkerX <= QteMinX)
        {
            _qteMarkerX = QteMinX;
            _qteDirection = 1;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            FinishQteRound(IsQteHit());
        }
        else if (_qteRoundTimer <= 0f)
        {
            FinishQteRound(false);
        }
    }

    private bool IsQteHit()
    {
        float halfWidth = qteTargetWidth * 0.5f;
        return _qteMarkerX >= _qteTargetX - halfWidth && _qteMarkerX <= _qteTargetX + halfWidth;
    }

    private void FinishQteRound(bool hit)
    {
        if (hit)
            _qteHits++;

        if (_qteRound >= qteRounds)
        {
            FinishPlantingQte();
            return;
        }

        StartQteRound();
    }

    private void FinishPlantingQte()
    {
        _qteActive = false;
        IsPlantingQteActive = false;

        float boost = _qteHits > 0
            ? _qteHits * qteGrowthBoostPerHit
            : qteGrowthBoostOnMiss;

        AddGrowthTime(boost);

        Debug.Log($"{gameObject.name}: planting QTE finished. Hits: {_qteHits}/{qteRounds}. Growth boost: {boost:0.0}s.");
    }

    private void Harvest()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        if (_currentCropData == null)
        {
            Debug.LogError($"{gameObject.name}: Current crop data missing!");
            return;
        }

        GameManager.Instance.AddHarvest(plantedCrop, _currentCropData.harvestAmount);

        RemovePlants();

        plantedCrop = CropType.None;
        _currentCropData = null;

        currentState = PlotState.Empty;
        _growTimer = 0f;
        _qteActive = false;
        IsPlantingQteActive = false;

        UpdateVisual();

        Debug.Log($"{gameObject.name}: harvested.");
    }

    private void SpawnPlants()
    {
        RemovePlants();

        if (_currentCropData == null)
            return;

        if (_currentCropData.plantPrefab == null)
        {
            Debug.LogWarning($"{_currentCropData.cropName}: plant prefab is missing!");
            return;
        }

        if (_plantPoints == null || _plantPoints.Length == 0)
        {
            Debug.LogError($"{gameObject.name}: Plant Points are missing!");
            return;
        }

        foreach (Transform point in _plantPoints)
        {
            if (point == null)
                continue;

            GameObject plant = Instantiate(
                _currentCropData.plantPrefab,
                point.position,
                point.rotation,
                point
            );

            plant.transform.localPosition = Vector3.zero;
            plant.transform.localRotation = Quaternion.identity;
            plant.transform.localScale = _currentCropData.startScale;

            _currentPlantObjects.Add(plant);
        }
    }

    private void UpdatePlantGrowth()
    {
        if (_currentCropData == null)
            return;

        float progress = GetGrowthProgress();

        Vector3 newScale = Vector3.Lerp(
            _currentCropData.startScale,
            _currentCropData.fullScale,
            progress
        );

        foreach (GameObject plant in _currentPlantObjects)
        {
            if (plant == null)
                continue;

            plant.transform.localScale = newScale;
        }
    }

    private void RemovePlants()
    {
        foreach (GameObject plant in _currentPlantObjects)
        {
            if (plant != null)
                Destroy(plant);
        }

        _currentPlantObjects.Clear();
    }

    private void UpdateVisual()
    {
        if (soilRenderer == null)
            return;

        if (currentState == PlotState.Empty)
        {
            soilRenderer.material.color = ColorEmpty;
            return;
        }

        if (_currentCropData == null)
        {
            soilRenderer.material.color = ColorEmpty;
            return;
        }

        if (currentState == PlotState.Growing)
        {
            soilRenderer.material.color = _currentCropData.growingColor;
        }
        else if (currentState == PlotState.ReadyToHarvest)
        {
            soilRenderer.material.color = _currentCropData.readyColor;
        }
    }

    public float GetGrowthProgress()
    {
        if (currentState != PlotState.Growing && currentState != PlotState.ReadyToHarvest)
            return 0f;

        if (_currentCropData == null)
            return 0f;

        return Mathf.Clamp01(_growTimer / _currentCropData.growTime);
    }

    public void AddGrowthTime(float seconds)
    {
        if (currentState != PlotState.Growing)
            return;

        if (_currentCropData == null)
            return;

        _growTimer = Mathf.Clamp(_growTimer + seconds, 0f, _currentCropData.growTime);
        UpdatePlantGrowth();

        if (_growTimer >= _currentCropData.growTime)
        {
            currentState = PlotState.ReadyToHarvest;
            UpdateVisual();

            Debug.Log($"{gameObject.name}: {plantedCrop} is ready!");
        }
    }

    public string GetPlantedCropName()
    {
        if (_currentCropData == null)
            return "";

        return _currentCropData.cropName;
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
