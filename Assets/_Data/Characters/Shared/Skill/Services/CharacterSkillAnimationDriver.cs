using UnityEngine;

public sealed class CharacterSkillAnimationDriver
{
    private const string AttackStateName = "Attack";
    private const string SkillIndexParameter = "SkillIndex";

    private readonly CharacterCtrl characterCtrl;
    private int suppressedAttackHitEvents;

    public CharacterSkillAnimationDriver(CharacterCtrl characterCtrl)
    {
        this.characterCtrl = characterCtrl;
    }

    public bool ConsumeAttackHitAnimationEvent()
    {
        if (suppressedAttackHitEvents <= 0)
            return false;

        suppressedAttackHitEvents--;
        return true;
    }

    public void PlayCastAnimation(CharacterSkillDefinition definition)
    {
        Animator animator = characterCtrl != null ? characterCtrl.Animator : null;
        if (animator == null || definition == null) return;

        if (HasAnimatorParameter(animator, SkillIndexParameter, AnimatorControllerParameterType.Int))
            animator.SetInteger(SkillIndexParameter, definition.AnimationIndex);

        if (!string.IsNullOrWhiteSpace(definition.TriggerName) &&
            HasAnimatorParameter(animator, definition.TriggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(definition.TriggerName);
        }
    }

    public bool PlaySkillAttackAnimation()
    {
        if (characterCtrl == null || characterCtrl.CharacterAnimation == null)
            return false;

        suppressedAttackHitEvents++;
        characterCtrl.CharacterAnimation.PlayAttackAnimation();
        return true;
    }

    public bool IsAttackVisualActive()
    {
        Animator animator = characterCtrl != null ? characterCtrl.Animator : null;
        if (animator == null) return false;

        if (!animator.IsInTransition(0))
            return animator.GetCurrentAnimatorStateInfo(0).IsName(AttackStateName);

        return animator.GetNextAnimatorStateInfo(0).IsName(AttackStateName);
    }

    private static bool HasAnimatorParameter(
        Animator animator,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
