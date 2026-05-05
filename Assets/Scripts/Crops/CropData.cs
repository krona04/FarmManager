using UnityEngine;

[System.Serializable]
public class CropData
{
    public CropType cropType;

    public string cropName;

    public int seedPrice;
    public int sellPrice;
    public int harvestAmount;

    public float growTime;

    public Color growingColor;
    public Color readyColor;
}