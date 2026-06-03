using System;
using System.IO;
using UnityEngine;

public enum SaveStartupAction
{
    None,
    Load,
    NewGame
}

public struct SaveStartupRequest
{
    public SaveStartupAction action;
    public int slot;

    public bool HasRequest => action != SaveStartupAction.None;
}

public static class SaveSystem
{
    // ── Slot management ───────────────────────────────────────────────────────
    public const int SlotCount = 3;
    private const string CurrentSlotKey = "fm_save_slot";
    private const string StartupActionKey = "fm_startup_action";
    private const string StartupSlotKey = "fm_startup_slot";
    private static int? _sessionCurrentSlot;
    private static SaveData _pendingLoadData;

    // PlayerPrefs instead of a static field: survives scene loads AND editor
    // domain reloads, so the slot selection is always preserved.
    public static int CurrentSlot
    {
        get => ClampSlot(_sessionCurrentSlot ?? UnityEngine.PlayerPrefs.GetInt(CurrentSlotKey, 0));
        set
        {
            int slot = ClampSlot(value);
            _sessionCurrentSlot = slot;
            UnityEngine.PlayerPrefs.SetInt(CurrentSlotKey, slot);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    private static int ClampSlot(int slot) => Mathf.Clamp(slot, 0, SlotCount - 1);

    public static void RequestLoadSlot(int slot)
    {
        int s = ClampSlot(slot);
        CurrentSlot = s;
        _pendingLoadData = Load(s);
        SetStartupRequest(SaveStartupAction.Load, s);
    }

    public static void RequestNewGameSlot(int slot)
    {
        int s = ClampSlot(slot);
        _pendingLoadData = null;
        DeleteSlot(s);
        CurrentSlot = s;
        SetStartupRequest(SaveStartupAction.NewGame, s);
    }

    public static SaveData TakePendingLoadData()
    {
        SaveData data = _pendingLoadData;
        _pendingLoadData = null;
        return data;
    }

    public static SaveStartupRequest ConsumeStartupRequest()
    {
        var request = new SaveStartupRequest
        {
            action = (SaveStartupAction)UnityEngine.PlayerPrefs.GetInt(StartupActionKey, 0),
            slot = ClampSlot(UnityEngine.PlayerPrefs.GetInt(StartupSlotKey, CurrentSlot))
        };

        UnityEngine.PlayerPrefs.DeleteKey(StartupActionKey);
        UnityEngine.PlayerPrefs.DeleteKey(StartupSlotKey);
        UnityEngine.PlayerPrefs.Save();

        if (!System.Enum.IsDefined(typeof(SaveStartupAction), request.action))
            request.action = SaveStartupAction.None;

        if (request.HasRequest)
            CurrentSlot = request.slot;

        return request;
    }

    private static void SetStartupRequest(SaveStartupAction action, int slot)
    {
        UnityEngine.PlayerPrefs.SetInt(StartupActionKey, (int)action);
        UnityEngine.PlayerPrefs.SetInt(StartupSlotKey, ClampSlot(slot));
        UnityEngine.PlayerPrefs.Save();
    }

    private static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    // Legacy path from the single-file save system
    private static string LegacyPath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    // ── Migration ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Call once at game startup (e.g., from MainMenu or GameManager).
    /// Moves old save.json → save_slot0.json so existing saves aren't lost.
    /// </summary>
    public static void MigrateLegacySave()
    {
        if (!File.Exists(LegacyPath)) return;
        if (File.Exists(SlotPath(0))) return; // slot 0 already has data, keep it

        try
        {
            File.Copy(LegacyPath, SlotPath(0));
            File.Delete(LegacyPath);
            Debug.Log("[SaveSystem] Migrated legacy save.json → save_slot0.json");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Migration failed: {e.Message}");
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    public static void Save(SaveData data, int slot = -1)
    {
        if (data == null) return;

        int s = slot < 0 ? CurrentSlot : ClampSlot(slot);
        data.EnsureDefaults();
        data.saveDate = DateTime.Now.ToString("dd.MM.yyyy  HH:mm");
        try
        {
            string path = SlotPath(s);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] Saved → slot {s}");
        }
        catch (Exception e) { Debug.LogError($"[SaveSystem] Save failed: {e.Message}"); }
    }

    // ── Load ──────────────────────────────────────────────────────────────────
    public static SaveData Load(int slot = -1)
    {
        int s = slot < 0 ? CurrentSlot : ClampSlot(slot);
        string path = SlotPath(s);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSystem] No save file for slot {s}: {path}");
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            data?.EnsureDefaults();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load slot {s} failed: {e.Message}");
            return null;
        }
    }

    // ── Metadata for main menu ────────────────────────────────────────────────
    public static SaveSlotMeta GetSlotMeta(int slot)
    {
        var meta = new SaveSlotMeta { hasData = false };
        if (!File.Exists(SlotPath(slot))) return meta;

        SaveData data = Load(slot);
        if (data == null) return meta;

        meta.hasData  = true;
        meta.level    = data.playerLevel?.level ?? 1;
        meta.money    = data.money;
        meta.saveDate = string.IsNullOrEmpty(data.saveDate) ? "—" : data.saveDate;
        return meta;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static bool HasSave(int slot) => File.Exists(SlotPath(ClampSlot(slot)));

    public static void DeleteSlot(int slot)
    {
        int s = ClampSlot(slot);
        string path = SlotPath(s);
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"[SaveSystem] Slot {s} deleted.");
    }
}
