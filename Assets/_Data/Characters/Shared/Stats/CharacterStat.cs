using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStat : CharacterAbstract
{
    [Header("Offense")]
    public StatValue Attack;
    public StatValue CritChance;
    public StatValue CritDamage;

    [Header("Defense")]
    public StatValue Armor;
    public StatValue MaxHealth;

    [Header("Utility")]
    public StatValue MaxMana;
    public StatValue MoveSpeed;
    public StatValue AttackSpeed;
    public StatValue HealthRegen;
    public StatValue ManaRegen;

    [SerializeField] private float previousMaxHealth;
    public float PreviousMaxHealth => previousMaxHealth;

    [SerializeField] private float currentHealth;
    public float CurrentHealth => currentHealth;

    public event Action<StatType> OnStatChanged;
    public event Action<float> OnHealthChanged;

    protected override void Awake()
    {
        EnsureStatValues();
        InitBaseStats();

        currentHealth = MaxHealth.FinalValue;
        previousMaxHealth = MaxHealth.FinalValue;

        RegisterStatValueListeners();
    }

    protected override void Update()
    {
        base.Update();
        TickHealthRegen();
    }

    public virtual void InitBaseStats() { }

    private void EnsureStatValues()
    {
        Attack ??= new StatValue();
        CritChance ??= new StatValue();
        CritDamage ??= new StatValue();
        Armor ??= new StatValue();
        MaxHealth ??= new StatValue();
        MaxMana ??= new StatValue();
        MoveSpeed ??= new StatValue();
        AttackSpeed ??= new StatValue();
        HealthRegen ??= new StatValue();
        ManaRegen ??= new StatValue();
    }

    private void RegisterStatValueListeners()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            var stat = GetStat(type);

            if (stat == null)
                continue;

            StatType capturedType = type;

            stat.OnValueChanged += (finalValue) =>
            {
                NotifyStatChanged(capturedType);
            };
        }
    }

    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, MaxHealth?.FinalValue ?? value);

        OnHealthChanged?.Invoke(currentHealth);
    }

    private void ClampHealthToMax()
    {
        if (MaxHealth == null) return;

        if (currentHealth > MaxHealth.FinalValue)
        {
            SetCurrentHealth(MaxHealth.FinalValue);
        }
    }

    public virtual void RecalculateSkillTree(List<StatModifier> skillTreeModifiers)
    {
        ClearSkillTreeModifiers();

        foreach (var mod in skillTreeModifiers)
        {
            GetStat(mod.StatType)?.AddSkillTreeModifier(mod);
        }

        ClampHealthToMax();

        NotifyAllStatsChanged();
    }

    protected virtual void ClearSkillTreeModifiers()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            GetStat(type)?.ClearSkillTreeModifiers();
        }
    }

    public virtual void RecalculateEquipment(IEnumerable<StatModifier> equipmentModifiers)
    {
        ClearEquipmentModifiers();

        if (equipmentModifiers != null)
        {
            foreach (StatModifier mod in equipmentModifiers)
            {
                if (mod == null) continue;

                GetStat(mod.StatType)?.AddEquipmentModifier(mod);
            }
        }

        ClampHealthToMax();

        NotifyAllStatsChanged();
    }

    protected virtual void ClearEquipmentModifiers()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            GetStat(type)?.ClearEquipmentModifiers();
        }
    }

    public virtual void ClearAllModifiers()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            GetStat(type)?.ClearAllModifiers();
        }

        ClampHealthToMax();

        NotifyAllStatsChanged();
    }

    public virtual void RemoveModifiersFromSource(UnityEngine.Object source)
    {
        if (source == null) return;

        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            GetStat(type)?.RemoveModifierFromSource(source);
        }

        ClampHealthToMax();
        NotifyAllStatsChanged();
    }

    public void NotifyAllStatsChanged()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            NotifyStatChanged(type);
        }
    }

    protected void NotifyStatChanged(StatType type)
    {
        OnStatChanged?.Invoke(type);
    }

    private void TickHealthRegen()
    {
        if (HealthRegen == null || MaxHealth == null)
            return;

        if (characterCtrl != null &&
            characterCtrl.CharacterDamReceiver != null &&
            characterCtrl.CharacterDamReceiver.IsDead)
        {
            return;
        }

        float regenPerSecond = HealthRegen.FinalValue;
        if (regenPerSecond <= 0f || currentHealth >= MaxHealth.FinalValue)
            return;

        SetCurrentHealth(currentHealth + regenPerSecond * Time.deltaTime);
    }

    public virtual StatValue GetStat(StatType type)
    {
        return type switch
        {
            StatType.Attack => Attack,
            StatType.Armor => Armor,
            StatType.MaxHealth => MaxHealth,
            StatType.MaxMana => MaxMana,
            StatType.MoveSpeed => MoveSpeed,
            StatType.AttackSpeed => AttackSpeed,
            StatType.CritChance => CritChance,
            StatType.CritDamage => CritDamage,
            StatType.HealthRegen => HealthRegen,
            StatType.ManaRegen => ManaRegen,
            _ => null
        };
    }

    public void SetPreviousMaxHealth(float value)
    {
        previousMaxHealth = value;
    }
}
