using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // ── Slot management ───────────────────────────────────────────────────────
    public const int SlotCount = 3;

    // PlayerPrefs instead of a static field: survives scene loads AND editor
    // domain reloads, so the slot selection is always preserved.
    public static int CurrentSlot
    {
        get => UnityEngine.PlayerPrefs.GetInt("fm_save_slot", 0);
        set { UnityEngine.PlayerPrefs.SetInt("fm_save_slot", value); UnityEngine.PlayerPrefs.Save(); }
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
        int s = slot < 0 ? CurrentSlot : slot;
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
        int s = slot < 0 ? CurrentSlot : slot;
        string path = SlotPath(s);

        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
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
    public static bool HasSave(int slot) => File.Exists(SlotPath(slot));

    public static void DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"[SaveSystem] Slot {slot} deleted.");
    }
}
