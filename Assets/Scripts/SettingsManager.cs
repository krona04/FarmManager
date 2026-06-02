using UnityEngine;

public static class SettingsManager
{
    private const string K_Music   = "cfg_musicVol";
    private const string K_Sfx     = "cfg_sfxVol";
    private const string K_Sens    = "cfg_mouseSens";
    private const string K_Full    = "cfg_fullscreen";
    private const string K_Quality = "cfg_quality";
    private const string K_Shuffle = "cfg_shuffle";

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(K_Music, 0.5f);
        set { PlayerPrefs.SetFloat(K_Music, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(K_Sfx, 1f);
        set { PlayerPrefs.SetFloat(K_Sfx, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(K_Sens, 10f);
        set { PlayerPrefs.SetFloat(K_Sens, Mathf.Clamp(value, 0.5f, 30f)); ApplyControls(); }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(K_Full, Screen.fullScreen ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(K_Full, value ? 1 : 0); Screen.fullScreen = value; Save(); }
    }

    public static int QualityIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(K_Quality, QualitySettings.GetQualityLevel()),
                           0, Mathf.Max(0, QualitySettings.names.Length - 1));
        set
        {
            int idx = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            PlayerPrefs.SetInt(K_Quality, idx);
            QualitySettings.SetQualityLevel(idx, true);
            Save();
        }
    }

    public static bool ShufflePlaylist
    {
        get => PlayerPrefs.GetInt(K_Shuffle, 1) == 1;
        set { PlayerPrefs.SetInt(K_Shuffle, value ? 1 : 0); ApplyAudio(); }
    }

    // ── Apply all settings to active systems ──────────────────────────────────

    public static void ApplyAll()
    {
        Screen.fullScreen = Fullscreen;
        QualitySettings.SetQualityLevel(QualityIndex, true);
        ApplyAudio();
        ApplyControls();
    }

    public static void Save() => PlayerPrefs.Save();

    // ── Internal ──────────────────────────────────────────────────────────────

    private static void ApplyAudio()
    {
        AudioListener.volume = SfxVolume;

        var menu = UnityEngine.Object.FindObjectOfType<MenuMusic>();
        if (menu != null) menu.ApplyVolume(MusicVolume);

        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.SetVolume(MusicVolume);
            GameMusicManager.Instance.shuffle = ShufflePlaylist;
        }

        PlayerPrefs.Save();
    }

    private static void ApplyControls()
    {
        var cam = UnityEngine.Object.FindObjectOfType<CameraLook>();
        if (cam != null) cam.mouseSensitivity = MouseSensitivity;
        PlayerPrefs.Save();
    }
}
