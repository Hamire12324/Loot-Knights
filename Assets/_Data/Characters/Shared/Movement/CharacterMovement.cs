using UnityEngine;

public class CharacterMovement : CharacterAbstract
{
    private const float MinSpeedMultiplier = 0.1f;

    [SerializeField] protected float moveSpeed = 5f;

    protected Vector2 moveInput;
    public Vector2 MoveInput => moveInput;

    protected Vector2 lookDirection = Vector2.down;
    public Vector2 LookDirection => lookDirection;

    public virtual void SetLookDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        lookDirection = dir.normalized;
    }

    protected float GetMoveSpeed()
    {
        StatValue moveSpeedStat = characterCtrl != null && characterCtrl.CharacterStat != null
            ? characterCtrl.CharacterStat.GetStat(StatType.MoveSpeed)
            : null;

        float multiplier = 1f + (moveSpeedStat != null ? moveSpeedStat.FinalValue : 0f);
        return moveSpeed * Mathf.Max(MinSpeedMultiplier, multiplier);
    }
}
