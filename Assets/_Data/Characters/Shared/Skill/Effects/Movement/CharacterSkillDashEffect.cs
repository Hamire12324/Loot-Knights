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
    [SerializeField] private bool invincibleDuringDash;
    [SerializeField] private string onArriveTrigger;
    [SerializeField] private RankScaling[] rankScalings;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        context.Controller.StartCoroutine(DashRoutine(
            context.Caster,
            context.AimDirection,
            context.Definition != null ? context.Definition.SkillId : null));
    }

    private IEnumerator DashRoutine(CharacterCtrl caster, Vector2 direction, string skillId)
    {
        if (caster.Rb == null)
            yield break;

        Vector2 normalizedDirection = direction == Vector2.zero ? Vector2.down : direction.normalized;
        Vector2 start = caster.Rb.position;
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

        effectiveDistance = Mathf.Max(0f, effectiveDistance);
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
