using UnityEngine;

public class EnemyAnimation : CharacterAnimation
{
    private Vector3 originalScale;

    protected override void Awake()
    {
        base.Awake();

        originalScale = characterCtrl.transform.localScale;
    }

    protected override void Update()
    {
        Vector2 moveInput = characterCtrl.CharacterMovement.MoveInput;
        Vector2 lookDir = characterCtrl.CharacterMovement.LookDirection;

        characterCtrl.Animator.SetFloat("Speed", moveInput.sqrMagnitude);

        FlipByLookDirection(lookDir);
    }

    private void FlipByLookDirection(Vector2 lookDir)
    {
        if (Mathf.Abs(lookDir.x) <= 0.01f) return;

        Vector3 scale = originalScale;
        scale.x = lookDir.x >= 0f
            ? Mathf.Abs(originalScale.x)
            : -Mathf.Abs(originalScale.x);

        characterCtrl.transform.localScale = scale;
    }
}