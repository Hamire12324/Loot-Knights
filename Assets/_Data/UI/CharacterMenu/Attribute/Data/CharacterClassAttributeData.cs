using System;
using UnityEngine;

[Serializable]
public class CharacterClassAttributeData
{
    [SerializeField] private CharacterClass characterClass = CharacterClass.Knight;
    [SerializeField] private float attack = 100f;
    [SerializeField] private float armor;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float critChance = 0.05f;
    [SerializeField] private float critDamage = 1.5f;

    public CharacterClass CharacterClass => characterClass;

    public CharacterAttributeData ToAttributeData()
    {
        float safeMaxHealth = Mathf.Max(1f, maxHealth);

        return new CharacterAttributeData(
            attack,
            armor,
            safeMaxHealth,
            safeMaxHealth,
            critChance,
            critDamage);
    }
}
