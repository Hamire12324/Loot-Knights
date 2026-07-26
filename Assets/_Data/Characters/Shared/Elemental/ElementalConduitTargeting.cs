using UnityEngine;

public static class ElementalConduitTargeting
{
    public static int GetTargetLayer(CharacterCtrl caster, LayerMask overrideLayer)
    {
        if (overrideLayer.value != 0)
            return overrideLayer;

        return caster != null && caster.CharacterTargetFinder != null
            ? caster.CharacterTargetFinder.TargetLayer
            : Physics2D.DefaultRaycastLayers;
    }

    public static bool TryGetEnemyFromCollider(Collider2D hitCollider, CharacterCtrl caster, out CharacterCtrl target)
    {
        target = hitCollider != null ? hitCollider.GetComponentInParent<CharacterCtrl>() : null;
        if (target == null || target == caster)
            return false;

        if (!IsTargetBodyCollider(hitCollider, target))
            return false;

        if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead)
            return false;

        return caster == null || FactionManager.CanAttack(caster.Faction, target.Faction);
    }

    public static Vector2 GetReleaseOrigin(
        Vector2 heroPosition,
        Vector2 aimDirection,
        float forwardOffset,
        float sideOffset)
    {
        Vector2 right = new(-aimDirection.y, aimDirection.x);
        return heroPosition + aimDirection * forwardOffset + right * sideOffset;
    }

    public static bool IsInsideAngle(
        Vector2 casterPosition,
        Vector2 aimDirection,
        Vector2 targetPosition,
        float angle)
    {
        if (angle >= 359f)
            return true;

        Vector2 toTarget = targetPosition - casterPosition;
        if (toTarget.sqrMagnitude <= 0.001f)
            return true;

        return Vector2.Angle(aimDirection, toTarget.normalized) <= angle * 0.5f;
    }

    private static bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (hitCollider == null || target == null)
            return false;

        if (target.Collider2D != null)
            return hitCollider == target.Collider2D;

        Collider2D rootCollider = target.GetComponent<Collider2D>();
        return hitCollider == rootCollider;
    }
}
