using System;

public readonly struct EquipmentSetProgress
{
    public EquipmentSetDefinition Set { get; }
    public int EquippedPieceCount { get; }

    public EquipmentSetProgress(EquipmentSetDefinition set, int equippedPieceCount)
    {
        Set = set;
        EquippedPieceCount = Math.Max(0, equippedPieceCount);
    }
}
