using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ArmoredOrcSkillController : EnemySkillController
{
    private const float SpecialAttackChance = 0.4f;
    private const float SpecialCooldown = 7f;
    private const float InitialDelay = 2f;
    private const float ChargeDamageMultiplierValue = 0.7f;

    private readonly List<CharacterCtrl> targets = new();
    private float nextSpecialTime;
    private bool isExecutingSpecial;

    public bool IsCharging => isExecutingSpecial;
    public float ChargeDamageMultiplier => ChargeDamageMultiplierValue;

    protected override void OnEnable()
    {
        base.OnEnable();
        isExecutingSpecial = false;
        nextSpecialTime = Time.time + InitialDelay;
    }

    protected override void Update()
    {
        base.Update();
    }

    public override bool TryCastBasicAttack()
    {
        if (isExecutingSpecial || IsBlocking)
            return false;

        if (Time.time >= nextSpecialTime && HasComboSkills() && Random.value <= SpecialAttackChance)
        {
            return StartCombo();
        }

        return base.TryCastBasicAttack();
    }

    private bool StartCombo()
    {
        CharacterSkillRuntime thrustSkill = GetSkill(1);
        if (thrustSkill == null || !TryCast(1))
            return false;

        isExecutingSpecial = true;
        nextSpecialTime = Time.time + SpecialCooldown;
        StartCoroutine(TrySweepAfterThrust(thrustSkill.Definition.CastTime));
        return true;
    }

    private IEnumerator TrySweepAfterThrust(float thrustCastTime)
    {
        yield return new WaitForSeconds(thrustCastTime);
        while (IsCasting)
            yield return null;

        if (characterCtrl != null && characterCtrl.CharacterDamReceiver != null &&
            !characterCtrl.CharacterDamReceiver.IsDead && HasThrustTarget())
        {
            if (TryCast(2))
            {
                while (IsCasting)
                    yield return null;
            }
        }

        isExecutingSpecial = false;
    }

    private bool HasComboSkills()
    {
        return GetSkill(1)?.Definition != null && GetSkill(2)?.Definition != null;
    }

    private bool HasThrustTarget()
    {
        if (characterCtrl == null)
            return false;

        Transform target = CharacterSkillTargeting.FindTarget(characterCtrl);
        Vector2 direction = CharacterSkillTargeting.GetAimDirection(characterCtrl, target);
        if (direction.sqrMagnitude <= 0.001f)
            direction = characterCtrl.CharacterMovement.LookDirection;

        targets.Clear();
        CharacterSkillTargetUtility.FindCircleTargets(
            characterCtrl,
            (Vector2)characterCtrl.transform.position + direction * 0.72f,
            0.95f,
            default,
            targets);

        bool hasTarget = false;
        foreach (CharacterCtrl candidate in targets)
        {
            if (CharacterSkillTargetUtility.IsInsideAngle(
                    characterCtrl.transform.position, direction, candidate.transform.position, 70f))
            {
                hasTarget = true;
                break;
            }
        }

        targets.Clear();
        return hasTarget;
    }

}
