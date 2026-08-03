using UnityEngine;

/// <summary>
/// Bone Guard: attacks originating in front of the skeleton are mitigated before armor is applied.
/// </summary>
public sealed class ArmoredSkeletonDamReceiver : EnemyDamReceiver
{
    [Header("Bone Guard")]
    [SerializeField, Range(0f, 1f)] private float frontDamageMultiplier = 0.6f;
    [SerializeField, Range(1f, 180f)] private float guardAngle = 120f;

    public override void ReceiveDamage(float damage, Transform attacker = null, DamageData damageData = null)
    {
        if (attacker != null && IsAttackerInFront(attacker.position))
            damage *= frontDamageMultiplier;

        base.ReceiveDamage(damage, attacker, damageData);
    }

    private bool IsAttackerInFront(Vector3 attackerPosition)
    {
        if (CharacterCtrl == null || CharacterCtrl.CharacterMovement == null)
            return false;

        Vector2 toAttacker = (Vector2)(attackerPosition - CharacterCtrl.transform.position);
        if (toAttacker.sqrMagnitude <= 0.0001f)
            return true;

        return Vector2.Angle(CharacterCtrl.CharacterMovement.LookDirection, toAttacker) <= guardAngle * 0.5f;
    }
}
