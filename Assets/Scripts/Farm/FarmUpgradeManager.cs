using UnityEngine;

/// <summary>
/// Tracks structural farm upgrades bought at the kiosk (paid with money).
/// Provides bonus accessors used by FarmPlot, GameManager, and BuildManager.
/// </summary>
public class FarmUpgradeManager : MonoBehaviour
{
    public static FarmUpgradeManager Instance { get; private set; }

    // ── Upgrade levels ─────────────────────────────────────────────────────────
    public int extraPlotsOwned    = 0;   // total plots bought (informational)
    public int freePlotTokens     = 0;   // consumable: use in BuildManager to place free plot
    public int growthBoosterLevel = 0;   // max 5
    public int harvestBoosterLevel= 0;   // max 3
    public int tradeNetworkLevel  = 0;   // max 5

    // ── Max levels ────────────────────────────────────────────────────────────
    public const int MaxExtraPlots     = 5;
    public const int MaxGrowthBooster  = 5;
    public const int MaxHarvestBooster = 3;
    public const int MaxTradeNetwork   = 5;

    // ── Costs per level (index = level being purchased, i.e. going to level+1) ─
    private static readonly int[] ExtraPlotCosts    = { 150, 250, 400, 600, 900 };
    private static readonly int[] GrowthCosts       = { 100, 150, 220, 320, 450 };
    private static readonly int[] HarvestCosts      = { 120, 200, 320 };
    private static readonly int[] TradeCosts        = {  80, 120, 180, 260, 380 };

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public bonus accessors ────────────────────────────────────────────────

    /// <summary>Growth time multiplier (< 1 = faster). Stacks with TalentManager.</summary>
    public float GetGrowthMultiplier() => 1f - growthBoosterLevel * 0.08f;

    /// <summary>Bonus harvest units added to every plot harvest.</summary>
    public int GetHarvestBonus() => harvestBoosterLevel;

    /// <summary>Flat bonus added to sell price per unit.</summary>
    public int GetSellBonus() => tradeNetworkLevel * 5;

    // ── Purchase API ──────────────────────────────────────────────────────────

    public int  GetExtraPlotCost()    => extraPlotsOwned    < MaxExtraPlots     ? ExtraPlotCosts[extraPlotsOwned]    : 0;
    public int  GetGrowthCost()       => growthBoosterLevel < MaxGrowthBooster  ? GrowthCosts[growthBoosterLevel]    : 0;
    public int  GetHarvestCost()      => harvestBoosterLevel< MaxHarvestBooster ? HarvestCosts[harvestBoosterLevel]  : 0;
    public int  GetTradeCost()        => tradeNetworkLevel  < MaxTradeNetwork   ? TradeCosts[tradeNetworkLevel]      : 0;

    public bool BuyExtraPlot()
    {
        if (extraPlotsOwned >= MaxExtraPlots) return false;
        int cost = GetExtraPlotCost();
        if (GameManager.Instance == null || GameManager.Instance.money < cost) return false;
        GameManager.Instance.money -= cost;
        GameManager.Instance.UpdateUI();
        extraPlotsOwned++;
        freePlotTokens++;
        return true;
    }

    public bool BuyGrowthBooster()
    {
        if (growthBoosterLevel >= MaxGrowthBooster) return false;
        int cost = GetGrowthCost();
        if (GameManager.Instance == null || GameManager.Instance.money < cost) return false;
        GameManager.Instance.money -= cost;
        GameManager.Instance.UpdateUI();
        growthBoosterLevel++;
        return true;
    }

    public bool BuyHarvestBooster()
    {
        if (harvestBoosterLevel >= MaxHarvestBooster) return false;
        int cost = GetHarvestCost();
        if (GameManager.Instance == null || GameManager.Instance.money < cost) return false;
        GameManager.Instance.money -= cost;
        GameManager.Instance.UpdateUI();
        harvestBoosterLevel++;
        return true;
    }

    public bool BuyTradeNetwork()
    {
        if (tradeNetworkLevel >= MaxTradeNetwork) return false;
        int cost = GetTradeCost();
        if (GameManager.Instance == null || GameManager.Instance.money < cost) return false;
        GameManager.Instance.money -= cost;
        GameManager.Instance.UpdateUI();
        tradeNetworkLevel++;
        return true;
    }

    /// <summary>Called by BuildManager when placing a free plot via token.</summary>
    public bool UseFreePlotToken()
    {
        if (freePlotTokens <= 0) return false;
        freePlotTokens--;
        return true;
    }

    // ── Save / Load helpers ───────────────────────────────────────────────────

    public FarmUpgradeSaveData GetSaveData() => new FarmUpgradeSaveData
    {
        extraPlotsOwned     = extraPlotsOwned,
        freePlotTokens      = freePlotTokens,
        growthBoosterLevel  = growthBoosterLevel,
        harvestBoosterLevel = harvestBoosterLevel,
        tradeNetworkLevel   = tradeNetworkLevel
    };

    public void LoadFromSaveData(FarmUpgradeSaveData d)
    {
        if (d == null) return;
        extraPlotsOwned     = d.extraPlotsOwned;
        freePlotTokens      = d.freePlotTokens;
        growthBoosterLevel  = d.growthBoosterLevel;
        harvestBoosterLevel = d.harvestBoosterLevel;
        tradeNetworkLevel   = d.tradeNetworkLevel;
    }
}
