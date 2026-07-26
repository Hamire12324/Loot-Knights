using UnityEngine;
using UnityEngine.InputSystem;

public class HeroMovement : CharacterMovement
{
    private bool inputEnabled = true;

    protected override void FixedUpdate()
    {
        if (characterCtrl.CharacterDamReceiver != null &&
            characterCtrl.CharacterDamReceiver.IsHitStunned)
        {
            characterCtrl.Rb.linearVelocity = Vector2.zero;
            return;
        }

        characterCtrl.Rb.linearVelocity = moveInput * GetMoveSpeed();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        moveInput = Vector2.zero;

        if (characterCtrl != null && characterCtrl.Rb != null)
            characterCtrl.Rb.linearVelocity = Vector2.zero;
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>().normalized;

        if (moveInput != Vector2.zero)
        {
            lookDirection = moveInput;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
            moveInput = Vector2.zero;
    }

}
