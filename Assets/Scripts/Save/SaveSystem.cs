using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // ── Slot management ───────────────────────────────────────────────────────
    public static int CurrentSlot { get; set; } = 0;   // 0, 1 or 2
    public const  int SlotCount   = 3;

    private static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    // ── Save ──────────────────────────────────────────────────────────────────
    public static void Save(SaveData data, int slot = -1)
    {
        int s = slot < 0 ? CurrentSlot : slot;
        data.saveDate = DateTime.Now.ToString("dd.MM.yyyy  HH:mm");
        try
        {
            File.WriteAllText(SlotPath(s), JsonUtility.ToJson(data, true));
            Debug.Log($"[SaveSystem] Saved → slot {s}  ({SlotPath(s)})");
        }
        catch (Exception e) { Debug.LogError($"[SaveSystem] Save failed: {e.Message}"); }
    }

    // ── Load ──────────────────────────────────────────────────────────────────
    public static SaveData Load(int slot = -1)
    {
        int s = slot < 0 ? CurrentSlot : slot;
        if (!File.Exists(SlotPath(s))) return null;
        try   { return JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(s))); }
        catch (Exception e) { Debug.LogError($"[SaveSystem] Load failed: {e.Message}"); return null; }
    }

    // ── Metadata (for menu display without loading full data) ─────────────────
    public static SaveSlotMeta GetSlotMeta(int slot)
    {
        var meta = new SaveSlotMeta();
        if (!File.Exists(SlotPath(slot))) return meta;  // hasData = false

        SaveData data = Load(slot);
        if (data == null) return meta;

        meta.hasData  = true;
        meta.level    = data.playerLevel?.level ?? 1;
        meta.money    = data.money;
        meta.saveDate = data.saveDate ?? "—";
        return meta;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static bool HasSave(int slot) => File.Exists(SlotPath(slot));

    public static void DeleteSlot(int slot)
    {
        if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot));
        Debug.Log($"[SaveSystem] Slot {slot} deleted.");
    }
}
