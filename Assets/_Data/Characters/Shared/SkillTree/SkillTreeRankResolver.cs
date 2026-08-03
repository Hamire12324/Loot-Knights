using UnityEngine;
using System.Collections.Generic;

public static class SkillTreeRankResolver
{
    public static int GetRank(CharacterCtrl character, string nodeId)
    {
        if (character == null || string.IsNullOrWhiteSpace(nodeId))
            return 0;

        HeroSkillLoadoutPhotonSync loadoutSync = character.GetComponent<HeroSkillLoadoutPhotonSync>();
        SkillTreeDefinition tree = loadoutSync != null
            ? loadoutSync.FindSkillTreeContainingNode(nodeId)
            : null;

        SkillTreeNodeDefinition node = tree != null ? tree.FindNode(nodeId) : null;
        return tree != null && node != null
            ? PlayerSkillTreeManager.Service.GetRank(tree, node)
            : 0;
    }
}

public enum SkillModifierType
{
    DamageMultiplier,
    FlatDamage,
    Radius,
    Duration,
    TickIntervalReduction,
    LifeStealPercent,
    ProjectileLength,
    ProjectileSpeed,
    ProjectileCount,
    Penetration,
    HitStunDuration,
    DashDistance,
    CooldownReduction,
    AreaAngle,
    CooldownRefund,
    ManaRestoreMaxPercent,
    TemporaryMoveSpeedPercent,
    TemporaryMoveSpeedDuration,
    ReturnDamagePerOutboundHit,
    ReturnDamageStackCap,
    ChargeProjectilesPerCharge,
    ChargeCapacity,
    ChargeMinimum,
    TargetSearchRadius,
    DashDurationReduction,
    ReturnDamageMultiplier,
    ContinuousHitStun
}

[System.Serializable]
public sealed class SkillModifierData
{
    [SerializeField] private string skillId;
    [SerializeField] private SkillModifierType modifierType;
    [SerializeField] private float amount;
    [SerializeField] private bool scaleByRank = true;

    public string SkillId => skillId;
    public SkillModifierType ModifierType => modifierType;

    public float GetValue(int rank)
    {
        return scaleByRank ? amount * Mathf.Max(0, rank) : amount;
    }
}

public static class SkillTreeSkillModifierResolver
{
    public static float GetValue(CharacterCtrl character, string skillId, SkillModifierType modifierType)
    {
        if (character == null || string.IsNullOrWhiteSpace(skillId))
            return 0f;

        HeroSkillLoadoutPhotonSync loadout = character.GetComponent<HeroSkillLoadoutPhotonSync>();
        if (loadout == null)
            return 0f;

        float total = 0f;
        foreach (SkillTreeDefinition tree in loadout.GetSkillTrees())
        {
            if (tree == null)
                continue;

            foreach (SkillTreeNodeDefinition node in tree.Nodes)
            {
                if (node == null)
                    continue;

                int rank = PlayerSkillTreeManager.Service.GetRank(tree, node);
                if (rank <= 0)
                    continue;

                foreach (SkillModifierData modifier in node.SkillModifiers)
                {
                    if (modifier != null && modifier.SkillId == skillId && modifier.ModifierType == modifierType)
                        total += modifier.GetValue(rank);
                }
            }
        }

        return total;
    }
}
