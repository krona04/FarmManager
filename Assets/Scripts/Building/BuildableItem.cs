using UnityEngine;

[System.Serializable]
public class BuildableItem
{
    public string     itemName;
    public GameObject prefab;
    public int        cost;
    // If true, BuildManager registers the placed object as a FarmPlot with GameManager.
    public bool       isFarmPlot;
}
