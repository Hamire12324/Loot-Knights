using UnityEngine;

public class CharacterAnimation : CharacterAbstract
{
    private const string AttackTrigger = "Attack";
    private const string HurtTrigger = "Hurt";
    private const string DeathTrigger = "Death";

    protected Vector2 lastMove;

    public virtual void PlayAttackAnimation()
    {
        characterCtrl.Animator.SetTrigger(AttackTrigger);
    }

    public virtual void PlayHurt()
    {
        characterCtrl.Animator.SetTrigger(HurtTrigger);
    }

    public virtual void PlayDeath()
    {
        characterCtrl.Animator.SetTrigger(DeathTrigger);
    }

    public virtual void ResetAfterRevive()
    {
        Animator animator = characterCtrl != null ? characterCtrl.Animator : null;
        if (animator == null) return;

        ResetTriggerIfExists(animator, AttackTrigger);
        ResetTriggerIfExists(animator, HurtTrigger);
        ResetTriggerIfExists(animator, DeathTrigger);

        animator.Rebind();
        if (animator.isActiveAndEnabled && animator.gameObject.activeInHierarchy)
            animator.Update(0f);
    }

    private static void ResetTriggerIfExists(Animator animator, string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger) continue;
            if (parameter.name != triggerName) continue;

            animator.ResetTrigger(triggerName);
            return;
        }
    }
}
