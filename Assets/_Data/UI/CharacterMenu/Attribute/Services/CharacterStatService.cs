using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds display stats when a gameplay Hero does not exist yet (MainMenu).
/// It must not be used to recalculate an existing CharacterStat's FinalValue.
/// </summary>
public static class CharacterStatService
{
    public static CharacterAttributeData FromCharacterStat(CharacterStat characterStat)
    {
        if (characterStat == null)
            return default;

        return new CharacterAttributeData(
            characterStat.Attack.FinalValue,
            characterStat.Armor.FinalValue,
            characterStat.CurrentHealth,
            characterStat.MaxHealth.FinalValue,
            characterStat.CritChance.FinalValue,
            characterStat.CritDamage.FinalValue);
    }

    public static CharacterAttributeData FromProfile(
        CharacterClassAttributeData[] classAttributes,
        PlayerEquipmentManager equipmentManager,
        IReadOnlyList<SkillTreeDefinition> skillTrees)
    {
        CreatedCharacterData profile = CharacterProfileStorage.Load();
        CharacterClass characterClass = profile != null ? profile.CharacterClass : CharacterClass.Knight;
        CharacterAttributeData baseStats = GetBaseStats(characterClass, classAttributes);

        return ApplyProfileModifiers(baseStats, equipmentManager, skillTrees);
    }

    private static CharacterAttributeData GetBaseStats(
        CharacterClass characterClass,
        CharacterClassAttributeData[] classAttributes)
    {
        if (classAttributes != null)
        {
            foreach (CharacterClassAttributeData data in classAttributes)
            {
                if (data != null && data.CharacterClass == characterClass)
                    return data.ToAttributeData();
            }
        }

        return characterClass switch
        {
            CharacterClass.Ranger => new CharacterAttributeData(90f, 0f, 90f, 90f, .1f, 1.5f),
            CharacterClass.Mage => new CharacterAttributeData(120f, 0f, 80f, 80f, .05f, 1.7f),
            _ => new CharacterAttributeData(100f, 0f, 100f, 100f, .05f, 1.5f)
        };
    }

    private static CharacterAttributeData ApplyProfileModifiers(
        CharacterAttributeData baseStats,
        PlayerEquipmentManager equipmentManager,
        IReadOnlyList<SkillTreeDefinition> skillTrees)
    {
        StatValueAccumulator attack = new(baseStats.Attack);
        StatValueAccumulator armor = new(baseStats.Armor);
        StatValueAccumulator maxHealth = new(baseStats.MaxHealth);
        StatValueAccumulator critChance = new(baseStats.CritChance);
        StatValueAccumulator critDamage = new(baseStats.CritDamage);

        ApplyModifiers(GetEquipmentModifiers(equipmentManager), ref attack, ref armor, ref maxHealth, ref critChance, ref critDamage);
        ApplyAttributePoints(ref attack, ref armor, ref maxHealth, ref critChance, ref critDamage);

        if (skillTrees != null)
        {
            foreach (SkillTreeDefinition tree in skillTrees)
            {
                if (tree != null)
                    ApplyModifiers(new SkillTreeRuntime(tree).CreateStatModifiers(), ref attack, ref armor, ref maxHealth, ref critChance, ref critDamage);
            }
        }

        float finalMaxHealth = maxHealth.Value;
        return new CharacterAttributeData(
            attack.Value, armor.Value, finalMaxHealth, finalMaxHealth,
            critChance.Value, critDamage.Value);
    }

    private static IEnumerable<StatModifier> GetEquipmentModifiers(PlayerEquipmentManager equipmentManager)
    {
        if (equipmentManager == null)
            yield break;

        foreach (EquipmentSlotData slot in equipmentManager.EquippedSlots)
        {
            ItemDefinition item = slot?.Item;
            if (item == null) continue;

            IEnumerable<StatModifier> modifiers = slot.EquipmentInstance != null && slot.EquipmentInstance.IsValid
                ? slot.EquipmentInstance.BuildModifiers(item)
                : item.BuildEquipmentModifiers(equipmentManager.GetUpgradeLevel(slot.SlotType));

            if (modifiers == null) continue;

            foreach (StatModifier modifier in modifiers)
                yield return modifier;
        }
    }

    private static void ApplyAttributePoints(
        ref StatValueAccumulator attack, ref StatValueAccumulator armor,
        ref StatValueAccumulator maxHealth, ref StatValueAccumulator critChance,
        ref StatValueAccumulator critDamage)
    {
        AddPointBonus(StatType.Attack, ref attack);
        AddPointBonus(StatType.Armor, ref armor);
        AddPointBonus(StatType.MaxHealth, ref maxHealth);
        AddPointBonus(StatType.CritChance, ref critChance);
        AddPointBonus(StatType.CritDamage, ref critDamage);
    }

    private static void AddPointBonus(StatType statType, ref StatValueAccumulator stat)
    {
        float bonus = PlayerAttributePointStorage.GetBonusValue(statType);
        if (!Mathf.Approximately(bonus, 0f))
            stat.Add(new StatModifier(statType, ModifierType.Flat, bonus));
    }

    private static void ApplyModifiers(
        IEnumerable<StatModifier> modifiers,
        ref StatValueAccumulator attack, ref StatValueAccumulator armor,
        ref StatValueAccumulator maxHealth, ref StatValueAccumulator critChance,
        ref StatValueAccumulator critDamage)
    {
        if (modifiers == null) return;

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier == null || !modifier.IsEnabled) continue;

            switch (modifier.StatType)
            {
                case StatType.Attack: attack.Add(modifier); break;
                case StatType.Armor: armor.Add(modifier); break;
                case StatType.MaxHealth: maxHealth.Add(modifier); break;
                case StatType.CritChance: critChance.Add(modifier); break;
                case StatType.CritDamage: critDamage.Add(modifier); break;
            }
        }
    }

    private struct StatValueAccumulator
    {
        private readonly float baseValue;
        private float flat;
        private float percentAdd;
        private float percentMultiply;

        public float Value => (baseValue + flat) * (1f + percentAdd) * percentMultiply;

        public StatValueAccumulator(float baseValue)
        {
            this.baseValue = baseValue;
            flat = 0f;
            percentAdd = 0f;
            percentMultiply = 1f;
        }

        public void Add(StatModifier modifier)
        {
            float value = modifier.GetEffectiveValue();
            switch (modifier.ModifierType)
            {
                case ModifierType.Flat: flat += value; break;
                case ModifierType.PercentAdd: percentAdd += value; break;
                case ModifierType.PercentMultiply: percentMultiply *= 1f + value; break;
            }
        }
    }
}
