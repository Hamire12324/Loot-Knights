using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillReturningProjectileEffect", menuName = "Loot Knights/Character/Skill Effects/Returning Projectile")]
public sealed class CharacterSkillReturningProjectileEffect : CharacterSkillEffectDefinition
{
    [Header("Projectile")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0.05f)] private float distance = 7f;
    [SerializeField, Min(0f)] private float startOffset = 0.55f;
    [SerializeField, Min(0.05f)] private float speed = 8f;
    [SerializeField, Min(0f)] private float returnDelay = 0.5f;
    [SerializeField] private float rotationOffset;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField, Min(0.05f)] private float hitRadius = 0.45f;

    [Header("Damage")]
    [SerializeField] private DamageData outboundDamage = new(1f, true);
    [SerializeField] private DamageData returnDamage = new(1.15f, true);
    [SerializeField] private float flatBonusDamage;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        context.Controller.StartCoroutine(TravelRoutine(context.Caster, context.AimDirection));
    }

    private IEnumerator TravelRoutine(CharacterCtrl caster, Vector2 direction)
    {
        Vector2 normalizedDirection = direction == Vector2.zero ? Vector2.down : direction.normalized;
        Vector3 start = caster.transform.position + (Vector3)(normalizedDirection * startOffset);
        Vector3 outwardEnd = start + (Vector3)(normalizedDirection * distance);
        PoolObj visual = CharacterSkillVfxUtility.Play(projectileVfx, start, normalizedDirection);
        Transform mover = visual != null ? visual.transform : null;
        HashSet<CharacterCtrl> outboundHits = new();
        HashSet<CharacterCtrl> returnHits = new();

        yield return TravelSegment(caster, mover, start, outwardEnd, outboundDamage, outboundHits);
        if (returnDelay > 0f)
            yield return new WaitForSeconds(returnDelay);

        yield return TravelSegment(caster, mover, outwardEnd, caster.transform.position, returnDamage, returnHits);

        if (visual != null)
            visual.ReturnToPool();
    }

    private IEnumerator TravelSegment(
        CharacterCtrl caster,
        Transform mover,
        Vector3 start,
        Vector3 end,
        DamageData damageData,
        HashSet<CharacterCtrl> hitTargets)
    {
        float length = Vector3.Distance(start, end);
        float elapsed = 0f;
        float travelDuration = length / Mathf.Max(0.05f, speed);

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / travelDuration);
            Vector3 position = Vector3.Lerp(start, end, progress);
            Vector2 movementDirection = ((Vector2)(end - start)).normalized;

            if (mover != null)
            {
                mover.position = position;
                float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
                mover.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
            }

            List<CharacterCtrl> targets = new();
            CharacterSkillTargetUtility.FindCircleTargets(caster, position, hitRadius, targetLayer, targets);
            for (int i = 0; i < targets.Count; i++)
            {
                CharacterCtrl target = targets[i];
                if (target != null && hitTargets.Add(target))
                    CharacterSkillDamageUtility.DealDamage(caster, target, damageData, flatBonusDamage);
            }

            yield return null;
        }
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0.05f, distance);
        startOffset = Mathf.Max(0f, startOffset);
        speed = Mathf.Max(0.05f, speed);
        returnDelay = Mathf.Max(0f, returnDelay);
        hitRadius = Mathf.Max(0.05f, hitRadius);
        outboundDamage ??= new DamageData(1f, true);
        returnDamage ??= new DamageData(1.15f, true);
    }
}
