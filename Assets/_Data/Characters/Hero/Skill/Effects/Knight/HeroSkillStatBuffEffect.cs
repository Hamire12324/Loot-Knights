using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillStatBuffEffect", menuName = "Loot Knights/Hero/Skill Effects/Stat Buff")]
public class HeroSkillStatBuffEffect : CharacterSkillEffectDefinition
{
    private const float MinFeedbackInterval = 0.05f;

    [Header("Stat")]
    [SerializeField] private StatType statType = StatType.Attack;
    [SerializeField] private ModifierType modifierType = ModifierType.PercentAdd;
    [SerializeField] private float amount = 0.25f;
    [SerializeField, Min(0f)] private float duration = 4f;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition buffVfx;
    [SerializeField, Min(MinFeedbackInterval)] private float buffVfxInterval = 0.65f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null) return;

        StatValue stat = caster.CharacterStat.GetStat(statType);
        if (stat == null) return;

        StatModifier modifier = new(statType, modifierType, amount, this, duration);
        stat.AddBuffModifier(modifier);
        stat.NotifyValueChanged();

        if (duration > 0f && context.Controller != null)
        {
            context.Controller.StartCoroutine(NotifyWhenExpired(caster.CharacterStat, statType, duration));
            PlayBuffFeedback(context.Controller, caster.transform, context.AimDirection, duration);
        }
        else
        {
            PlayBuffFeedbackOnce(caster.transform, context.AimDirection);
        }
    }

    private static IEnumerator NotifyWhenExpired(CharacterStat stat, StatType type, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (stat == null) yield break;

        stat.GetStat(type)?.NotifyValueChanged();
        stat.NotifyAllStatsChanged();
    }

    private void PlayBuffFeedback(MonoBehaviour runner, Transform anchor, Vector2 direction, float activeDuration)
    {
        if (runner == null || anchor == null || buffVfx == null || !VFXManager.HasInstance)
            return;

        runner.StartCoroutine(PlayBuffFeedbackRoutine(anchor, direction, activeDuration));
    }

    private IEnumerator PlayBuffFeedbackRoutine(Transform anchor, Vector2 direction, float activeDuration)
    {
        float interval = Mathf.Max(MinFeedbackInterval, buffVfxInterval);
        float elapsed = 0f;

        while (anchor != null && elapsed < activeDuration)
        {
            PlayBuffFeedbackOnce(anchor, direction);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    private void PlayBuffFeedbackOnce(Transform anchor, Vector2 direction)
    {
        if (anchor == null || buffVfx == null || !VFXManager.HasInstance)
            return;

        VFXManager.InstanceOrNull.Play(buffVfx, anchor.position, direction, anchor);
    }
}
