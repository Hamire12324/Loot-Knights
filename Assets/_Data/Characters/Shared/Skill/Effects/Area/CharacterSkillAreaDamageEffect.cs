using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillAreaDamageEffect", menuName = "Loot Knights/Character/Skill Effects/Area Damage")]
public sealed class CharacterSkillAreaDamageEffect : CharacterSkillEffectDefinition
{
    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField, Range(1f, 360f)] private float angle = 360f;
    [SerializeField, Min(0f)] private float forwardOffset;
    [SerializeField] private float sideOffset;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField, Min(0f)] private float delay;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(1f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField] private float multiplierBonus;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition hitAreaVfx;

    private readonly List<CharacterCtrl> targets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (delay > 0f && context.Controller != null)
        {
            context.Controller.StartCoroutine(ExecuteAfterDelay(context));
            return;
        }

        ExecuteArea(context);
    }

    private IEnumerator ExecuteAfterDelay(CharacterSkillExecutionContext context)
    {
        yield return new WaitForSeconds(delay);
        ExecuteArea(context);
    }

    private void ExecuteArea(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 right = new(-direction.y, direction.x);
        Vector2 origin = (Vector2)caster.transform.position + direction * forwardOffset + right * sideOffset;
        string skillId = context.Definition != null ? context.Definition.SkillId : null;
        float effectiveRadius = radius + SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.Radius);
        float effectiveAngle = Mathf.Min(360f, angle + SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.AreaAngle));
        float effectiveFlatDamage = flatBonusDamage + SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.FlatDamage);
        float effectiveMultiplierBonus = multiplierBonus + SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.DamageMultiplier);
        float lifeStealPercent = SkillTreeSkillModifierResolver.GetValue(caster, skillId, SkillModifierType.LifeStealPercent);

        targets.Clear();
        CharacterSkillTargetUtility.FindCircleTargets(caster, origin, effectiveRadius, targetLayer, targets);

        for (int i = 0; i < targets.Count; i++)
        {
            CharacterCtrl target = targets[i];
            if (target == null)
                continue;

            if (!CharacterSkillTargetUtility.IsInsideAngle(caster.transform.position, direction, target.transform.position, effectiveAngle))
                continue;

            if (CharacterSkillDamageUtility.DealDamage(caster, target, damageData, out float dealtDamage, effectiveFlatDamage, effectiveMultiplierBonus) &&
                lifeStealPercent > 0f &&
                dealtDamage > 0f)
            {
                caster.CharacterDamReceiver?.Heal(dealtDamage * lifeStealPercent);
            }
        }

        targets.Clear();
        CharacterSkillVfxUtility.Play(hitAreaVfx, origin, direction);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
        forwardOffset = Mathf.Max(0f, forwardOffset);
        delay = Mathf.Max(0f, delay);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
