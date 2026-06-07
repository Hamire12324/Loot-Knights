using UnityEngine;

public class ButtonAttack : ButtonAbstract
{
    protected override void OnClick()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();

        if (hero == null) return;
        if (hero.CharacterCombatController == null) return;

        hero.CharacterCombatController.Attack();
    }
}
