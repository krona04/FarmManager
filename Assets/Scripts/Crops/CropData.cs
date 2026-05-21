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

    [Header("Visual")]
    public GameObject plantPrefab;
    public Vector3 startScale = new Vector3(0.15f, 0.15f, 0.15f);
    public Vector3 fullScale = new Vector3(1f, 1f, 1f);
}