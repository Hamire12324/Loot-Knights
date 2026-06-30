using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillBasicAttackEffect", menuName = "Loot Knights/Character/Skill Effects/Basic Attack")]
public class CharacterSkillBasicAttackEffect : CharacterSkillEffectDefinition
{
    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterCombatController == null) return;

        caster.CharacterCombatController.Attack();
    }
}
