using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CharacterDamSender : CharacterAbstract
{
    private const int MaxOverlapResults = 20;

    [Header("Damage Settings")]
    [SerializeField] protected DamageData hitDamage;

    [Header("Hitbox Settings")]
    [SerializeField] private Collider2D hitboxCollider;
    [SerializeField] protected LayerMask targetLayer;

    private readonly HashSet<CharacterDamReceiver> damagedTargets = new();
    private readonly Dictionary<CharacterDamReceiver, Coroutine> dotCoroutines = new();
    private readonly Collider2D[] overlapResults =
        new Collider2D[MaxOverlapResults];

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCollider2D();
    }

    protected virtual void LoadCollider2D()
    {
        if (hitboxCollider != null) return;

        hitboxCollider = GetComponent<Collider2D>();
        Debug.Log($"{transform.name}: LoadCollider2D", gameObject);
    }

    public virtual float DealHitboxDamage()
    {
        if (hitboxCollider == null || characterCtrl == null)
            return 0f;

        float totalDamageDealt = 0f;

        ContactFilter2D filter = new();
        filter.SetLayerMask(targetLayer);
        filter.useTriggers = true;

        int count = hitboxCollider.Overlap(filter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = overlapResults[i];

            CharacterCtrl targetCtrl =
                hitCollider.GetComponentInParent<CharacterCtrl>();

            if (!IsValidTarget(hitCollider, targetCtrl))
                continue;

            CharacterDamReceiver receiver =
                targetCtrl.CharacterDamReceiver;

            if (!damagedTargets.Add(receiver))
                continue;

            float healthBefore = receiver.CharacterCtrl.CharacterStat != null
                ? receiver.CharacterCtrl.CharacterStat.CurrentHealth
                : 0f;

            DealDamage(receiver);

            if (receiver.CharacterCtrl.CharacterStat != null)
                totalDamageDealt += Mathf.Max(0f, healthBefore - receiver.CharacterCtrl.CharacterStat.CurrentHealth);
        }

        return totalDamageDealt;
    }

    protected override void OnDisable()
    {
        DisableHitbox();
        StopAllCoroutines();
        dotCoroutines.Clear();
        damagedTargets.Clear();
        base.OnDisable();
    }

    protected virtual bool IsValidTarget(
        Collider2D hitCollider,
        CharacterCtrl targetCtrl)
    {
        if (targetCtrl == null || targetCtrl == characterCtrl)
            return false;

        CharacterDamReceiver receiver =
            targetCtrl.CharacterDamReceiver;

        if (receiver == null || receiver.IsDead)
            return false;

        if (!IsTargetBodyCollider(hitCollider, targetCtrl))
            return false;

        return FactionManager.CanAttack(
            characterCtrl.Faction,
            targetCtrl.Faction);
    }

    private bool IsTargetBodyCollider(
        Collider2D hitCollider,
        CharacterCtrl targetCtrl)
    {
        Collider2D bodyCollider = targetCtrl.Collider2D;

        if (bodyCollider == null)
            bodyCollider = targetCtrl.GetComponent<Collider2D>();

        return hitCollider == bodyCollider;
    }

    public virtual void DealDamage(CharacterDamReceiver target)
    {
        if (target == null || target.IsDead)
            return;

        target.ReceiveDamage(
            CalculateDamage(),
            transform,
            hitDamage
        );
    }

    public virtual void DealDamageOverTime(
        CharacterDamReceiver target,
        float totalDamage,
        float duration,
        int ticks = 5)
    {
        if (target == null || target.IsDead)
            return;

        ticks = Mathf.Max(1, ticks);
        duration = Mathf.Max(0f, duration);

        if (dotCoroutines.TryGetValue(target, out Coroutine existing))
        {
            StopCoroutine(existing);
            dotCoroutines.Remove(target);
        }

        Coroutine coroutine = StartCoroutine(
            DamageOverTimeCoroutine(
                target,
                totalDamage,
                duration,
                ticks
            )
        );

        dotCoroutines[target] = coroutine;
    }

    private IEnumerator DamageOverTimeCoroutine(
        CharacterDamReceiver target,
        float totalDamage,
        float duration,
        int ticks)
    {
        float damagePerTick = totalDamage / ticks;
        float interval = duration / ticks;

        for (int i = 0; i < ticks; i++)
        {
            if (target == null || target.IsDead)
            {
                dotCoroutines.Remove(target);
                yield break;
            }

            target.ReceiveDamage(damagePerTick);

            if (i < ticks - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        dotCoroutines.Remove(target);
    }

    protected virtual float CalculateDamage()
    {
        if (characterCtrl?.CharacterStat == null)
            return 0f;

        CharacterStat stats = characterCtrl.CharacterStat;

        float multiplier = hitDamage?.Multiplier ?? 1f;
        float damage = stats.Attack.FinalValue * multiplier;

        if (hitDamage != null &&
            hitDamage.CanCrit &&
            Random.value <= Mathf.Clamp01(stats.CritChance.FinalValue))
        {
            damage *= stats.CritDamage.FinalValue;
        }

        return damage;
    }

    public virtual void EnableHitbox()
    {
        damagedTargets.Clear();

        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
    }

    public virtual void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        damagedTargets.Clear();
    }

    public virtual void SetDamageData(DamageData data)
    {
        hitDamage = data;
    }
}
