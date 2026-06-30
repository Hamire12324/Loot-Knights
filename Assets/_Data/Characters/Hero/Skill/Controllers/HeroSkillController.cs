using UnityEngine;

public class HeroSkillController : CharacterSkillController
{
    public HeroCtrl Hero => characterCtrl as HeroCtrl;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<HeroCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no HeroCtrl in {gameObject.name}", gameObject);
    }

    protected override void SetMovementLocked(bool locked)
    {
        base.SetMovementLocked(locked);

        HeroMovement movement = characterCtrl != null ? characterCtrl.CharacterMovement as HeroMovement : null;
        movement?.SetInputEnabled(!locked);
    }

    public void OnSkill1() => TryCast(0);
    public void OnSkill2() => TryCast(1);
    public void OnSkill3() => TryCast(2);
    public void OnSkill4() => TryCast(3);
}
