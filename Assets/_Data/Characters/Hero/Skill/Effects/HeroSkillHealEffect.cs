using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillHealEffect", menuName = "Loot Knights/Hero/Skill Effects/Heal")]
public class HeroSkillHealEffect : CharacterSkillEffectDefinition
{
    [SerializeField, Min(0f)] private float flatHeal = 25f;
    [SerializeField, Min(0f)] private float maxHealthPercent;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterDamReceiver == null || caster.CharacterStat == null) return;

        float maxHealth = caster.CharacterStat.MaxHealth != null
            ? caster.CharacterStat.MaxHealth.FinalValue
            : 0f;

        caster.CharacterDamReceiver.Heal(flatHeal + maxHealth * maxHealthPercent);
    }
}
