using System.Collections.Generic;

[System.Serializable]
public class PlotSaveData
{
    public int   state;          // PlotState as int
    public int   plantedCrop;    // CropType as int
    public float growTimer;
    public float growTimeTarget; // _currentGrowTimeTarget (after talent modifier)
}

[System.Serializable]
public class BuildingSaveData
{
    public int   itemIndex;      // index into BuildManager.buildableItems[]
    public float px, py, pz;    // world position
    public float rotY;           // Y rotation in degrees
    // Only populated when BuildableItem.isFarmPlot == true
    public PlotSaveData plotState = new PlotSaveData();
}

[System.Serializable]
public class TalentSaveData
{
    public int movementSpeed;
    public int sellPrice;
    public int growthSpeed;
    public int doubleYieldChance;
}

[System.Serializable]
public class SaveData
{
    public int money;
    public int selectedCrop;     // CropType as int

    public int carrotSeeds;
    public int potatoSeeds;
    public int wheatSeeds;

    public int carrots;
    public int potatoes;
    public int wheat;

    public TalentSaveData talents = new TalentSaveData();

    public List<PlotSaveData>     plots           = new List<PlotSaveData>();
    public List<BuildingSaveData> placedBuildings = new List<BuildingSaveData>();
}
