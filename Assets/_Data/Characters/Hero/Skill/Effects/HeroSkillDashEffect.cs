using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillDashEffect", menuName = "Loot Knights/Hero/Skill Effects/Dash")]
public class HeroSkillDashEffect : CharacterSkillEffectDefinition
{
    [SerializeField, Min(0f)] private float distance = 2.5f;
    [SerializeField, Min(0.01f)] private float duration = 0.12f;
    [SerializeField] private bool invincibleDuringDash = true;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Controller == null || context.Caster == null) return;

        context.Controller.StartCoroutine(DashRoutine(context.Caster, context.AimDirection));
    }

    private IEnumerator DashRoutine(CharacterCtrl caster, Vector2 direction)
    {
        if (caster.Rb == null) yield break;

        CharacterDamReceiver receiver = caster.CharacterDamReceiver;
        bool previousInvincible = receiver != null && receiver.IsInvincible;

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(true);

        Vector2 start = caster.Rb.position;
        Vector2 end = start + direction.normalized * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            caster.Rb.MovePosition(Vector2.Lerp(start, end, t));
            yield return null;
        }

        caster.Rb.MovePosition(end);

        if (invincibleDuringDash && receiver != null)
            receiver.SetInvincible(previousInvincible);
    }
}
