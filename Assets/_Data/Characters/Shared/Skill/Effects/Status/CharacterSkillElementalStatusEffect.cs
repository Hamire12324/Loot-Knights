using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillElementalStatusEffect", menuName = "Loot Knights/Character/Skill Effects/Elemental Status")]
public sealed class CharacterSkillElementalStatusEffect : CharacterSkillEffectDefinition
{
    [Header("Shape")]
    [SerializeField, Min(0.05f)] private float radius = 1.25f;
    [SerializeField, Range(1f, 360f)] private float angle = 360f;
    [SerializeField, Min(0f)] private float forwardOffset;
    [SerializeField] private float sideOffset;
    [SerializeField] private LayerMask targetLayer;

    [Header("Element")]
    [SerializeField] private ElementType element = ElementType.Fire;
    [SerializeField, Min(0f)] private float elementalPower = 1f;
    [SerializeField, Min(0f)] private float statusDuration = 4f;
    [SerializeField] private bool consumeElementOnReaction = true;

    private readonly List<CharacterCtrl> targets = new();

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || element == ElementType.None)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 right = new(-direction.y, direction.x);
        Vector2 origin = (Vector2)caster.transform.position + direction * forwardOffset + right * sideOffset;

        DamageData statusData = new DamageData(0f, false).CloneWithElement(
            element,
            elementalPower,
            statusDuration,
            consumeElementOnReaction);

        targets.Clear();
        CharacterSkillTargetUtility.FindCircleTargets(caster, origin, radius, targetLayer, targets);

        for (int i = 0; i < targets.Count; i++)
        {
            CharacterCtrl target = targets[i];
            if (target == null || !CharacterSkillTargetUtility.IsInsideAngle(
                    caster.transform.position,
                    direction,
                    target.transform.position,
                    angle))
                continue;

            target.CharacterDamReceiver.ReceiveDamage(0f, caster.transform, statusData);
        }

        targets.Clear();
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
        forwardOffset = Mathf.Max(0f, forwardOffset);
        elementalPower = Mathf.Max(0f, elementalPower);
        statusDuration = Mathf.Max(0f, statusDuration);
    }
}
