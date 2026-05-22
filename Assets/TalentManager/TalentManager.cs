using UnityEngine;
using System.Collections.Generic;

public enum TalentType
{
    MovementSpeed,
    SellPrice,
    GrowthSpeed,
    DoubleYieldChance
}

public class TalentManager : MonoBehaviour
{
    public static TalentManager Instance;
    private Dictionary<TalentType, int> talentLevels = new Dictionary<TalentType, int>();
    private Dictionary<TalentType, int> maxLevels = new Dictionary<TalentType, int>()
    {
        { TalentType.MovementSpeed, 5 },
        { TalentType.SellPrice, 10 },
        { TalentType.GrowthSpeed, 3 },
        { TalentType.DoubleYieldChance, 2 }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetCurrentLevel(TalentType type)
    {
        return talentLevels.ContainsKey(type) ? talentLevels[type] : 0;
    }

    public int GetMaxLevel(TalentType type)
    {
        return maxLevels[type];
    }

    public int GetUpgradeCost(TalentType type)
    {
        int level = GetCurrentLevel(type);
        return (level + 1) * 100;
    }

    public bool IsMaxLevel(TalentType type)
    {
        return GetCurrentLevel(type) >= maxLevels[type];
    }

    public void UpgradeTalent(TalentType type)
    {
        if (IsMaxLevel(type)) return;
        if (talentLevels.ContainsKey(type)) talentLevels[type]++;
        else talentLevels[type] = 1;
    }

    /// <summary>Directly set a talent level — used by the save/load system.</summary>
    public void SetLevel(TalentType type, int level)
    {
        int clamped = Mathf.Clamp(level, 0, maxLevels[type]);
        if (clamped == 0) talentLevels.Remove(type);
        else              talentLevels[type] = clamped;
    }

    public float GetTalentModifier(TalentType type)
    {
        int level = GetCurrentLevel(type);
        switch (type)
        {
            case TalentType.MovementSpeed: return 1f + (level * 0.05f);
            case TalentType.SellPrice: return level * 5f;
            case TalentType.GrowthSpeed: return 1f - (level * 0.05f);
            case TalentType.DoubleYieldChance: return level * 5f;
            default: return 1f;
        }
    }
}