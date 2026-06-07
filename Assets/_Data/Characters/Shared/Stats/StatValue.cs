using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatValue
{
    [SerializeField] private float baseValue;
    [SerializeField] private bool isDirty = true;
    [SerializeField] private float cachedFinalValue;

    private readonly List<StatModifier> skillTreeModifiers = new();
    private readonly List<StatModifier> equipmentModifiers = new();
    private readonly List<StatModifier> buffModifiers = new();

    public event Action<float> OnValueChanged;

    public float BaseValue
    {
        get => baseValue;
        set
        {
            if (Mathf.Approximately(baseValue, value)) return;

            this.baseValue = value;
            this.MarkDirty();
        }
    }
    public float FinalValue
    {
        get
        {
            if (isDirty) this.Recalculate();

            return this.cachedFinalValue;
        }
    }
    public void AddSkillTreeModifier(StatModifier mod)
        => AddModifier(skillTreeModifiers, mod);

    public void AddEquipmentModifier(StatModifier mod)
        => AddModifier(equipmentModifiers, mod);

    public void AddBuffModifier(StatModifier mod)
        => AddModifier(buffModifiers, mod);
    private void AddModifier(List<StatModifier> list, StatModifier mod)
    {
        if (mod.Source != null)
        {
            list.RemoveAll(m =>
                m.Source == mod.Source &&
                m.StatType == mod.StatType &&
                m.ModifierType == mod.ModifierType);
        }

        list.Add(mod);

        this.MarkDirty();
    }
    public void RemoveModifierFromSource(UnityEngine.Object source)
    {
        RemoveModifierFromSource(skillTreeModifiers, source);
        RemoveModifierFromSource(equipmentModifiers, source);
        RemoveModifierFromSource(buffModifiers, source);

        this.MarkDirty();
    }
    private void RemoveModifierFromSource(
        List<StatModifier> list,
        UnityEngine.Object source)
    {
        list.RemoveAll(m => m.Source == source);
    }
    public void ClearAllModifiers()
    {
        skillTreeModifiers.Clear();
        equipmentModifiers.Clear();
        buffModifiers.Clear();

        this.MarkDirty();
    }
    public void ClearSkillTreeModifiers()
    {
        skillTreeModifiers.Clear();
        this.MarkDirty();
    }
    public void ClearEquipmentModifiers()
    {
        equipmentModifiers.Clear();
        this.MarkDirty();
    }
    public void ClearBuffModifiers()
    {
        buffModifiers.Clear();
        this.MarkDirty();
    }
    public void AddFlatModifier(
        StatType statType,
        float amount,
        UnityEngine.Object source = null,
        float duration = -1f,
        int stack = 1)
    {
        AddBuffModifier(
            new StatModifier(
                statType,
                ModifierType.Flat,
                amount,
                source,
                duration,
                stack));
    }
    public void AddPercentAddModifier(
        StatType statType,
        float amount,
        UnityEngine.Object source = null,
        float duration = -1f,
        int stack = 1)
    {
        AddBuffModifier(
            new StatModifier(
                statType,
                ModifierType.PercentAdd,
                amount,
                source,
                duration,
                stack));
    }
    public void AddPercentMultiplyModifier(
        StatType statType,
        float amount,
        UnityEngine.Object source = null,
        float duration = -1f,
        int stack = 1)
    {
        AddBuffModifier(
            new StatModifier(
                statType,
                ModifierType.PercentMultiply,
                amount,
                source,
                duration,
                stack));
    }
    private void Recalculate()
    {
        this.RemoveExpiredModifiers();

        float flat = 0f;
        float percentAdd = 0f;
        float percentMultiply = 1f;

        AccumulateModifiers(
            skillTreeModifiers,
            ref flat,
            ref percentAdd,
            ref percentMultiply);

        AccumulateModifiers(
            equipmentModifiers,
            ref flat,
            ref percentAdd,
            ref percentMultiply);

        AccumulateModifiers(
            buffModifiers,
            ref flat,
            ref percentAdd,
            ref percentMultiply);

        float finalValue = baseValue;

        finalValue += flat;

        finalValue *= (1f + percentAdd);

        finalValue *= percentMultiply;

        cachedFinalValue = finalValue;

        isDirty = false;

        OnValueChanged?.Invoke(cachedFinalValue);
    }

    private void AccumulateModifiers(List<StatModifier> modifiers,
        ref float flat, ref float percentAdd, ref float percentMultiply)
    {
        foreach (var mod in modifiers)
        {
            if (!mod.IsEnabled) continue;

            float amount = mod.GetEffectiveValue();

            switch (mod.ModifierType)
            {
                case ModifierType.Flat:
                    flat += amount;
                    break;

                case ModifierType.PercentAdd:
                    percentAdd += amount;
                    break;

                case ModifierType.PercentMultiply:
                    percentMultiply *= (1f + amount);
                    break;
            }
        }
    }

    private void RemoveExpiredModifiers()
    {
        skillTreeModifiers.RemoveAll(m => m.HasExpired());
        equipmentModifiers.RemoveAll(m => m.HasExpired());
        buffModifiers.RemoveAll(m => m.HasExpired());
    }

    public void NotifyValueChanged()
    {
        OnValueChanged?.Invoke(FinalValue);
    }
    private void MarkDirty()
    {
        isDirty = true;
    }

    public void SetBaseValue(float value)
    {
        BaseValue = value;
    }
}
