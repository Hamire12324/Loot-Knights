using UnityEngine;

/// <summary>Armor plating is strongest while the orc visibly raises its guard.</summary>
public sealed class ArmoredOrcDamReceiver : EnemyDamReceiver
{
    private ArmoredOrcSkillController skillController;

    public override void ReceiveDamage(float damage, Transform attacker = null, DamageData damageData = null)
    {
        if (skillController == null)
            skillController = GetComponentInParent<ArmoredOrcSkillController>();

        if (skillController != null)
        {
            if (skillController.IsCharging)
                damage *= skillController.ChargeDamageMultiplier;
        }

        base.ReceiveDamage(damage, attacker, damageData);
    }
}
