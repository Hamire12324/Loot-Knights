using System;

public readonly struct EquipmentSetProgress
{
    public EquipmentSetDefinition Set { get; }
    public ItemRarity Rarity { get; }
    public int EquippedPieceCount { get; }

    public EquipmentSetProgress(EquipmentSetDefinition set, ItemRarity rarity, int equippedPieceCount)
    {
        Set = set;
        Rarity = rarity;
        EquippedPieceCount = Math.Max(0, equippedPieceCount);
    }
}
