using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillDashEffect", menuName = "Loot Knights/Character/Skill Effects/Dash")]
public sealed class CharacterSkillDashEffect : CharacterSkillEffectDefinition
{
    [System.Serializable]
    private struct RankScaling
    {
        [SerializeField] private string skillTreeNodeId;
        [SerializeField] private float distancePerRank;
        [SerializeField, Range(0f, 0.95f)] private float durationReductionPerRank;

        public void Apply(CharacterCtrl caster, ref float effectiveDistance, ref float effectiveDuration)
        {
            int rank = string.IsNullOrWhiteSpace(skillTreeNodeId)
                ? 0
                : SkillTreeRankResolver.GetRank(caster, skillTreeNodeId);

            effectiveDistance += rank * distancePerRank;
            effectiveDuration *= Mathf.Max(0.05f, 1f - rank * durationReductionPerRank);
        }
    }

    [SerializeField, Min(0f)] private float distance = 3f;
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField] private bool instantTeleport;
    [SerializeField] private bool invincibleDuringDash;
    [SerializeField] private string onArriveTrigger;
    [SerializeField] private RankScaling[] rankScalings;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        if (instantTeleport)
        {
            TeleportImmediately(
                context.Caster,
                context.AimDirection,
                context.Definition != null ? context.Definition.SkillId : null,
                context.HasManualTargetPosition ? context.ManualTargetPosition : (Vector2?)null);
            return;
        }

        context.Controller.StartCoroutine(DashRoutine(
            context.Caster,
            context.AimDirection,
            context.Definition != null ? context.Definition.SkillId : null,
            context.HasManualTargetPosition ? context.ManualTargetPosition : (Vector2?)null));
    }

    private void TeleportImmediately(
        CharacterCtrl caster,
        Vector2 direction,
        string skillId,
        Vector2? manualTargetPosition)
    {
        if (caster.Rb == null)
            return;

        Vector2 start = caster.Rb.position;
        Vector2 normalizedDirection = GetDashDirection(direction, start, manualTargetPosition);
        float effectiveDistance = distance;
        float unusedDuration = duration;
        foreach (RankScaling scaling in rankScalings ?? System.Array.Empty<RankScaling>())
            scaling.Apply(caster, ref effectiveDistance, ref unusedDuration);

        effectiveDistance += SkillTreeSkillModifierResolver.GetValue(
            caster,
            skillId,
            SkillModifierType.DashDistance);
        effectiveDistance = ClampToManualTargetDistance(effectiveDistance, start, manualTargetPosition);

        caster.Rb.linearVelocity = Vector2.zero;
        caster.Rb.position = start + normalizedDirection * Mathf.Max(0f, effectiveDistance);
        PlayArrivalTrigger(caster);
    }

    private IEnumerator DashRoutine(
        CharacterCtrl caster,
        Vector2 direction,
        string skillId,
        Vector2? manualTargetPosition)
    {
        if (caster.Rb == null)
            yield break;

        Vector2 start = caster.Rb.position;
        Vector2 normalizedDirection = GetDashDirection(direction, start, manualTargetPosition);
        float effectiveDistance = distance;
        float effectiveDuration = duration;
        foreach (RankScaling scaling in rankScalings ?? System.Array.Empty<RankScaling>())
            scaling.Apply(caster, ref effectiveDistance, ref effectiveDuration);

        effectiveDistance += SkillTreeSkillModifierResolver.GetValue(
            caster,
            skillId,
            SkillModifierType.DashDistance);
        float durationReduction = SkillTreeSkillModifierResolver.GetValue(
            caster,
            skillId,
            SkillModifierType.DashDurationReduction);
        effectiveDuration *= Mathf.Max(0.05f, 1f - durationReduction);

        effectiveDistance = ClampToManualTargetDistance(effectiveDistance, start, manualTargetPosition);
        effectiveDuration = Mathf.Max(0.01f, effectiveDuration);
        Vector2 end = start + normalizedDirection * effectiveDistance;
        CharacterDamReceiver receiver = caster.CharacterDamReceiver;
        bool wasInvincible = receiver != null && receiver.IsInvincible;

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(true);

        float elapsed = 0f;
        while (elapsed < effectiveDuration)
        {
            elapsed += Time.deltaTime;
            caster.Rb.MovePosition(Vector2.Lerp(start, end, Mathf.Clamp01(elapsed / effectiveDuration)));
            yield return null;
        }

        caster.Rb.MovePosition(end);
        PlayArrivalTrigger(caster);

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(wasInvincible);
    }

    private static Vector2 GetDashDirection(Vector2 fallbackDirection, Vector2 start, Vector2? manualTargetPosition)
    {
        if (manualTargetPosition.HasValue)
        {
            Vector2 manualDirection = manualTargetPosition.Value - start;
            if (manualDirection.sqrMagnitude > 0.001f)
                return manualDirection.normalized;
        }

        return fallbackDirection == Vector2.zero ? Vector2.down : fallbackDirection.normalized;
    }

    private static float ClampToManualTargetDistance(
        float effectiveDistance,
        Vector2 start,
        Vector2? manualTargetPosition)
    {
        effectiveDistance = Mathf.Max(0f, effectiveDistance);
        if (!manualTargetPosition.HasValue)
            return effectiveDistance;

        float desiredDistance = Vector2.Distance(start, manualTargetPosition.Value);
        return Mathf.Min(effectiveDistance, desiredDistance);
    }

    private void PlayArrivalTrigger(CharacterCtrl caster)
    {
        if (string.IsNullOrWhiteSpace(onArriveTrigger) || caster == null || caster.Animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in caster.Animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger || parameter.name != onArriveTrigger)
                continue;

            caster.Animator.SetTrigger(onArriveTrigger);
            return;
        }
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0f, distance);
        duration = Mathf.Max(0.01f, duration);
    }
}
