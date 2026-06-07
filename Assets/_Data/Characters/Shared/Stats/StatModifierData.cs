using System;
using UnityEngine;

[Serializable]
public class StatModifierData
{
    [SerializeField] private StatType statType;
    public StatType StatType => statType;
    [SerializeField] private ModifierType modifierType;
    public ModifierType ModifierType => modifierType;

    [SerializeField] private float amount;
    public float Amount => amount;
    public StatModifierData(StatType statType, ModifierType modifierType, float amount)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.amount = amount;
    }
    public StatModifier ToRuntimeModifier()
    {
        return new StatModifier(statType, modifierType, amount);
    }
}