using UnityEngine;

/// <summary>
/// Legacy script — replaced by TalentScreen.cs ([T] key).
/// Hides the old Unity-UI talent panel on Start so it doesn't conflict.
/// You can safely remove this component and delete the old panel GameObject.
/// </summary>
public class MenuToggle : MonoBehaviour
{
    [Tooltip("Old Unity-UI talent panel — will be hidden automatically.")]
    public GameObject      talentMenuPanel;
    public MonoBehaviour   playerMovementScript; // kept for inspector compatibility

    private void Start()
    {
        // Hide old panel — TalentScreen.cs handles this functionality now
        if (talentMenuPanel != null)
            talentMenuPanel.SetActive(false);
    }
}
