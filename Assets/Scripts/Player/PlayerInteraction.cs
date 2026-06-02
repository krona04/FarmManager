using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Key   interactKey      = Key.E;

    [Header("Keyboard Shortcuts")]
    public Key buySeedKey     = Key.B;
    public Key sellHarvestKey = Key.P;
    public Key switchCropKey  = Key.Q;

    [Header("UI")]
    public Text hintText;

    private Camera _camera;

    private void Start()
    {
        _camera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        bool anyMenu = PauseMenu.IsPaused
                       || ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen
                       || BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive;

        if (anyMenu)
        {
            if (hintText != null) hintText.text = string.Empty;
            return;
        }

        CheckHint();
        TryInteract();
        TryShortcuts();
    }

    // ── Hint ──────────────────────────────────────────────────────────────────

    private void CheckHint()
    {
        if (hintText == null) return;

        var shop = Raycast<ShopInteractable>();
        if (shop != null)
        {
            hintText.text = $"[E] {shop.shopName}";
            return;
        }

        var plot = Raycast<FarmPlot>();
        if (plot == null)
        {
            hintText.text = "[Q] Switch crop  |  [B] Buy seed  |  [P] Sell  |  [I] Seed shop";
            return;
        }

        hintText.text = plot.currentState switch
        {
            PlotState.Empty          => $"[E] Plant {GameManager.Instance.selectedCrop}",
            PlotState.Growing        => $"{plot.GetPlantedCropName()} growing... {Mathf.RoundToInt(plot.GetGrowthProgress() * 100f)}%",
            PlotState.ReadyToHarvest => $"[E] Harvest {plot.GetPlantedCropName()}",
            _                        => ""
        };
    }

    // ── E key ─────────────────────────────────────────────────────────────────

    private void TryInteract()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb[interactKey].wasPressedThisFrame) return;

        var shop = Raycast<ShopInteractable>();
        if (shop != null)
        {
            // Route to the correct shop based on type
            switch (shop.shopType)
            {
                case ShopType.FarmUpgrades:
                    FarmUpgradeShop.Instance?.Open();
                    break;
            }
            return;
        }

        Raycast<FarmPlot>()?.Interact();
    }

    // ── Keyboard shortcuts (work outside any menu) ───────────────────────────

    private void TryShortcuts()
    {
        var kb = Keyboard.current;
        if (kb == null || GameManager.Instance == null) return;

        if (kb[buySeedKey].wasPressedThisFrame)     GameManager.Instance.BuySelectedSeed();
        if (kb[sellHarvestKey].wasPressedThisFrame) GameManager.Instance.SellAllHarvest();
        if (kb[switchCropKey].wasPressedThisFrame)  GameManager.Instance.SelectNextCrop();
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    private T Raycast<T>() where T : Component
    {
        if (_camera == null) return null;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            return hit.collider.GetComponentInParent<T>();
        return null;
    }
}
