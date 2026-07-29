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
    private Vector2 baseBoxColliderSize;
    private bool hasBaseBoxColliderSize;

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
        int penetration,
        float widthScale = 1f)
    {
        this.caster = caster;
        this.targetLayer = targetLayer;
        this.damageData = damageData;
        this.flatBonusDamage = flatBonusDamage;
        this.multiplierBonus = multiplierBonus;
        remainingPenetration = Mathf.Max(0, penetration);
        configured = true;
        damagedTargets.Clear();
        ApplyWidthScale(widthScale);
    }

    private void ApplyWidthScale(float widthScale)
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            return;

        if (!hasBaseBoxColliderSize)
        {
            baseBoxColliderSize = boxCollider.size;
            hasBaseBoxColliderSize = true;
        }

        float safeWidthScale = Mathf.Max(0.1f, widthScale);
        boxCollider.size = new Vector2(baseBoxColliderSize.x, baseBoxColliderSize.y * safeWidthScale);
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

        receiver.ReceiveDamage(CalculateDamage(target), caster.transform, damageData);

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

    private float CalculateDamage(CharacterCtrl target)
    {
        if (caster == null || caster.CharacterStat == null)
            return 0f;

        float ambushMultiplierBonus = 0f;
        float ambushCritChanceBonus = 0f;
        ArcherAmbushStance.TryConsume(caster, out ambushMultiplierBonus, out ambushCritChanceBonus);

        float multiplier = (damageData != null ? damageData.Multiplier : 1f) + multiplierBonus;
        multiplier += IsBehindTarget(target) ? ambushMultiplierBonus * 1.5f : ambushMultiplierBonus;
        float damage = caster.CharacterStat.Attack.FinalValue * multiplier + flatBonusDamage;

        if (damageData != null &&
            damageData.CanCrit &&
            Random.value <= caster.CharacterStat.CritChance.FinalValue + ambushCritChanceBonus)
        {
            damage *= caster.CharacterStat.CritDamage.FinalValue;
        }

        return damage;
    }

    private bool IsBehindTarget(CharacterCtrl target)
    {
        if (caster == null || target == null || target.CharacterMovement == null)
            return false;

        Vector2 targetLookDirection = target.CharacterMovement.LookDirection;
        if (targetLookDirection == Vector2.zero)
            return false;

        Vector2 directionToCaster = caster.transform.position - target.transform.position;
        if (directionToCaster == Vector2.zero)
            return false;

        return Vector2.Dot(targetLookDirection.normalized, directionToCaster.normalized) < -0.45f;
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
