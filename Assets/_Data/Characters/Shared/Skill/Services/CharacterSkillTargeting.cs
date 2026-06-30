using UnityEngine;

public static class CharacterSkillTargeting
{
    public static Transform FindTarget(CharacterCtrl characterCtrl)
    {
        return characterCtrl != null && characterCtrl.CharacterTargetFinder != null
            ? characterCtrl.CharacterTargetFinder.FindClosestTarget()
            : null;
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
}
