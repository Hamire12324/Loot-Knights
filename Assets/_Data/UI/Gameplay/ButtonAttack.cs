using UnityEngine;

public class ButtonAttack : ButtonAbstract
{
    protected override void OnClick()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();

        if (hero == null)
        {
            Debug.LogWarning($"{nameof(ButtonAttack)} clicked but no local hero was found.", gameObject);
            return;
        }

        CharacterSkillController skillController = hero.CharacterSkillController;
        string basicSkillName = skillController != null &&
            skillController.BasicAttackRuntime != null &&
            skillController.BasicAttackRuntime.Definition != null
                ? skillController.BasicAttackRuntime.Definition.name
                : "null";


        if (skillController != null && skillController.TryCastBasicAttack()) return;

        hero.CharacterCombatController?.Attack();
    }
}
