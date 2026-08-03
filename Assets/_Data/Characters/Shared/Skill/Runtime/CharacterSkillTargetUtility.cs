using System.Collections.Generic;
using UnityEngine;

public static class CharacterSkillTargetUtility
{
    private static readonly Collider2D[] Hits = new Collider2D[64];

    public static int GetTargetLayerMask(CharacterCtrl caster, LayerMask overrideLayer)
    {
        if (overrideLayer.value != 0)
            return overrideLayer.value;

        return caster != null && caster.CharacterTargetFinder != null
            ? caster.CharacterTargetFinder.TargetLayer
            : Physics2D.DefaultRaycastLayers;
    }

    public static ContactFilter2D CreateTargetFilter(CharacterCtrl caster, LayerMask overrideLayer)
    {
        return new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true,
            layerMask = GetTargetLayerMask(caster, overrideLayer)
        };
    }

    public static void FindCircleTargets(
        CharacterCtrl caster,
        Vector2 center,
        float radius,
        LayerMask targetLayer,
        ICollection<CharacterCtrl> results)
    {
        if (results == null)
            return;

        ContactFilter2D filter = CreateTargetFilter(caster, targetLayer);
        int count = Physics2D.OverlapCircle(center, Mathf.Max(0.05f, radius), filter, Hits);
        AddValidTargets(caster, count, results);
    }

    public static void FindBoxTargets(
        CharacterCtrl caster,
        Vector2 center,
        Vector2 size,
        float angle,
        LayerMask targetLayer,
        ICollection<CharacterCtrl> results)
    {
        if (results == null)
            return;

        ContactFilter2D filter = CreateTargetFilter(caster, targetLayer);
        int count = Physics2D.OverlapBox(center, size, angle, filter, Hits);
        AddValidTargets(caster, count, results);
    }

    public static bool IsValidTarget(CharacterCtrl caster, Collider2D hitCollider, CharacterCtrl target)
    {
        if (target == null || target == caster)
            return false;

        if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead)
            return false;

        if (caster != null && !FactionManager.CanAttack(caster.Faction, target.Faction))
            return false;

        return IsCharacterBodyCollider(hitCollider, target);
    }

    public static bool IsInsideAngle(
        Vector2 origin,
        Vector2 direction,
        Vector2 targetPosition,
        float angle)
    {
        if (angle >= 359f)
            return true;

        Vector2 toTarget = targetPosition - origin;
        if (toTarget.sqrMagnitude <= 0.001f)
            return true;

        return Vector2.Angle(direction.normalized, toTarget.normalized) <= angle * 0.5f;
    }

    public static CharacterCtrl FindClosestTarget(
        CharacterCtrl caster,
        Vector2 center,
        float radius,
        LayerMask targetLayer)
    {
        ContactFilter2D filter = CreateTargetFilter(caster, targetLayer);
        int count = Physics2D.OverlapCircle(center, Mathf.Max(0.05f, radius), filter, Hits);
        CharacterCtrl closest = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            Hits[i] = null;
            CharacterCtrl target = hit != null ? hit.GetComponentInParent<CharacterCtrl>() : null;
            if (!IsValidTarget(caster, hit, target))
                continue;

            float distanceSqr = ((Vector2)target.transform.position - center).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closest = target;
            closestDistanceSqr = distanceSqr;
        }

        return closest;
    }

    private static void AddValidTargets(
        CharacterCtrl caster,
        int count,
        ICollection<CharacterCtrl> results)
    {
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            Hits[i] = null;

            CharacterCtrl target = hit != null ? hit.GetComponentInParent<CharacterCtrl>() : null;
            if (IsValidTarget(caster, hit, target) && !results.Contains(target))
                results.Add(target);
        }
    }

    private static bool IsCharacterBodyCollider(Collider2D hitCollider, CharacterCtrl target)
    {
        if (hitCollider == null || target == null)
            return false;

        if (target.Collider2D != null)
            return hitCollider == target.Collider2D;

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        return targetCollider == null || hitCollider == targetCollider;
    }
}
