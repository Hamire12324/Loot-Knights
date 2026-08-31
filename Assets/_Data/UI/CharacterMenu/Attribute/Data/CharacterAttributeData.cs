/// <summary>
/// Values ready for the Attribute UI. This is data only: no GameObject,
/// events, or modifier logic lives here.
/// </summary>
public readonly struct CharacterAttributeData
{
    public readonly bool IsValid;
    public readonly float Attack;
    public readonly float Armor;
    public readonly float CurrentHealth;
    public readonly float MaxHealth;
    public readonly float CritChance;
    public readonly float CritDamage;

    public CharacterAttributeData(
        float attack, float armor, float currentHealth, float maxHealth,
        float critChance, float critDamage)
    {
        IsValid = true;
        Attack = attack;
        Armor = armor;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        CritChance = critChance;
        CritDamage = critDamage;
    }
}
