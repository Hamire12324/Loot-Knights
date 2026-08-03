using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillShieldBashEffect", menuName = "Loot Knights/Enemy/Skill Effects/Shield Bash")]
public sealed class CharacterSkillShieldBashEffect : CharacterSkillEffectDefinition
{
    [Header("Hit Area")]
    [SerializeField] private Vector2 size = new(1.1f, 0.8f);
    [SerializeField, Min(0f)] private float forwardOffset = 0.55f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Continuous Damage and Knockback")]
    [SerializeField] private DamageData damageData = new(0.25f, false)
    {
        CausesHitStun = true,
        HitStunDuration = 0.12f,
        HitStunImmunityDuration = 0.5f
    };
    [SerializeField, Min(0.05f)] private float damageInterval = 0.2f;
    [SerializeField, Min(0f)] private float knockbackForce = 2.4f;

    [Header("Animation Binding")]
    [SerializeField] private string animationStateName = "Attack_Special";
    [SerializeField, Min(0.05f)] private float maximumDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool logHitResults;

    private readonly List<CharacterCtrl> targets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        context.Controller.StartCoroutine(DealDamageWhileAnimationPlays(context));
    }

    private IEnumerator DealDamageWhileAnimationPlays(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        Animator animator = caster.Animator;
        if (animator == null)
            yield break;

        float endTime = Time.time + maximumDuration;
        while (!IsAnimationStateActive(animator) && Time.time < endTime)
            yield return null;

        if (!IsAnimationStateActive(animator))
        {
            if (logHitResults)
                Debug.LogWarning($"Shield Bash cancelled: animator never entered '{animationStateName}'.", caster);
            yield break;
        }

        while (IsAnimationStateActive(animator) && Time.time < endTime)
        {
            DealDamageTick(caster, context.AimDirection);
            yield return new WaitForSeconds(damageInterval);
        }
    }

    private void DealDamageTick(CharacterCtrl caster, Vector2 aimDirection)
    {
        Vector2 direction = aimDirection.sqrMagnitude > 0.001f
            ? aimDirection.normalized
            : caster.CharacterMovement.LookDirection;
        Vector2 center = (Vector2)caster.transform.position + direction * forwardOffset;

        targets.Clear();
        CharacterSkillTargetUtility.FindBoxTargets(caster, center, size, 0f, targetLayer, targets);

        foreach (CharacterCtrl target in targets)
        {
            float healthBefore = target.CharacterStat != null ? target.CharacterStat.CurrentHealth : 0f;
            if (!CharacterSkillDamageUtility.DealDamage(caster, target, damageData))
                continue;

            float appliedDamage = healthBefore - (target.CharacterStat != null ? target.CharacterStat.CurrentHealth : healthBefore);

            Rigidbody2D targetBody = target.Rb;
            if (targetBody != null && knockbackForce > 0f)
                targetBody.linearVelocity = direction * knockbackForce;
        }
        targets.Clear();
    }

    private bool IsAnimationStateActive(Animator animator)
    {
        if (string.IsNullOrWhiteSpace(animationStateName))
            return true;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(animationStateName))
            return true;

        return animator.IsInTransition(0) &&
               animator.GetNextAnimatorStateInfo(0).IsName(animationStateName);
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.05f, size.x);
        size.y = Mathf.Max(0.05f, size.y);
        forwardOffset = Mathf.Max(0f, forwardOffset);
        damageInterval = Mathf.Max(0.05f, damageInterval);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        maximumDuration = Mathf.Max(0.05f, maximumDuration);
        damageData ??= new DamageData(0.25f, false);
    }
}
