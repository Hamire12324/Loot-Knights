using UnityEngine;

/// <summary>
/// Gives the Armored Skeleton an occasional Shield Bash while preserving its normal basic attack.
/// </summary>
public sealed class ArmoredSkeletonSkillController : EnemySkillController
{
    [Header("Shield Bash")]
    [SerializeField, Range(0f, 1f)] private float shieldBashChance = 0.35f;

    public override bool TryCastBasicAttack()
    {
        CharacterSkillRuntime shieldBash = GetSkill(0);
        if (shieldBash != null && shieldBash.CanCast(this) && Random.value <= shieldBashChance)
            return TryCast(0);

        return base.TryCastBasicAttack();
    }
}
