using UnityEngine;

[CreateAssetMenu(fileName = "BatLifeStealBiteEffect", menuName = "Loot Knights/Enemy/Skill Effects/Bat Life Steal Bite")]
public sealed class BatLifeStealBiteEffect : CharacterSkillEffectDefinition
{
    [SerializeField, Range(0f, 1f)] private float lifeStealPercent = 0.5f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        CharacterDamSender damageSender = caster != null ? caster.CharacterDamSender : null;
        if (damageSender == null)
            return;

        damageSender.EnableHitbox();
        float damageDealt = damageSender.DealHitboxDamage();
        damageSender.DisableHitbox();

        if (damageDealt > 0f)
            caster.CharacterDamReceiver?.Heal(damageDealt * lifeStealPercent);
    }
}
