using UnityEngine;

/// <summary>
/// Elite Orc combat rhythm: two axe chops telegraph a high-impact jump slam.
/// Once its spin is ready, it periodically replaces the slam with an evasive, full-circle axe spin.
/// </summary>
public sealed class EliteOrcSkillController : EnemySkillController
{
    [Header("Pattern")]
    [SerializeField, Min(1)] private int basicAttacksBeforeSpecial = 2;
    [SerializeField, Min(1)] private int enragedAttacksBeforeSpin = 2;

    private int basicAttacksSinceSpecial;

    protected override void OnEnable()
    {
        base.OnEnable();
        basicAttacksSinceSpecial = 0;
    }

    public override bool TryCastBasicAttack()
    {
        if (IsBlocking || IsCasting)
            return false;

        if (ShouldUseSpin() && TryCast(1))
        {
            basicAttacksSinceSpecial = 0;
            return true;
        }

        if (basicAttacksSinceSpecial >= basicAttacksBeforeSpecial && TryCast(0))
        {
            basicAttacksSinceSpecial = 0;
            return true;
        }

        if (!base.TryCastBasicAttack())
            return false;

        basicAttacksSinceSpecial++;
        return true;
    }

    private bool ShouldUseSpin()
    {
        if (basicAttacksSinceSpecial < enragedAttacksBeforeSpin || GetSkill(1) == null)
            return false;

        return true;
    }
}
