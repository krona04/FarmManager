using System.Collections;
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
    public int wheatSeeds  = 0;

    [Header("Inventory - Harvest")]
    public int carrots  = 0;
    public int potatoes = 0;
    public int wheat    = 0;

    [Header("UI")]
    public Text moneyText;
    public Text selectedCropText;
    public Text seedsText;
    public Text harvestText;

    [Header("Farm Plots (pre-placed in scene)")]
    public FarmPlot[] farmPlots;

    [Header("Decor")]
    public GameObject carDecorObject;
    public bool carPurchased = false;
    public const int CarPrice = 1500;

    [Header("Auto Save")]
    public float autoSaveInterval = 120f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private float _autoSaveTimer;
    private float _saveNotifTimer;
    private bool  _allowSaving = true;
    private const float NotifDur = 2.5f;

    // Plots added at runtime by BuildManager
    private readonly List<FarmPlot> _dynamicPlots = new();

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // IEnumerator Start so we yield one frame — ensures all FarmPlot.Start() run before Load
    private IEnumerator Start()
    {
        yield return null;

        _allowSaving = false;
        SaveSystem.MigrateLegacySave();

        SaveStartupRequest startup = SaveSystem.ConsumeStartupRequest();
        SaveData pendingLoadData = startup.action == SaveStartupAction.Load
            ? SaveSystem.TakePendingLoadData()
            : null;
        bool loaded = false;

        if (startup.action == SaveStartupAction.NewGame)
        {
            Debug.Log($"[GameManager] Starting new game in slot {SaveSystem.CurrentSlot}.");
        }
        else
        {
            loaded = LoadGame(pendingLoadData);
        }

        _allowSaving = loaded || startup.action != SaveStartupAction.Load;
        RefreshCarDecor();
        UpdateUI();
        _autoSaveTimer = autoSaveInterval;
    }

    private void Update()
    {
        _autoSaveTimer -= Time.deltaTime;
        if (_autoSaveTimer <= 0f) { SaveGame(); _autoSaveTimer = autoSaveInterval; }

        if (_saveNotifTimer > 0f) _saveNotifTimer -= Time.unscaledDeltaTime;
    }

    private void OnApplicationQuit() => SaveGame();

    // ── Save notification ─────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (PauseMenu.IsPaused || _saveNotifTimer <= 0f) return;
        float a = Mathf.Clamp01(_saveNotifTimer);
        var s = new GUIStyle(GUI.skin.label)
            { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        s.normal.textColor = new Color(.55f, 1f, .55f, a);
        GUI.Label(new Rect(Screen.width - 220f, 14f, 200f, 28f), "Game Saved!", s);
    }

    // ── Dynamic plot registration (called by BuildManager) ───────────────────

    public void RegisterFarmPlot(FarmPlot plot)
    {
        if (plot != null) _dynamicPlots.Add(plot);
    }

    private IEnumerable<FarmPlot> AllPlots()
    {
        if (farmPlots != null)
            foreach (var p in farmPlots) if (p != null) yield return p;
        foreach (var p in _dynamicPlots) if (p != null) yield return p;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public void SaveGame()
    {
        if (!_allowSaving)
        {
            Debug.LogWarning($"[GameManager] Save skipped because slot {SaveSystem.CurrentSlot} was requested for loading but did not load.");
            return;
        }

        var data = new SaveData
        {
            money        = money,
            selectedCrop = (int)selectedCrop,
            carrotSeeds  = carrotSeeds,
            potatoSeeds  = potatoSeeds,
            wheatSeeds   = wheatSeeds,
            carrots      = carrots,
            potatoes     = potatoes,
            wheat        = wheat,
            carPurchased = carPurchased
        };

        // Save talent levels
        if (TalentManager.Instance != null)
        {
            data.talents = new TalentSaveData
            {
                movementSpeed     = TalentManager.Instance.GetCurrentLevel(TalentType.MovementSpeed),
                sellPrice         = TalentManager.Instance.GetCurrentLevel(TalentType.SellPrice),
                growthSpeed       = TalentManager.Instance.GetCurrentLevel(TalentType.GrowthSpeed),
                doubleYieldChance = TalentManager.Instance.GetCurrentLevel(TalentType.DoubleYieldChance)
            };
        }

        // Save player level
        if (PlayerLevel.Instance != null)
        {
            data.playerLevel = new PlayerLevelSaveData
            {
                level         = PlayerLevel.Instance.level,
                xp            = PlayerLevel.Instance.xp,
                upgradePoints = PlayerLevel.Instance.upgradePoints
            };
        }

        // Save farm building upgrades (kiosk)
        if (FarmUpgradeManager.Instance != null)
            data.farmUpgrades = FarmUpgradeManager.Instance.GetSaveData();

        // Save all plot states (pre-placed first, then dynamic — must match LoadGame order)
        foreach (var p in AllPlots()) data.plots.Add(p.GetSaveData());

        // Save dynamically placed buildings
        if (BuildManager.Instance != null)
            data.placedBuildings = BuildManager.Instance.GetSaveData();

        SaveSystem.Save(data);
        _saveNotifTimer = NotifDur;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    public bool LoadGame(SaveData pendingData = null)
    {
        SaveData data = pendingData ?? SaveSystem.Load();
        if (data == null)
        {
            Debug.LogWarning($"[GameManager] No save data found for slot {SaveSystem.CurrentSlot}. Starting a new game.");
            return false;
        }

        data.EnsureDefaults();

        money        = data.money;
        selectedCrop = (CropType)data.selectedCrop;
        carrotSeeds  = data.carrotSeeds;
        potatoSeeds  = data.potatoSeeds;
        wheatSeeds   = data.wheatSeeds;
        carrots      = data.carrots;
        potatoes     = data.potatoes;
        wheat        = data.wheat;
        carPurchased = data.carPurchased;
        RefreshCarDecor();

        // Restore talent levels
        if (TalentManager.Instance != null && data.talents != null)
        {
            TalentManager.Instance.SetLevel(TalentType.MovementSpeed,     data.talents.movementSpeed);
            TalentManager.Instance.SetLevel(TalentType.SellPrice,         data.talents.sellPrice);
            TalentManager.Instance.SetLevel(TalentType.GrowthSpeed,       data.talents.growthSpeed);
            TalentManager.Instance.SetLevel(TalentType.DoubleYieldChance, data.talents.doubleYieldChance);
        }

        // Restore player level
        if (PlayerLevel.Instance != null && data.playerLevel != null)
        {
            PlayerLevel.Instance.level         = Mathf.Max(1, data.playerLevel.level);
            PlayerLevel.Instance.xp            = Mathf.Max(0, data.playerLevel.xp);
            PlayerLevel.Instance.upgradePoints = Mathf.Max(0, data.playerLevel.upgradePoints);
        }

        // Restore farm building upgrades (kiosk)
        FarmUpgradeManager.Instance?.LoadFromSaveData(data.farmUpgrades);

        // Restore pre-placed plot states (by index)
        int staticCount = farmPlots != null ? farmPlots.Length : 0;
        for (int i = 0; i < staticCount && i < data.plots.Count; i++)
            farmPlots[i]?.LoadFromSaveData(data.plots[i]);

        // Restore dynamically placed buildings (BuildManager also restores their plot states)
        BuildManager.Instance?.RestoreBuildings(data.placedBuildings);

        Debug.Log("[GameManager] Game loaded.");
        return true;
    }

    // ── Crop data ─────────────────────────────────────────────────────────────

    public CropData GetCropData(CropType cropType)
    {
        foreach (var c in crops) if (c.cropType == cropType) return c;
        return null;
    }

    // ── Inventory ─────────────────────────────────────────────────────────────

    public int GetSeedCount(CropType cropType) =>
        cropType switch
        {
            CropType.Carrot => carrotSeeds,
            CropType.Potato => potatoSeeds,
            CropType.Wheat  => wheatSeeds,
            _               => 0
        };

    public bool TryUseSeed(CropType cropType)
    {
        switch (cropType)
        {
            case CropType.Carrot: if (carrotSeeds <= 0) return false; carrotSeeds--; break;
            case CropType.Potato: if (potatoSeeds <= 0) return false; potatoSeeds--; break;
            case CropType.Wheat:  if (wheatSeeds  <= 0) return false; wheatSeeds--;  break;
            default: return false;
        }
        UpdateUI(); return true;
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
            case CropType.Carrot: carrots  += amount; break;
            case CropType.Potato: potatoes += amount; break;
            case CropType.Wheat:  wheat    += amount; break;
        }

        // Award XP for harvesting
        PlayerLevel.Instance?.AddXP(XpForHarvest(cropType, amount));

        UpdateUI();
    }

    private static int XpForHarvest(CropType crop, int amount) =>
        amount * (crop switch
        {
            CropType.Carrot => 5,
            CropType.Potato => 8,
            CropType.Wheat  => 6,
            _               => 5
        });

    // ── Shop ──────────────────────────────────────────────────────────────────

    /// <summary>Buy 'amount' seeds of the given crop type (used by ShopMenu).</summary>
    public void BuySeed(CropType cropType, int amount)
    {
        CropData data = GetCropData(cropType);
        if (data == null) return;
        int cost = data.seedPrice * amount;
        if (money < cost) { Debug.Log("Not enough money!"); return; }
        money -= cost;
        switch (cropType)
        {
            case CropType.Carrot: carrotSeeds += amount; break;
            case CropType.Potato: potatoSeeds += amount; break;
            case CropType.Wheat:  wheatSeeds  += amount; break;
        }
        UpdateUI();
    }

    /// <summary>Buy 1 seed of the currently selected crop (old B-key shortcut).</summary>
    public void BuySelectedSeed()
    {
        CropData cropData = GetCropData(selectedCrop);
        if (cropData == null) { Debug.LogError("Crop data not found!"); return; }
        if (money < cropData.seedPrice) { Debug.Log("Not enough money!"); return; }

        money -= cropData.seedPrice;
        switch (selectedCrop)
        {
            case CropType.Carrot: carrotSeeds++; break;
            case CropType.Potato: potatoSeeds++; break;
            case CropType.Wheat:  wheatSeeds++;  break;
        }
        UpdateUI();
    }

    public void SellAllHarvest()
    {
        int totalMoney = 0;
        int flatBonus  = Mathf.RoundToInt(TalentManager.Instance.GetTalentModifier(TalentType.SellPrice))
                       + (FarmUpgradeManager.Instance?.GetSellBonus() ?? 0);

        CropData cd;
        cd = GetCropData(CropType.Carrot); if (cd != null) totalMoney += carrots  * (cd.sellPrice + flatBonus);
        cd = GetCropData(CropType.Potato); if (cd != null) totalMoney += potatoes * (cd.sellPrice + flatBonus);
        cd = GetCropData(CropType.Wheat);  if (cd != null) totalMoney += wheat    * (cd.sellPrice + flatBonus);

        if (totalMoney <= 0) return;

        money   += totalMoney;
        carrots  = 0;
        potatoes = 0;
        wheat    = 0;
        UpdateUI();
    }

    public bool BuyCarDecor()
    {
        if (carPurchased) return false;
        if (money < CarPrice) return false;
        money -= CarPrice;
        carPurchased = true;
        RefreshCarDecor();
        UpdateUI();
        return true;
    }

    public void RefreshCarDecor()
    {
        if (carDecorObject != null)
            carDecorObject.SetActive(carPurchased);
    }

    public void SelectNextCrop()
    {
        if      (selectedCrop == CropType.Carrot) selectedCrop = CropType.Potato;
        else if (selectedCrop == CropType.Potato) selectedCrop = CropType.Wheat;
        else                                       selectedCrop = CropType.Carrot;
        UpdateUI();
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    public void UpdateUI()
    {
        if (moneyText       != null) moneyText.text       = $"Money: {money}$";
        if (selectedCropText != null) selectedCropText.text = $"Selected: {selectedCrop}";
        if (seedsText != null)
            seedsText.text = $"Seeds:\nCarrot: {carrotSeeds}\nPotato: {potatoSeeds}\nWheat: {wheatSeeds}";
        if (harvestText != null)
            harvestText.text = $"Harvest:\nCarrot: {carrots}\nPotato: {potatoes}\nWheat: {wheat}";
    }
}
