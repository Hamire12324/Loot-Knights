using System.Collections.Generic;

public static class EquipmentStatCalculator
{
    public static List<StatModifier> BuildModifiers(IReadOnlyList<EquipmentSlotData> slots)
    {
        List<StatModifier> modifiers = new();

        if (slots == null)
            return modifiers;

        foreach (EquipmentSlotData slot in slots)
        {
            ItemDefinition item = slot?.Item;
            EquipmentInstanceData instance = slot?.EquipmentInstance;

            if (item != null && instance != null && instance.IsValid)
                modifiers.AddRange(instance.BuildModifiers(item));
        }

        foreach (EquipmentSetProgress progress in BuildSetProgresses(slots))
            progress.Set.AddActiveBonuses(progress.EquippedPieceCount, modifiers);

        return modifiers;
    }

    public static List<EquipmentSetProgress> BuildSetProgresses(IReadOnlyList<EquipmentSlotData> slots)
    {
        // A set threshold is earned only by pieces of the same rarity. This keeps
        // a mixed loadout (for example 4 Uncommon + 2 Rare) at its two separate
        // thresholds instead of incorrectly treating it as one 6-piece set.
        Dictionary<EquipmentSetDefinition, Dictionary<ItemRarity, int>> pieceCounts = new();

        if (slots != null)
        {
            foreach (EquipmentSlotData slot in slots)
            {
                EquipmentSetDefinition set = slot?.Item != null ? slot.Item.EquipmentSet : null;
                if (set == null || !set.IsValid) continue;

                if (!pieceCounts.TryGetValue(set, out Dictionary<ItemRarity, int> rarityCounts))
                {
                    rarityCounts = new Dictionary<ItemRarity, int>();
                    pieceCounts.Add(set, rarityCounts);
                }

                rarityCounts.TryGetValue(slot.Item.Rarity, out int count);
                rarityCounts[slot.Item.Rarity] = count + 1;
            }
        }

        List<EquipmentSetProgress> progresses = new(pieceCounts.Count);
        foreach (KeyValuePair<EquipmentSetDefinition, Dictionary<ItemRarity, int>> pair in pieceCounts)
            foreach (KeyValuePair<ItemRarity, int> rarityCount in pair.Value)
                progresses.Add(new EquipmentSetProgress(pair.Key, rarityCount.Key, rarityCount.Value));

        return progresses;
    }
}
