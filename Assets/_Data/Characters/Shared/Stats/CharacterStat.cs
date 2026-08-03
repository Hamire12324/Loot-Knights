using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStat : CharacterAbstract
{
    private static readonly StatType[] StatTypes = (StatType[])Enum.GetValues(typeof(StatType));

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

    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMana;
    public float CurrentHealth => currentHealth;
    public float CurrentMana => currentMana;

    public event Action<StatType> OnStatChanged;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnManaChanged;

    protected override void Awake()
    {
        EnsureStatValues();
        InitBaseStats();

        currentHealth = MaxHealth.FinalValue;
        currentMana = MaxMana.FinalValue;

        RegisterStatValueListeners();
    }

    protected override void Update()
    {
        base.Update();
        TickHealthRegen();
        TickManaRegen();
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
        foreach (StatType type in StatTypes)
        {
            StatValue stat = GetStat(type);

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

    public void SetCurrentMana(float value)
    {
        currentMana = Mathf.Clamp(value, 0f, MaxMana?.FinalValue ?? value);
        OnManaChanged?.Invoke(currentMana);
    }

    public void RestoreMana(float amount)
    {
        if (amount > 0f)
            SetCurrentMana(currentMana + amount);
    }

    public bool TrySpendMana(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentMana < amount)
            return false;

        SetCurrentMana(currentMana - amount);
        return true;
    }

    public virtual void RecalculateSkillTree(List<StatModifier> skillTreeModifiers)
    {
        float previousMaxHealth = GetMaxHealth();
        float previousCurrentHealth = currentHealth;
        float previousMaxMana = GetMaxMana();
        float previousCurrentMana = currentMana;

        ClearSkillTreeModifiers();

        if (skillTreeModifiers != null)
        {
            foreach (StatModifier mod in skillTreeModifiers)
            {
                if (mod == null) continue;

                GetStat(mod.StatType)?.AddSkillTreeModifier(mod);
            }
        }

        ApplyMaxHealthDelta(previousMaxHealth, previousCurrentHealth);
        ApplyMaxManaDelta(previousMaxMana, previousCurrentMana);
        NotifyAllStatsChanged();
    }

    protected virtual void ClearSkillTreeModifiers()
    {
        ForEachStat(stat => stat.ClearSkillTreeModifiers());
    }

    public virtual void RecalculateEquipment(IEnumerable<StatModifier> equipmentModifiers)
    {
        float previousMaxHealth = GetMaxHealth();
        float previousCurrentHealth = currentHealth;
        float previousMaxMana = GetMaxMana();
        float previousCurrentMana = currentMana;

        ClearEquipmentModifiers();

        if (equipmentModifiers != null)
        {
            foreach (StatModifier mod in equipmentModifiers)
            {
                if (mod == null) continue;

                GetStat(mod.StatType)?.AddEquipmentModifier(mod);
            }
        }

        ApplyMaxHealthDelta(previousMaxHealth, previousCurrentHealth);
        ApplyMaxManaDelta(previousMaxMana, previousCurrentMana);
        NotifyAllStatsChanged();
    }

    protected virtual void ClearEquipmentModifiers()
    {
        ForEachStat(stat => stat.ClearEquipmentModifiers());
    }

    public virtual void ClearAllModifiers()
    {
        float previousMaxHealth = GetMaxHealth();
        float previousCurrentHealth = currentHealth;
        float previousMaxMana = GetMaxMana();
        float previousCurrentMana = currentMana;

        ForEachStat(stat => stat.ClearAllModifiers());

        ApplyMaxHealthDelta(previousMaxHealth, previousCurrentHealth);
        ApplyMaxManaDelta(previousMaxMana, previousCurrentMana);
        NotifyAllStatsChanged();
    }

    public virtual void RemoveModifiersFromSource(UnityEngine.Object source, bool updateHealthByMaxHealthDelta = true)
    {
        if (source == null) return;

        float previousMaxHealth = GetMaxHealth();
        float previousCurrentHealth = currentHealth;
        float previousMaxMana = GetMaxMana();
        float previousCurrentMana = currentMana;

        ForEachStat(stat => stat.RemoveModifierFromSource(source));

        if (updateHealthByMaxHealthDelta)
        {
            ApplyMaxHealthDelta(previousMaxHealth, previousCurrentHealth);
            ApplyMaxManaDelta(previousMaxMana, previousCurrentMana);
        }

        NotifyAllStatsChanged();
    }

    public void NotifyAllStatsChanged()
    {
        foreach (StatType type in StatTypes)
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

    private void TickManaRegen()
    {
        if (ManaRegen == null || MaxMana == null || currentMana >= MaxMana.FinalValue)
            return;

        float regenPerSecond = ManaRegen.FinalValue;
        if (regenPerSecond > 0f)
            SetCurrentMana(currentMana + regenPerSecond * Time.deltaTime);
    }

    public void ApplyMaxHealthDelta(float previousMaxHealth, float previousCurrentHealth)
    {
        SetCurrentHealth(previousCurrentHealth + GetMaxHealth() - previousMaxHealth);
    }

    public void ApplyMaxManaDelta(float previousMaxMana, float previousCurrentMana)
    {
        SetCurrentMana(previousCurrentMana + GetMaxMana() - previousMaxMana);
    }

    private float GetMaxHealth()
    {
        return MaxHealth != null ? MaxHealth.FinalValue : 0f;
    }

    private float GetMaxMana()
    {
        return MaxMana != null ? MaxMana.FinalValue : 0f;
    }

    private void ForEachStat(Action<StatValue> action)
    {
        foreach (StatType type in StatTypes)
        {
            StatValue stat = GetStat(type);
            if (stat != null)
                action(stat);
        }
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
}
