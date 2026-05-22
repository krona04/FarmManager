using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[SaveSystem] Saved → {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    public static bool HasSave()   => File.Exists(SavePath);
    public static void DeleteSave() { if (File.Exists(SavePath)) File.Delete(SavePath); }
}
