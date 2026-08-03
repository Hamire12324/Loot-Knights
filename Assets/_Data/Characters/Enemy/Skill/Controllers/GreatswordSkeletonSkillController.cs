using UnityEngine;

/// <summary>
/// Mini-boss attack pattern: regular swings build into a cleave, while the enraged
/// half of the fight can use a close-range ground smash to punish players who stay close.
/// </summary>
public sealed class GreatswordSkeletonSkillController : EnemySkillController
{
    [Header("Cleave Pattern")]
    [SerializeField, Min(2)] private int swingsBeforeCleave = 3;

    [Header("Enrage")]
    [SerializeField, Range(0f, 1f)] private float enrageHealthThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float gravebreakerChanceWhileEnraged = 0.45f;

    private int swingsSinceCleave;

    protected override void OnEnable()
    {
        base.OnEnable();
        swingsSinceCleave = 0;
    }

    public override bool TryCastBasicAttack()
    {
        CharacterSkillRuntime gravebreaker = GetSkill(1);
        if (IsEnraged() && gravebreaker != null && gravebreaker.CanCast(this) &&
            Random.value <= gravebreakerChanceWhileEnraged && TryCast(1))
        {
            swingsSinceCleave = 0;
            return true;
        }

        CharacterSkillRuntime cleave = GetSkill(0);
        if (swingsSinceCleave >= swingsBeforeCleave - 1 && cleave != null && cleave.CanCast(this) && TryCast(0))
        {
            swingsSinceCleave = 0;
            return true;
        }

        if (!base.TryCastBasicAttack())
            return false;

        swingsSinceCleave++;
        return true;
    }

    private bool IsEnraged()
    {
        CharacterStat stat = Enemy != null ? Enemy.CharacterStat : null;
        float maxHealth = stat != null && stat.MaxHealth != null ? stat.MaxHealth.FinalValue : 0f;
        return maxHealth > 0f && stat.CurrentHealth / maxHealth <= enrageHealthThreshold;
    }
}
