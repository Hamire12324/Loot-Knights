using UnityEngine;

/// <summary>
/// Werebear's attack cadence is deliberately readable: two swipes lead into a
/// double rake, then one more swipe leads into its high-impact ground slam.
/// </summary>
public sealed class WerebearSkillController : EnemySkillController
{
    [SerializeField, Min(1)] private int basicAttacksBeforeSpecial = 2;
    [SerializeField, Range(0f, 1f)] private float specialChance = 0.4f;

    private int basicAttacksSinceSpecial;
    private bool useTwinRakeNext = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        basicAttacksSinceSpecial = 0;
        useTwinRakeNext = true;
    }

    public override bool TryCastBasicAttack()
    {
        if (IsBlocking || IsCasting)
            return false;

        if (basicAttacksSinceSpecial >= basicAttacksBeforeSpecial)
        {
            int skillIndex = useTwinRakeNext ? 0 : 1;
            CharacterSkillRuntime skill = GetSkill(skillIndex);
            if (skill != null && skill.CanCast(this) && Random.value <= specialChance && TryCast(skillIndex))
            {
                basicAttacksSinceSpecial = 0;
                useTwinRakeNext = !useTwinRakeNext;
                return true;
            }
        }

        if (!base.TryCastBasicAttack())
            return false;

        basicAttacksSinceSpecial++;
        return true;
    }
}
