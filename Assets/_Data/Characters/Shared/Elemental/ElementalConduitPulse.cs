using System.Collections.Generic;
using UnityEngine;

public struct ElementalConduitPulseRequest
{
    public CharacterSkillExecutionContext Context;
    public int Rank;
    public ElementType PrimaryElement;
    public float PrimaryPower;
    public int PrimaryStacks;
    public bool UsePrimer;
    public ElementType PrimerElement;
    public ElementalReactionType Reaction;
    public float PrimerPower;
    public int PrimerStacks;
    public DamageData PrimaryDamageData;
    public DamageData ReactionPrimerDamageData;
    public float PrimaryMultiplierPerRank;
    public float PrimerMultiplierPerRank;
    public float FlatBonusDamage;
    public float FlatBonusDamagePerRank;
    public float DamageBonusPerStack;
    public float StatusDuration;
    public float ReleaseRadius;
    public float Angle;
    public float ForwardOffset;
    public float SideOffset;
    public LayerMask TargetLayer;
    public VFXDefinition ImpactVfx;
    public SFXDefinition ImpactSfx;
    public bool UseImpactVfxColliderForDamage;
    public bool DebugHitArea;
    public bool DebugLogHits;
    public float DebugDrawDuration;
    public Color DebugHitColor;
    public Color DebugRejectedColor;
}

public static class ElementalConduitPulse
{
    private static readonly Collider2D[] Hits = new Collider2D[48];
    private static readonly HashSet<CharacterDamReceiver> DamagedTargets = new();

    public static void Release(ElementalConduitPulseRequest request)
    {
        CharacterCtrl caster = request.Context.Caster;
        if (caster == null || caster.CharacterStat == null)
            return;

        Vector2 direction = request.Context.AimDirection == Vector2.zero
            ? Vector2.down
            : request.Context.AimDirection.normalized;

        Vector2 casterPosition = caster.transform.position;
        Transform castTarget = request.Context.Target;
        CharacterCtrl focusedTarget = castTarget != null
            ? castTarget.GetComponentInParent<CharacterCtrl>()
            : null;
        Vector2 origin = request.Context.HasManualTargetPosition
            ? request.Context.ManualTargetPosition
            : castTarget != null
            ? (Vector2)castTarget.position
            : ElementalConduitTargeting.GetReleaseOrigin(
                casterPosition,
                direction,
                request.ForwardOffset,
                request.SideOffset);

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            useTriggers = true,
            layerMask = ElementalConduitTargeting.GetTargetLayer(caster, request.TargetLayer)
        };

        DamagedTargets.Clear();
        PoolObj impactVfx = PlayImpactFeedback(request, origin, direction, castTarget);
        Collider2D impactCollider = request.UseImpactVfxColliderForDamage
            ? FindDamageCollider(impactVfx)
            : null;

        if (request.UseImpactVfxColliderForDamage &&
            request.ImpactVfx != null &&
            impactCollider == null &&
            request.DebugLogHits)
        {
            Debug.LogWarning(
                $"{nameof(ElementalConduitPulse)}: Impact VFX '{request.ImpactVfx.name}' has no enabled Collider2D. Falling back to radius/angle damage.",
                caster);
        }

        int count = impactCollider != null
            ? impactCollider.Overlap(filter, Hits)
            : Physics2D.OverlapCircle(origin, request.ReleaseRadius, filter, Hits);

        bool usingVfxCollider = impactCollider != null;
        DrawDebugHitArea(request, casterPosition, origin, direction, count, impactCollider);

        int validTargetCount = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            if (!ElementalConduitTargeting.TryGetEnemyFromCollider(hit, caster, out CharacterCtrl target))
                continue;

            if (request.Reaction == ElementalReactionType.Overload &&
                focusedTarget != null &&
                target != focusedTarget)
            {
                continue;
            }

            if (!usingVfxCollider &&
                !ElementalConduitTargeting.IsInsideAngle(
                    casterPosition,
                    direction,
                    target.transform.position,
                    request.Angle))
            {
                continue;
            }

            if (!DamagedTargets.Add(target.CharacterDamReceiver))
                continue;

            validTargetCount++;
            DrawDebugTargetLine(request, origin, target.transform.position, true);

            if (request.UsePrimer && request.PrimerElement != ElementType.None)
            {
                DealElementalHit(
                    request,
                    caster,
                    target,
                    direction,
                    request.PrimerElement,
                    request.PrimerPower,
                    request.PrimerStacks,
                    request.ReactionPrimerDamageData,
                    request.PrimerMultiplierPerRank);
            }

            DealElementalHit(
                request,
                caster,
                target,
                direction,
            request.PrimaryElement,
            request.PrimaryPower,
            request.PrimaryStacks,
            request.PrimaryDamageData,
            request.PrimaryMultiplierPerRank);
        }

        if (request.DebugLogHits)
        {
            Debug.Log(
                $"{nameof(ElementalConduitPulse)} hit debug: mode={(usingVfxCollider ? $"VFX Collider ({impactCollider.name})" : "Radius/Angle")}, overlap={count}, validTargets={validTargetCount}, origin={origin}, radius={request.ReleaseRadius:0.00}, angle={request.Angle:0.0}.",
                caster);
        }

        DamagedTargets.Clear();
    }

    private static void DealElementalHit(
        ElementalConduitPulseRequest request,
        CharacterCtrl caster,
        CharacterCtrl target,
        Vector2 direction,
        ElementType element,
        float elementalPower,
        int stacks,
        DamageData damageTemplate,
        float multiplierPerRank)
    {
        if (element == ElementType.None || damageTemplate == null)
            return;

        DamageData damageData = damageTemplate.CloneWithElement(
            element,
            Mathf.Max(0f, elementalPower),
            request.StatusDuration,
            true);

        damageData.Multiplier = Mathf.Max(
            0f,
            damageData.Multiplier + Mathf.Max(0, request.Rank - 1) * multiplierPerRank);

        float flatBonus = request.FlatBonusDamage +
                          Mathf.Max(0, request.Rank - 1) * request.FlatBonusDamagePerRank;
        float stackMultiplier = Mathf.Max(0, stacks - 1) * Mathf.Max(0f, request.DamageBonusPerStack);
        float damage = CharacterSkillDamageUtility.CalculateDamage(
            caster,
            damageData,
            flatBonus,
            stackMultiplier);

        target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, damageData);
    }

    private static PoolObj PlayImpactFeedback(
        ElementalConduitPulseRequest request,
        Vector3 position,
        Vector2 direction,
        Transform target)
    {
        PoolObj spawnedVfx = null;
        if (request.ImpactVfx != null && VFXManager.HasInstance)
            spawnedVfx = VFXManager.InstanceOrNull.Play(request.ImpactVfx, position, direction, target);

        if (request.ImpactSfx != null)
            SFXManager.Play(request.ImpactSfx, position);

        return spawnedVfx;
    }

    private static Collider2D FindDamageCollider(PoolObj impactVfx)
    {
        if (impactVfx == null)
            return null;

        Collider2D[] colliders = impactVfx.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                return collider;
        }

        return null;
    }

    private static void DrawDebugHitArea(
        ElementalConduitPulseRequest request,
        Vector2 casterPosition,
        Vector2 origin,
        Vector2 direction,
        int overlapCount,
        Collider2D impactCollider)
    {
        if (!request.DebugHitArea)
            return;

        float duration = Mathf.Max(0.02f, request.DebugDrawDuration);
        Color color = request.DebugHitColor == default ? Color.cyan : request.DebugHitColor;
        Color rejectedColor = request.DebugRejectedColor == default ? Color.red : request.DebugRejectedColor;

        if (impactCollider != null)
        {
            DrawDebugBounds(impactCollider.bounds, color, duration);
            Debug.DrawLine(casterPosition, impactCollider.bounds.center, Color.yellow, duration);
        }
        else
        {
            DrawDebugCircle(origin, request.ReleaseRadius, color, duration);

            float halfAngle = request.Angle * 0.5f;
            Vector2 left = Quaternion.Euler(0f, 0f, halfAngle) * direction;
            Vector2 right = Quaternion.Euler(0f, 0f, -halfAngle) * direction;
            Debug.DrawLine(casterPosition, casterPosition + left * request.ReleaseRadius, color, duration);
            Debug.DrawLine(casterPosition, casterPosition + right * request.ReleaseRadius, color, duration);
            Debug.DrawLine(casterPosition, origin, Color.yellow, duration);
            Debug.DrawLine(origin, origin + direction * request.ReleaseRadius, color, duration);
        }

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null)
                continue;

            Vector2 hitPosition = hit.bounds.center;
            bool insideAngle = impactCollider != null ||
                               ElementalConduitTargeting.IsInsideAngle(
                                   casterPosition,
                                   direction,
                                   hitPosition,
                                   request.Angle);

            DrawDebugTargetLine(request, origin, hitPosition, insideAngle);
            if (!insideAngle)
                DrawDebugCircle(hitPosition, 0.15f, rejectedColor, duration);
        }
    }

    private static void DrawDebugTargetLine(
        ElementalConduitPulseRequest request,
        Vector2 origin,
        Vector2 targetPosition,
        bool accepted)
    {
        if (!request.DebugHitArea)
            return;

        float duration = Mathf.Max(0.02f, request.DebugDrawDuration);
        Color acceptedColor = request.DebugHitColor == default ? Color.cyan : request.DebugHitColor;
        Color rejectedColor = request.DebugRejectedColor == default ? Color.red : request.DebugRejectedColor;
        Debug.DrawLine(origin, targetPosition, accepted ? acceptedColor : rejectedColor, duration);
    }

    private static void DrawDebugCircle(Vector2 center, float radius, Color color, float duration)
    {
        const int segments = 40;
        float safeRadius = Mathf.Max(0f, radius);
        Vector3 previous = center + Vector2.right * safeRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * safeRadius;
            Debug.DrawLine(previous, next, color, duration);
            previous = next;
        }
    }

    private static void DrawDebugBounds(Bounds bounds, Color color, float duration)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 topLeft = new(min.x, max.y, bounds.center.z);
        Vector3 topRight = new(max.x, max.y, bounds.center.z);
        Vector3 bottomLeft = new(min.x, min.y, bounds.center.z);
        Vector3 bottomRight = new(max.x, min.y, bounds.center.z);

        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }
}
