using System.Collections.Generic;
using UnityEngine;

public static class ArcherAmbushStance
{
    private sealed class Buff
    {
        public float MultiplierBonus;
        public float CritChanceBonus;
        public float ExpireTime;
    }

    private static readonly Dictionary<CharacterCtrl, Buff> Buffs = new();

    public static void Apply(CharacterCtrl caster, float multiplierBonus, float critChanceBonus, float duration)
    {
        if (caster == null)
            return;

        Buffs[caster] = new Buff
        {
            MultiplierBonus = Mathf.Max(0f, multiplierBonus),
            CritChanceBonus = Mathf.Max(0f, critChanceBonus),
            ExpireTime = Time.time + Mathf.Max(0.1f, duration)
        };
    }

    public static bool TryConsume(CharacterCtrl caster, out float multiplierBonus, out float critChanceBonus)
    {
        multiplierBonus = 0f;
        critChanceBonus = 0f;

        if (caster == null || !Buffs.TryGetValue(caster, out Buff buff))
            return false;

        Buffs.Remove(caster);
        if (Time.time > buff.ExpireTime)
            return false;

        multiplierBonus = buff.MultiplierBonus;
        critChanceBonus = buff.CritChanceBonus;
        return true;
    }
}
