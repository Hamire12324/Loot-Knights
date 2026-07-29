using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillStatModifierEffect", menuName = "Loot Knights/Character/Skill Effects/Stat Modifier")]
public sealed class CharacterSkillStatModifierEffect : CharacterSkillEffectDefinition
{
    [Header("Target")]
    [SerializeField] private bool applyToCaster = true;

    [Header("Stat")]
    [SerializeField] private StatType statType = StatType.Attack;
    [SerializeField] private ModifierType modifierType = ModifierType.PercentAdd;
    [SerializeField] private float amount = 0.25f;
    [SerializeField, Min(0f)] private float duration = 4f;
    [SerializeField] private string skillTreeNodeId;
    [SerializeField] private float amountPerRank;
    [SerializeField] private float durationPerRank;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition vfx;
    [SerializeField, Min(0.05f)] private float vfxInterval = 0.65f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl target = applyToCaster
            ? context.Caster
            : context.Target != null
                ? context.Target.GetComponentInParent<CharacterCtrl>()
                : null;

        if (target == null || target.CharacterStat == null)
            return;

        StatValue stat = target.CharacterStat.GetStat(statType);
        if (stat == null)
            return;

        int rank = string.IsNullOrWhiteSpace(skillTreeNodeId)
            ? 0
            : SkillTreeRankResolver.GetRank(target, skillTreeNodeId);
        float effectiveAmount = amount + rank * amountPerRank;
        float effectiveDuration = Mathf.Max(0f, duration + rank * durationPerRank);

        stat.AddBuffModifier(new StatModifier(statType, modifierType, effectiveAmount, this, effectiveDuration));
        stat.NotifyValueChanged();
        target.CharacterStat.NotifyAllStatsChanged();

        if (context.Controller != null && effectiveDuration > 0f)
            context.Controller.StartCoroutine(NotifyWhenExpired(target.CharacterStat, statType, effectiveDuration));

        PlayFeedback(context.Controller, target.transform, context.AimDirection, effectiveDuration);
    }

    private static IEnumerator NotifyWhenExpired(CharacterStat stat, StatType statType, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (stat == null)
            yield break;

        stat.GetStat(statType)?.NotifyValueChanged();
        stat.NotifyAllStatsChanged();
    }

    private void PlayFeedback(MonoBehaviour runner, Transform anchor, Vector2 direction, float activeDuration)
    {
        if (anchor == null || vfx == null)
            return;

        if (runner == null || activeDuration <= 0f)
        {
            CharacterSkillVfxUtility.Play(vfx, anchor.position, direction, anchor);
            return;
        }

        runner.StartCoroutine(PlayFeedbackRoutine(anchor, direction, activeDuration));
    }

    private IEnumerator PlayFeedbackRoutine(Transform anchor, Vector2 direction, float activeDuration)
    {
        float elapsed = 0f;
        WaitForSeconds wait = new(Mathf.Max(0.05f, vfxInterval));

        while (anchor != null && elapsed < activeDuration)
        {
            CharacterSkillVfxUtility.Play(vfx, anchor.position, direction, anchor);
            yield return wait;
            elapsed += vfxInterval;
        }
    }
}
