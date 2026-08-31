using UnityEngine;

public static class EconomyPricing
{
    public const int CoinsPerDiamondExchange = 250;

    public static int GetEquipmentUpgradeCost(ItemDefinition item, int currentLevel)
    {
        int rarityMultiplier = item == null ? 1 : (int)item.Rarity + 1;
        int levelMultiplier = Mathf.Max(0, currentLevel) + 1;
        return 50 * rarityMultiplier * levelMultiplier;
    }

    public static int GetSellValue(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return 0;

        int rarityMultiplier = (int)item.Rarity + 1;
        int categoryMultiplier = item.Category == ItemCategory.Equipment ? 3 : 1;
        return 20 * rarityMultiplier * categoryMultiplier * amount;
    }

    public static int GetInventoryExpansionCoinCost(int purchasedExpansionCount)
    {
        return 400 + Mathf.Max(0, purchasedExpansionCount) * 250;
    }

    public static int GetInventoryExpansionDiamondCost(int purchasedExpansionCount)
    {
        return 5 + Mathf.Max(0, purchasedExpansionCount) * 2;
    }
}
