using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillHealEffect", menuName = "Loot Knights/Character/Skill Effects/Heal")]
public sealed class CharacterSkillHealEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private bool healCaster = true;
    [SerializeField, Min(0f)] private float flatAmount;
    [SerializeField, Min(0f)] private float maxHealthPercent = 0.1f;
    [SerializeField] private VFXDefinition healVfx;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl target = healCaster
            ? context.Caster
            : context.Target != null
                ? context.Target.GetComponentInParent<CharacterCtrl>()
                : null;

        if (target == null || target.CharacterDamReceiver == null)
            return;

        float maxHealth = target.CharacterStat != null && target.CharacterStat.MaxHealth != null
            ? target.CharacterStat.MaxHealth.FinalValue
            : 0f;

        float amount = flatAmount + maxHealth * maxHealthPercent;
        if (amount <= 0f)
            return;

        target.CharacterDamReceiver.Heal(amount);
        CharacterSkillVfxUtility.Play(healVfx, target.transform.position, context.AimDirection, target.transform);
    }
}
