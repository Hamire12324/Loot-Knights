using UnityEngine;

public class HeroAnimation : CharacterAnimation
{
    protected override void Update()
    {
        Vector2 moveInput = characterCtrl.CharacterMovement.MoveInput;
        Vector2 lookDir = characterCtrl.CharacterMovement.LookDirection;

        characterCtrl.Animator.SetFloat("MoveX", lookDir.x);
        characterCtrl.Animator.SetFloat("MoveY", lookDir.y);
        characterCtrl.Animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }
}