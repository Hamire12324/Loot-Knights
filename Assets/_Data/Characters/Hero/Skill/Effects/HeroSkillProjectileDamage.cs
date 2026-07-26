using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HeroSkillProjectileDamage : MonoBehaviour
{
    private readonly HashSet<CharacterDamReceiver> damagedTargets = new();

    private CharacterCtrl caster;
    private LayerMask targetLayer;
    private DamageData damageData;
    private float flatBonusDamage;
    private float multiplierBonus;
    private int remainingPenetration;
    private bool configured;

    private void OnDisable()
    {
        Clear();
    }

    public void Configure(
        CharacterCtrl caster,
        LayerMask targetLayer,
        DamageData damageData,
        float flatBonusDamage,
        float multiplierBonus,
        int penetration)
    {
        this.caster = caster;
        this.targetLayer = targetLayer;
        this.damageData = damageData;
        this.flatBonusDamage = flatBonusDamage;
        this.multiplierBonus = multiplierBonus;
        remainingPenetration = Mathf.Max(0, penetration);
        configured = true;
        damagedTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!configured || other == null || caster == null)
            return;

        if (!IsInTargetLayer(other.gameObject.layer))
            return;

        CharacterCtrl target = other.GetComponentInParent<CharacterCtrl>();
        if (!IsValidTarget(other, target))
            return;

        CharacterDamReceiver receiver = target.CharacterDamReceiver;
        if (!damagedTargets.Add(receiver))
            return;

        receiver.ReceiveDamage(CalculateDamage(), caster.transform, damageData);

        if (remainingPenetration <= 0)
        {
            ReturnProjectile();
            return;
        }

        remainingPenetration--;
    }

    private bool IsInTargetLayer(int layer)
    {
        return targetLayer.value == 0 || (targetLayer.value & (1 << layer)) != 0;
    }

    private bool IsValidTarget(Collider2D hitCollider, CharacterCtrl target)
    {
        if (target == null || target == caster)
            return false;

        if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead)
            return false;

        if (!FactionManager.CanAttack(caster.Faction, target.Faction))
            return false;

        Collider2D targetCollider = target.Collider2D != null
            ? target.Collider2D
            : target.GetComponent<Collider2D>();

        return targetCollider == null || hitCollider == targetCollider;
    }

    private float CalculateDamage()
    {
        if (caster == null || caster.CharacterStat == null)
            return 0f;

        float multiplier = (damageData != null ? damageData.Multiplier : 1f) + multiplierBonus;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private void ReturnProjectile()
    {
        PoolObj poolObj = GetComponent<PoolObj>();
        if (poolObj != null)
            poolObj.ReturnToPool();
        else
            gameObject.SetActive(false);
    }

    private void Clear()
    {
        caster = null;
        targetLayer = default;
        damageData = null;
        flatBonusDamage = 0f;
        multiplierBonus = 0f;
        remainingPenetration = 0;
        configured = false;
        damagedTargets.Clear();
    }
}
