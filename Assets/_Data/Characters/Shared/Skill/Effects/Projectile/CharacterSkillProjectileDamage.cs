using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterSkillProjectileDamage : MonoBehaviour
{
    private readonly HashSet<CharacterDamReceiver> damagedTargets = new();

    private CharacterCtrl caster;
    private LayerMask targetLayer;
    private DamageData damageData;
    private float flatBonusDamage;
    private float multiplierBonus;
    private int remainingPenetration;
    private bool returnToPoolOnFinalHit;
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
        int penetration,
        bool returnToPoolOnFinalHit,
        DamageData effectiveDamageData = null)
    {
        EnsurePhysicsBody();
        this.caster = caster;
        this.targetLayer = targetLayer;
        this.damageData = effectiveDamageData ?? damageData;
        this.flatBonusDamage = flatBonusDamage;
        this.multiplierBonus = multiplierBonus;
        remainingPenetration = Mathf.Max(0, penetration);
        this.returnToPoolOnFinalHit = returnToPoolOnFinalHit;
        configured = true;
        damagedTargets.Clear();
    }

    private void EnsurePhysicsBody()
    {
        Collider2D hitbox = GetComponent<Collider2D>();
        if (hitbox == null)
            hitbox = gameObject.AddComponent<CircleCollider2D>();

        hitbox.isTrigger = true;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!configured || other == null)
            return;

        int layerMask = CharacterSkillTargetUtility.GetTargetLayerMask(caster, targetLayer);
        if ((layerMask & (1 << other.gameObject.layer)) == 0)
            return;

        CharacterCtrl target = other.GetComponentInParent<CharacterCtrl>();
        if (!CharacterSkillTargetUtility.IsValidTarget(caster, other, target))
            return;

        if (!damagedTargets.Add(target.CharacterDamReceiver))
            return;

        CharacterSkillDamageUtility.DealDamage(caster, target, damageData, flatBonusDamage, multiplierBonus);

        if (remainingPenetration <= 0 && returnToPoolOnFinalHit)
        {
            ReturnProjectile();
            return;
        }

        remainingPenetration--;
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
        returnToPoolOnFinalHit = true;
        configured = false;
        damagedTargets.Clear();
    }
}
