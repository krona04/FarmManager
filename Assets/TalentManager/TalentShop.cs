using UnityEngine;
using UnityEngine.UI;

public class TalentShop : MonoBehaviour
{
    public Text movementText;
    public Text sellPriceText;
    public Text growthText;
    public Text yieldText;

    private void Start()
    {
        UpdateAllButtonTexts();
    }

    public void BuyTalent(string typeName)
    {
        TalentType type = (TalentType)System.Enum.Parse(typeof(TalentType), typeName);

        if (TalentManager.Instance.IsMaxLevel(type)) return;

        int cost = TalentManager.Instance.GetUpgradeCost(type);

        if (GameManager.Instance != null && GameManager.Instance.money >= cost)
        {
            GameManager.Instance.money -= cost;
            GameManager.Instance.UpdateUI();
            TalentManager.Instance.UpgradeTalent(type);
            UpdateAllButtonTexts();
        }
    }

    public void UpdateAllButtonTexts()
    {
        UpdateText(movementText, TalentType.MovementSpeed, "Speed");
        UpdateText(sellPriceText, TalentType.SellPrice, "Sell Bonus");
        UpdateText(growthText, TalentType.GrowthSpeed, "Growth");
        UpdateText(yieldText, TalentType.DoubleYieldChance, "Double Yield");
    }

    private void UpdateText(Text uiText, TalentType type, string label)
    {
        if (uiText == null) return;
        int cur = TalentManager.Instance.GetCurrentLevel(type);
        int max = TalentManager.Instance.GetMaxLevel(type);
        int cost = TalentManager.Instance.GetUpgradeCost(type);

        if (cur >= max) uiText.text = label + ": MAX";
        else uiText.text = label + " " + cur + "/" + max + " (" + cost + "$)";
    }
}