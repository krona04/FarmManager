using UnityEngine;

// Attach this to the kiosk GameObject in the scene.
// PlayerInteraction detects it via raycast and opens ShopMenu on [E].
public class ShopInteractable : MonoBehaviour
{
    public string shopName = "Shop";
}
