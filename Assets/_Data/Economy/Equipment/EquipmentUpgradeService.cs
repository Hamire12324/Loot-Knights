using UnityEngine;

public static class EquipmentUpgradeService
{
    public static int GetCost(ItemDefinition item, int currentLevel)
    {
        return item == null ? 0 : EconomyPricing.GetEquipmentUpgradeCost(item, currentLevel);
    }

    public static bool TryUpgrade(ItemDefinition item, EquipmentInstanceData instance, int requestedLevels)
    {
        if (item == null || instance == null || !instance.IsValid || requestedLevels <= 0)
            return false;

        int currentLevel = instance.UpgradeLevel;
        if (currentLevel >= item.MaxUpgradeLevel)
            return false;

        int levelsToBuy = Mathf.Min(requestedLevels, item.MaxUpgradeLevel - currentLevel);
        int totalCost = 0;
        for (int levelOffset = 0; levelOffset < levelsToBuy; levelOffset++)
            totalCost += GetCost(item, currentLevel + levelOffset);

        if (!PlayerCurrencyManager.Service.TrySpend(CurrencyType.Coins, totalCost))
            return false;

        instance.AddUpgradeLevels(levelsToBuy, item.MaxUpgradeLevel);
        return instance.UpgradeLevel != currentLevel;
    }
}
