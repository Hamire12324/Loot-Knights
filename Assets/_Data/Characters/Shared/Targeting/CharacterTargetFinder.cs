using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterTargetFinder : BaseMonoBehaviour
{
    [SerializeField] protected LayerMask targetLayer;
    public LayerMask TargetLayer => targetLayer;
    [SerializeField] private float detectRadius = 3f;
    public float DetectRadius => Mathf.Max(0f, detectRadius);

    [SerializeField] private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;

    public virtual Transform FindClosestTarget()
    {
        return FindClosestTarget(detectRadius);
    }

    public virtual Transform FindClosestTarget(float searchRadius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0f, searchRadius),
            targetLayer
        );

        CharacterCtrl selfCtrl = GetComponentInParent<CharacterCtrl>();
        HashSet<CharacterCtrl> checkedTargets = new();
        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var h in hits)
        {
            CharacterCtrl targetCtrl = h.GetComponentInParent<CharacterCtrl>();
            if (targetCtrl == null) continue;
            if (targetCtrl == selfCtrl) continue;
            if (!checkedTargets.Add(targetCtrl)) continue;
            if (targetCtrl.CharacterDamReceiver == null || targetCtrl.CharacterDamReceiver.IsDead) continue;

            if (selfCtrl != null && !FactionManager.CanAttack(selfCtrl.Faction, targetCtrl.Faction)) continue;

            float d = Vector2.Distance(transform.position, targetCtrl.transform.position);

            if (d < minDist)
            {
                minDist = d;
                closest = targetCtrl.transform;
            }
        }

        return closest;
    }

    public virtual Transform FindClosestTargetAnywhere()
    {
        CharacterCtrl selfCtrl = GetComponentInParent<CharacterCtrl>();
        CharacterCtrl[] characters = FindObjectsByType<CharacterCtrl>(FindObjectsInactive.Exclude);
        Transform closest = null;
        float minDistanceSquared = Mathf.Infinity;

        foreach (CharacterCtrl targetCtrl in characters)
        {
            if (targetCtrl == null || targetCtrl == selfCtrl) continue;
            if (targetCtrl.CharacterDamReceiver == null || targetCtrl.CharacterDamReceiver.IsDead) continue;
            if (selfCtrl != null && !FactionManager.CanAttack(selfCtrl.Faction, targetCtrl.Faction)) continue;

            float distanceSquared = (targetCtrl.transform.position - transform.position).sqrMagnitude;
            if (distanceSquared >= minDistanceSquared) continue;

            minDistanceSquared = distanceSquared;
            closest = targetCtrl.transform;
        }

        return closest;
    }
    public void SetTarget(Transform t)
    {
        currentTarget = t;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.parent.position, detectRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.parent.position, transform.parent.position + Vector3.right * detectRadius);
    }
}
