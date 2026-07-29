using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillDashEffect", menuName = "Loot Knights/Character/Skill Effects/Dash")]
public sealed class CharacterSkillDashEffect : CharacterSkillEffectDefinition
{
    [SerializeField, Min(0f)] private float distance = 3f;
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField] private bool invincibleDuringDash;
    [SerializeField] private string onArriveTrigger;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        context.Controller.StartCoroutine(DashRoutine(context.Caster, context.AimDirection));
    }

    private IEnumerator DashRoutine(CharacterCtrl caster, Vector2 direction)
    {
        if (caster.Rb == null)
            yield break;

        Vector2 normalizedDirection = direction == Vector2.zero ? Vector2.down : direction.normalized;
        Vector2 start = caster.Rb.position;
        Vector2 end = start + normalizedDirection * distance;
        CharacterDamReceiver receiver = caster.CharacterDamReceiver;
        bool wasInvincible = receiver != null && receiver.IsInvincible;

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            caster.Rb.MovePosition(Vector2.Lerp(start, end, Mathf.Clamp01(elapsed / duration)));
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
