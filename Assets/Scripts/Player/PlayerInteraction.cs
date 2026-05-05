using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Key interactKey = Key.E;

    [Header("Shop Controls")]
    public Key buySeedKey = Key.B;
    public Key sellHarvestKey = Key.P;
    public Key switchCropKey = Key.Q;

    [Header("UI")]
    public Text hintText;

    private Camera _camera;

    private void Start()
    {
        _camera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        CheckForInteractable();
        TryInteract();
        TryShopActions();
    }

    private void CheckForInteractable()
    {
        if (hintText == null)
            return;

        FarmPlot plot = GetLookedAtPlot();

        if (plot == null)
        {
            hintText.text = "[Q] Switch crop | [B] Buy selected seed | [P] Sell harvest";
            return;
        }

        hintText.text = plot.currentState switch
        {
            PlotState.Empty => $"[E] Plant {GameManager.Instance.selectedCrop}",
            PlotState.Growing => $"{plot.GetPlantedCropName()} growing... {Mathf.RoundToInt(plot.GetGrowthProgress() * 100f)}%",
            PlotState.ReadyToHarvest => $"[E] Harvest {plot.GetPlantedCropName()}",
            _ => ""
        };
    }

    private void TryInteract()
    {
        var keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard[interactKey].wasPressedThisFrame)
        {
            FarmPlot plot = GetLookedAtPlot();
            plot?.Interact();
        }
    }

    private void TryShopActions()
    {
        var keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (GameManager.Instance == null)
            return;

        if (keyboard[buySeedKey].wasPressedThisFrame)
        {
            GameManager.Instance.BuySelectedSeed();
        }

        if (keyboard[sellHarvestKey].wasPressedThisFrame)
        {
            GameManager.Instance.SellAllHarvest();
        }

        if (keyboard[switchCropKey].wasPressedThisFrame)
        {
            GameManager.Instance.SelectNextCrop();
        }
    }

    private FarmPlot GetLookedAtPlot()
    {
        if (_camera == null)
            return null;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.GetComponentInParent<FarmPlot>();
        }

        return null;
    }
}