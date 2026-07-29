using UnityEngine;

public static class CharacterSkillDamageUtility
{
    public static float CalculateDamage(
        CharacterCtrl caster,
        DamageData damageData,
        float flatBonusDamage = 0f,
        float multiplierBonus = 0f,
        float critChanceBonus = 0f)
    {
        if (caster == null || caster.CharacterStat == null)
            return 0f;

        float multiplier = (damageData != null ? damageData.Multiplier : 1f) + multiplierBonus;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue + critChanceBonus)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    public static bool DealDamage(
        CharacterCtrl caster,
        CharacterCtrl target,
        DamageData damageData,
        float flatBonusDamage = 0f,
        float multiplierBonus = 0f,
        float critChanceBonus = 0f)
    {
        if (target == null || target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead)
            return false;

        float damage = CalculateDamage(caster, damageData, flatBonusDamage, multiplierBonus, critChanceBonus);
        target.CharacterDamReceiver.ReceiveDamage(damage, caster != null ? caster.transform : null, damageData);
        return true;
    }

    public static DamageData CloneWithMultiplier(DamageData damageData, float multiplier)
    {
        DamageData clone = damageData != null
            ? damageData.CloneWithElement(damageData.Element)
            : new DamageData(1f, true);

        clone.Multiplier *= multiplier;
        return clone;
    }
}
