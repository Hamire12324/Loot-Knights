using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillBasicAttackEffect", menuName = "Loot Knights/Character/Skill Effects/Basic Attack")]
public class CharacterSkillBasicAttackEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private bool dealHitboxDamageAtCastTime;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterCombatController == null) return;

        if (!dealHitboxDamageAtCastTime)
        {
            caster.CharacterCombatController.Attack();
            return;
        }

        CharacterDamSender damageSender = caster.CharacterDamSender;
        if (damageSender == null) return;

        damageSender.EnableHitbox();
        damageSender.DealHitboxDamage();
        damageSender.DisableHitbox();
    }
}
