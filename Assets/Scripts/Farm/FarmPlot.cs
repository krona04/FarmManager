using UnityEngine;

public enum PlotState
{
    Empty,
    Growing,
    ReadyToHarvest
}

public class FarmPlot : MonoBehaviour
{
    [Header("Plot Info")]
    public PlotState currentState = PlotState.Empty;
    public CropType plantedCrop = CropType.None;

    private float _growTimer = 0f;
    private CropData _currentCropData;
    private Renderer _renderer;

    private static readonly Color ColorEmpty = new Color(0.55f, 0.37f, 0.22f);

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    private void Update()
    {
        if (currentState != PlotState.Growing)
            return;

        if (_currentCropData == null)
            return;

        _growTimer += Time.deltaTime;

        if (_growTimer >= _currentCropData.growTime)
        {
            currentState = PlotState.ReadyToHarvest;
            _growTimer = 0f;

            UpdateVisual();

            Debug.Log($"{gameObject.name}: {plantedCrop} is ready!");
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

        UpdateVisual();

        Debug.Log($"{gameObject.name}: planted {plantedCrop}.");
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
            Debug.LogError("Current crop data missing!");
            return;
        }

        GameManager.Instance.AddHarvest(plantedCrop, _currentCropData.harvestAmount);

        Debug.Log($"{gameObject.name}: harvested {plantedCrop}.");

        plantedCrop = CropType.None;
        _currentCropData = null;

        currentState = PlotState.Empty;
        _growTimer = 0f;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_renderer == null)
            return;

        if (currentState == PlotState.Empty)
        {
            _renderer.material.color = ColorEmpty;
            return;
        }

        if (_currentCropData == null)
        {
            _renderer.material.color = ColorEmpty;
            return;
        }

        if (currentState == PlotState.Growing)
        {
            _renderer.material.color = _currentCropData.growingColor;
        }
        else if (currentState == PlotState.ReadyToHarvest)
        {
            _renderer.material.color = _currentCropData.readyColor;
        }
    }

    public float GetGrowthProgress()
    {
        if (currentState != PlotState.Growing)
            return 0f;

        if (_currentCropData == null)
            return 0f;

        return Mathf.Clamp01(_growTimer / _currentCropData.growTime);
    }

    public string GetPlantedCropName()
    {
        if (_currentCropData == null)
            return "";

        return _currentCropData.cropName;
    }
}