using UnityEngine;

public class CharacterMovement : CharacterAbstract
{
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
}