public readonly struct AttributeStatSnapshot
{
    public readonly bool IsValid;
    public readonly float Attack;
    public readonly float Armor;
    public readonly float CurrentHealth;
    public readonly float MaxHealth;
    public readonly float CritChance;
    public readonly float CritDamage;

    public AttributeStatSnapshot(
        float attack,
        float armor,
        float currentHealth,
        float maxHealth,
        float critChance,
        float critDamage)
    {
        IsValid = true;
        Attack = attack;
        Armor = armor;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        CritChance = critChance;
        CritDamage = critDamage;
    }

    public static AttributeStatSnapshot FromCharacterStat(CharacterStat characterStat)
    {
        if (characterStat == null)
            return default;

        float maxHealth = characterStat.MaxHealth?.FinalValue ?? 0f;
        float currentHealth = maxHealth > 0f ? characterStat.CurrentHealth : 0f;
        if (currentHealth <= 0f && maxHealth > 0f)
            currentHealth = maxHealth;

        return new AttributeStatSnapshot(
            characterStat.Attack?.FinalValue ?? 0f,
            characterStat.Armor?.FinalValue ?? 0f,
            currentHealth,
            maxHealth,
            characterStat.CritChance?.FinalValue ?? 0f,
            characterStat.CritDamage?.FinalValue ?? 0f);
    }
}
