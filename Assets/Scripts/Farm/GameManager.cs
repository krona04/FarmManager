using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Economy")]
    public int money = 20;

    [Header("Selected Crop")]
    public CropType selectedCrop = CropType.Carrot;

    [Header("Crop Database")]
    public List<CropData> crops = new List<CropData>();

    [Header("Inventory - Seeds")]
    public int carrotSeeds = 5;
    public int potatoSeeds = 0;
    public int wheatSeeds = 0;

    [Header("Inventory - Harvest")]
    public int carrots = 0;
    public int potatoes = 0;
    public int wheat = 0;

    [Header("UI")]
    public Text moneyText;
    public Text selectedCropText;
    public Text seedsText;
    public Text harvestText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public CropData GetCropData(CropType cropType)
    {
        foreach (CropData crop in crops)
        {
            if (crop.cropType == cropType)
                return crop;
        }

        return null;
    }

    public bool TryUseSeed(CropType cropType)
    {
        switch (cropType)
        {
            case CropType.Carrot:
                if (carrotSeeds <= 0) return false;
                carrotSeeds--;
                break;

            case CropType.Potato:
                if (potatoSeeds <= 0) return false;
                potatoSeeds--;
                break;

            case CropType.Wheat:
                if (wheatSeeds <= 0) return false;
                wheatSeeds--;
                break;

            default:
                return false;
        }

        UpdateUI();
        return true;
    }

    public void AddHarvest(CropType cropType, int amount)
    {
        float chance = TalentManager.Instance.GetTalentModifier(TalentType.DoubleYieldChance);
        if (Random.Range(0f, 100f) < chance)
        {
            amount *= 2;
            Debug.Log("Double Yield Perk Activated!");
        }

        switch (cropType)
        {
            case CropType.Carrot: carrots += amount; break;
            case CropType.Potato: potatoes += amount; break;
            case CropType.Wheat: wheat += amount; break;
        }
        UpdateUI();
    }

    public void BuySelectedSeed()
    {
        CropData cropData = GetCropData(selectedCrop);

        if (cropData == null)
        {
            Debug.LogError("Crop data not found!");
            return;
        }

        if (money < cropData.seedPrice)
        {
            Debug.Log("Not enough money!");
            return;
        }

        money -= cropData.seedPrice;

        switch (selectedCrop)
        {
            case CropType.Carrot:
                carrotSeeds++;
                break;

            case CropType.Potato:
                potatoSeeds++;
                break;

            case CropType.Wheat:
                wheatSeeds++;
                break;
        }

        UpdateUI();
    }

    public void SellAllHarvest()
    {
        int totalMoney = 0;
        int flatBonus = Mathf.RoundToInt(TalentManager.Instance.GetTalentModifier(TalentType.SellPrice));

        CropData carrotData = GetCropData(CropType.Carrot);
        CropData potatoData = GetCropData(CropType.Potato);
        CropData wheatData = GetCropData(CropType.Wheat);

        if (carrotData != null)
            totalMoney += carrots * (carrotData.sellPrice + flatBonus);

        if (potatoData != null)
            totalMoney += potatoes * (potatoData.sellPrice + flatBonus);

        if (wheatData != null)
            totalMoney += wheat * (wheatData.sellPrice + flatBonus);

        if (totalMoney <= 0)
        {
            return;
        }

        money += totalMoney;

        carrots = 0;
        potatoes = 0;
        wheat = 0;

        UpdateUI();
    }

    public void SelectNextCrop()
    {
        if (selectedCrop == CropType.Carrot)
            selectedCrop = CropType.Potato;
        else if (selectedCrop == CropType.Potato)
            selectedCrop = CropType.Wheat;
        else
            selectedCrop = CropType.Carrot;

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"Money: {money}$";

        if (selectedCropText != null)
            selectedCropText.text = $"Selected: {selectedCrop}";

        if (seedsText != null)
        {
            seedsText.text =
                $"Seeds:\n" +
                $"Carrot: {carrotSeeds}\n" +
                $"Potato: {potatoSeeds}\n" +
                $"Wheat: {wheatSeeds}";
        }

        if (harvestText != null)
        {
            harvestText.text =
                $"Harvest:\n" +
                $"Carrot: {carrots}\n" +
                $"Potato: {potatoes}\n" +
                $"Wheat: {wheat}";
        }
    }
}