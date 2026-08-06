using UnityEngine;

/// <summary>
/// Werewolf pressures with sword slashes, then blinks through its target for
/// a fast finishing slash after the player has seen two regular attacks.
/// </summary>
public sealed class WerewolfSkillController : EnemySkillController
{
    [SerializeField, Min(1)] private int basicAttacksBeforeBlink = 2;
    [SerializeField, Range(0f, 1f)] private float blinkChance = 0.35f;
    [SerializeField, Min(0f)] private float autoBlinkMinDistance = 1.7f;

    private int basicAttacksSinceBlink;

    protected override void OnEnable()
    {
        base.OnEnable();
        basicAttacksSinceBlink = 0;
    }

    protected override void Update()
    {
        base.Update();
        TryAutoBlinkToTarget();
    }

    public override bool TryCastBasicAttack()
    {
        if (IsBlocking || IsCasting)
            return false;

        CharacterSkillRuntime blinkSlash = GetSkill(0);
        if (basicAttacksSinceBlink >= basicAttacksBeforeBlink &&
            blinkSlash != null && blinkSlash.CanCast(this) && Random.value <= blinkChance && TryCast(0))
        {
            basicAttacksSinceBlink = 0;
            return true;
        }

        if (!base.TryCastBasicAttack())
            return false;

        basicAttacksSinceBlink++;
        return true;
    }

    private void TryAutoBlinkToTarget()
    {
        if (IsCasting || Enemy == null || Enemy.CharacterDamReceiver == null ||
            Enemy.CharacterDamReceiver.IsDead || Enemy.CharacterTargetFinder == null)
        {
            return;
        }

        Transform target = Enemy.CharacterTargetFinder.CurrentTarget;
        if (target == null || Vector2.Distance(Enemy.transform.position, target.position) < autoBlinkMinDistance)
            return;

        CharacterSkillRuntime blinkSlash = GetSkill(0);
        if (blinkSlash != null && blinkSlash.CanCast(this))
            TryCast(0);
    }
}
