using UnityEngine;

public class AnimationEventBridge : CharacterAbstract
{
    public void OnAttackHit()
    {
        if (CharacterCtrl.CharacterSkillController != null &&
            CharacterCtrl.CharacterSkillController.OnAttackHitAnimationEvent())
        {
            return;
        }

        CharacterCtrl.CharacterCombatController?.OnAttackHitAnimationEvent();
    }

    public void OnAttackEnd()
    {
        CharacterCtrl.CharacterCombatController?.EndAttack();
    }
}
