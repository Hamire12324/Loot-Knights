using UnityEngine;

public class CharacterAnimation : CharacterAbstract
{
    protected Vector2 lastMove;
    public virtual void PlayAttackAnimation()
    {
        characterCtrl.Animator.SetTrigger("Attack");
    }
    public virtual void PlayHurt()
    {
        characterCtrl.Animator.SetTrigger("Hurt");
    }
    public virtual void PlayDeath()
    {
        characterCtrl.Animator.SetTrigger("Death");
    }
}