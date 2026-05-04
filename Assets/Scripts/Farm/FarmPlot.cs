using UnityEngine;

public enum PlotState { Empty, Planted, Growing, ReadyToHarvest }

public class FarmPlot : MonoBehaviour
{
    [Header("Plot Info")]
    public PlotState currentState = PlotState.Empty;
    public float growTime = 10f; 

    private float _growTimer = 0f;
    private Renderer _renderer;

    
    private static readonly Color ColorEmpty = new Color(0.55f, 0.37f, 0.22f); 
    private static readonly Color ColorPlanted = new Color(0.40f, 0.28f, 0.15f); 
    private static readonly Color ColorGrowing = new Color(0.50f, 0.75f, 0.25f); 
    private static readonly Color ColorReady = new Color(0.10f, 0.60f, 0.10f); 

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    void Update()
    {
        if (currentState == PlotState.Growing)
        {
            _growTimer += Time.deltaTime;

            if (_growTimer >= growTime)
            {
                currentState = PlotState.ReadyToHarvest;
                _growTimer = 0f;
                UpdateVisual();
                Debug.Log($"{gameObject.name}: The harvest is ready!");
            }
        }
    }

    public void Interact()
    {
        switch (currentState)
        {
            case PlotState.Empty:
                Plant();
                break;
            case PlotState.ReadyToHarvest:
                Harvest();
                break;
            case PlotState.Growing:
                Debug.Log("It's still growing... Wait!");
                break;
            case PlotState.Planted:
                Debug.Log("It has already been planted.");
                break;
        }
    }

    void Plant()
    {
        currentState = PlotState.Growing;
        _growTimer = 0f;
        Debug.Log($"{gameObject.name}: planted! Growing for {growTime} sec.");
        UpdateVisual();
    }

    void Harvest()
    {
        currentState = PlotState.Empty;
        Debug.Log($"{gameObject.name}: The harvest is in!");
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (_renderer == null) return;

        _renderer.material.color = currentState switch
        {
            PlotState.Empty => ColorEmpty,
            PlotState.Planted => ColorPlanted,
            PlotState.Growing => ColorGrowing,
            PlotState.ReadyToHarvest => ColorReady,
            _ => ColorEmpty
        };
    }
}