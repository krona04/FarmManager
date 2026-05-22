using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;

    private bool _isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        // Shop and build mode handle ESC themselves
        if (ShopMenu.IsShopOpen || BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _isPaused        = false;
    }

    /// <summary>Wired to the "Save" button in the Pause Menu UI.</summary>
    public void SaveGame()
    {
        GameManager.Instance?.SaveGame();
    }

    public void ExitToMenu()
    {
        GameManager.Instance?.SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        GameManager.Instance?.SaveGame();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ────────────────────────────────────────────────────────────────────────

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        _isPaused        = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
