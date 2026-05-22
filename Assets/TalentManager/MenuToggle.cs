using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggle : MonoBehaviour
{
    public GameObject talentMenuPanel;
    public MonoBehaviour playerMovementScript;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            bool isMenuOpen = !talentMenuPanel.activeSelf;
            talentMenuPanel.SetActive(isMenuOpen);

            if (isMenuOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (playerMovementScript != null)
                {
                    playerMovementScript.enabled = false;
                }
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                if (playerMovementScript != null)
                {
                    playerMovementScript.enabled = true;
                }
            }
        }
    }
}