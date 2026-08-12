using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentSetBonus
{
    [SerializeField, Min(1)] private int requiredPieceCount = 2;
    [SerializeField, TextArea] private string description;
    [SerializeField] private List<StatModifierData> modifiers = new();

    public int RequiredPieceCount => Mathf.Max(1, requiredPieceCount);
    public string Description => description;
    public IReadOnlyList<StatModifierData> Modifiers => modifiers;

    public void AddModifiersTo(List<StatModifier> output)
    {
        if (output == null || modifiers == null) return;

        foreach (StatModifierData modifier in modifiers)
        {
            if (modifier == null || modifier.StatType == StatType.None) continue;
            output.Add(modifier.ToRuntimeModifier());
        }
    }

    public void Validate()
    {
        requiredPieceCount = Mathf.Max(1, requiredPieceCount);
        modifiers ??= new List<StatModifierData>();
    }
}
