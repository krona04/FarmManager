using System.Collections.Generic;

// ── Per-slot metadata (shown in main menu) ────────────────────────────────────
[System.Serializable]
public class SaveSlotMeta
{
    public bool   hasData;
    public int    level;
    public int    money;
    public string saveDate; // "dd.MM.yyyy HH:mm"
}

// ── Plot / Building ────────────────────────────────────────────────────────────
[System.Serializable]
public class PlotSaveData
{
    public int   state;
    public int   plantedCrop;
    public float growTimer;
    public float growTimeTarget;
}

[System.Serializable]
public class BuildingSaveData
{
    public int          itemIndex;
    public float        px, py, pz;
    public float        rotY;
    public PlotSaveData plotState = new PlotSaveData();
}

// ── Talent / Level ─────────────────────────────────────────────────────────────
[System.Serializable]
public class TalentSaveData
{
    public int movementSpeed;
    public int sellPrice;
    public int growthSpeed;
    public int doubleYieldChance;
}

[System.Serializable]
public class PlayerLevelSaveData
{
    public int level         = 1;
    public int xp            = 0;
    public int upgradePoints = 0;
}

// ── Farm Upgrades (kiosk) ──────────────────────────────────────────────────────
[System.Serializable]
public class FarmUpgradeSaveData
{
    public int extraPlotsOwned;
    public int freePlotTokens;
    public int growthBoosterLevel;
    public int harvestBoosterLevel;
    public int tradeNetworkLevel;
}

// ── Full save ─────────────────────────────────────────────────────────────────
[System.Serializable]
public class SaveData
{
    public string saveDate;   // filled by SaveSystem.Save()

    public int money;
    public int selectedCrop;

    public int carrotSeeds;
    public int potatoSeeds;
    public int wheatSeeds;

    public int carrots;
    public int potatoes;
    public int wheat;

    public TalentSaveData      talents      = new TalentSaveData();
    public PlayerLevelSaveData playerLevel  = new PlayerLevelSaveData();
    public FarmUpgradeSaveData farmUpgrades = new FarmUpgradeSaveData();

    public bool carPurchased;

    public List<PlotSaveData>     plots           = new List<PlotSaveData>();
    public List<BuildingSaveData> placedBuildings = new List<BuildingSaveData>();

    public void EnsureDefaults()
    {
        if (talents == null) talents = new TalentSaveData();
        if (playerLevel == null) playerLevel = new PlayerLevelSaveData();
        if (farmUpgrades == null) farmUpgrades = new FarmUpgradeSaveData();
        if (plots == null) plots = new List<PlotSaveData>();
        if (placedBuildings == null) placedBuildings = new List<BuildingSaveData>();
    }
}
