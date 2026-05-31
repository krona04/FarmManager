using UnityEngine;

public enum ShopType
{
    // Seed shop is now keyboard-only ([I] key) — no ShopType needed for it.
    FarmUpgrades   // Opens FarmUpgradeShop (talent tree, costs upgrade points)
}

// Attach to the kiosk GameObject in the scene.
// PlayerInteraction detects it via raycast and opens the matching shop on [E].
public class ShopInteractable : MonoBehaviour
{
    public string   shopName = "Farm Upgrades";
    public ShopType shopType = ShopType.FarmUpgrades;
}
