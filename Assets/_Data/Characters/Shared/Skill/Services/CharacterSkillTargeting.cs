using UnityEngine;

public static class CharacterSkillTargeting
{
    private const float SkillTargetAcquireRadius = 8f;
    private static readonly bool DebugSkillTargeting = false;
    private const float DebugDrawDuration = 0.65f;

    public static Transform FindTarget(CharacterCtrl characterCtrl)
    {
        if (characterCtrl == null || characterCtrl.CharacterTargetFinder == null)
        {
            LogTargeting(characterCtrl, null, "No CharacterTargetFinder");
            return null;
        }

        CharacterTargetFinder targetFinder = characterCtrl.CharacterTargetFinder;
        Transform target = targetFinder.FindClosestTarget();
        string source = "normal";

        if (target == null && targetFinder.DetectRadius < SkillTargetAcquireRadius)
        {
            target = targetFinder.FindClosestTarget(SkillTargetAcquireRadius);
            source = "fallback";
        }

        targetFinder.SetTarget(target);
        LogTargeting(characterCtrl, target, source);
        return target;
    }

    public static Vector2 GetAimDirection(CharacterCtrl characterCtrl, Transform target)
    {
        if (characterCtrl == null)
            return Vector2.down;

        if (target != null)
        {
            Vector2 toTarget = target.position - characterCtrl.transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
                return toTarget.normalized;
        }

        if (characterCtrl.CharacterMovement != null &&
            characterCtrl.CharacterMovement.LookDirection != Vector2.zero)
        {
            return characterCtrl.CharacterMovement.LookDirection;
        }

        return Vector2.down;
    }

    private static void LogTargeting(CharacterCtrl characterCtrl, Transform target, string source)
    {
        if (!DebugSkillTargeting)
            return;

        string casterName = characterCtrl != null ? characterCtrl.name : "null";
        string targetName = target != null ? target.name : "null";
        CharacterTargetFinder targetFinder = characterCtrl != null ? characterCtrl.CharacterTargetFinder : null;
        float detectRadius = targetFinder != null ? targetFinder.DetectRadius : 0f;
        int targetLayer = targetFinder != null ? targetFinder.TargetLayer.value : 0;

        Debug.Log(
            $"{nameof(CharacterSkillTargeting)}: caster={casterName}, target={targetName}, source={source}, detectRadius={detectRadius:0.00}, fallbackRadius={SkillTargetAcquireRadius:0.00}, targetLayer={targetLayer}.",
            characterCtrl);

        if (characterCtrl == null)
            return;

        Vector3 start = characterCtrl.transform.position;
        if (target != null)
            Debug.DrawLine(start, target.position, Color.green, DebugDrawDuration);
        else
            Debug.DrawLine(start, start + Vector3.up * SkillTargetAcquireRadius, Color.red, DebugDrawDuration);
    }
}
