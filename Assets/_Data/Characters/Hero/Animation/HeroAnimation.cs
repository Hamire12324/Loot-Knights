using UnityEngine;

public class HeroAnimation : CharacterAnimation
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

        if (Mathf.Abs(moveInput.x) > 0.01f)
            FlipByHorizontalDirection(moveInput.x);

        characterCtrl.Animator.SetFloat("MoveX", 1f);
        characterCtrl.Animator.SetFloat("MoveY", 0f);
        characterCtrl.Animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    private void FlipByHorizontalDirection(float horizontalDirection)
    {
        Vector3 scale = originalScale;
        scale.x = horizontalDirection >= 0f
            ? Mathf.Abs(originalScale.x)
            : -Mathf.Abs(originalScale.x);

        characterCtrl.transform.localScale = scale;
    }
}
