using UnityEngine;

public class AnimationEventBridge : CharacterAbstract
{
    public void OnAttackHit()
    {
        CharacterCtrl.CharacterCombatController?.OnAttackHitAnimationEvent();
    }

    public void OnAttackEnd()
    {
        CharacterCtrl.CharacterCombatController?.EndAttack();
    }
}
