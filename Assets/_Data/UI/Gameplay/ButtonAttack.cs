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

        Debug.Log(
            $"{nameof(ButtonAttack)} clicked. hero={hero.name}, class={(hero.Profile != null ? hero.Profile.CharacterClass.ToString() : "no-profile")}, skillController={(skillController != null ? skillController.name : "null")}, basicSkill={basicSkillName}.",
            hero);

        if (skillController != null && skillController.TryCastBasicAttack())
        {
            Debug.Log($"{nameof(ButtonAttack)} cast basic skill {basicSkillName}.", hero);
            return;
        }

        Debug.LogWarning(
            $"{nameof(ButtonAttack)} basic skill did not cast, falling back to CharacterCombatController.Attack(). basicSkill={basicSkillName}, isCasting={(skillController != null && skillController.IsCasting)}.",
            hero);
        hero.CharacterCombatController?.Attack();
    }
}
