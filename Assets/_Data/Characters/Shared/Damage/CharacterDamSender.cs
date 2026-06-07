using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CharacterDamSender : CharacterAbstract
{
    [Header("Damage Settings")]
    [SerializeField] protected DamageData hitDamage;

    [Header("Hitbox Settings")]
    [SerializeField] private Collider2D hitboxCollider;
    [SerializeField] protected LayerMask targetLayer;

    [Header("Runtime")]
    [SerializeField, HideInInspector] 
    private HashSet<CharacterDamReceiver> damagedTargets = new();

    private Dictionary<CharacterDamReceiver, Coroutine> dotCoroutines = new();
    protected override void LoadComponents()
    {
        base.LoadComponents();

        this.LoadCollider2D();
    }
    protected virtual void LoadCollider2D()
    {
        if (this.hitboxCollider != null) return;
        this.hitboxCollider = GetComponent<Collider2D>();
        Debug.Log(transform.name + ": LoadCollider2D", gameObject);
    }
    public void DealHitboxDamage()
    {
        if (hitboxCollider == null) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useTriggers = true;

        Collider2D[] results = new Collider2D[20];
        int count = hitboxCollider.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = results[i];
            CharacterCtrl targetCtrl = hitCollider.GetComponentInParent<CharacterCtrl>();
            if (targetCtrl == null) continue;
            if (targetCtrl == characterCtrl) continue;
            if (!IsTargetBodyCollider(hitCollider, targetCtrl)) continue;
            if (targetCtrl.CharacterDamReceiver == null || targetCtrl.CharacterDamReceiver.IsDead) continue;

            CharacterDamReceiver receiver = targetCtrl.CharacterDamReceiver;
            if (CharacterCtrl == null || targetCtrl.Faction == characterCtrl.Faction) continue;
            if (!FactionManager.CanAttack(CharacterCtrl.Faction, targetCtrl.Faction)) continue;

            if (!damagedTargets.Add(receiver)) continue;

            this.DealDamage(receiver);
        }
    }
    private bool IsTargetBodyCollider(Collider2D hitCollider, CharacterCtrl targetCtrl)
    {
        if (hitCollider == null || targetCtrl == null) return false;

        Collider2D targetCollider = targetCtrl.Collider2D;
        if (targetCollider == null)
            targetCollider = targetCtrl.GetComponent<Collider2D>();

        return hitCollider == targetCollider;
    }
    public virtual void DealDamage(CharacterDamReceiver target)
    {
        if (target == null || target.IsDead) return;

        float finalDamage = CalculateDamage();
        target.ReceiveDamage(finalDamage, this.transform, hitDamage);

    }
    public virtual void DealDamageOverTime(CharacterDamReceiver target, float totalDamage, float duration, int ticks = 5)
    {
        if (target == null || target.IsDead) return;

        if (dotCoroutines.TryGetValue(target, out var existing))
            StopCoroutine(existing);

        Coroutine c = StartCoroutine(DamageOverTimeCoroutine(target, totalDamage, duration, ticks));
        dotCoroutines[target] = c;
    }
    private IEnumerator DamageOverTimeCoroutine(CharacterDamReceiver target, float totalDamage, float duration, int ticks)
    {
        float damagePerTick = totalDamage / ticks;
        float interval = duration / ticks;

        for (int i = 0; i < ticks; i++)
        {
            if (target == null || target.IsDead) yield break;

            target.ReceiveDamage(damagePerTick);
            yield return new WaitForSeconds(interval);
        }

        dotCoroutines.Remove(target);
    }
    protected virtual float CalculateDamage()
    {
        if (characterCtrl == null || characterCtrl.CharacterStat == null)
            return 0f;

        var stats = characterCtrl.CharacterStat;

        float damageMultiplier = hitDamage != null ? hitDamage.Multiplier : 1f;
        float damage = stats.Attack.FinalValue * damageMultiplier;

        if (hitDamage != null &&
            hitDamage.CanCrit &&
            Random.value <= stats.CritChance.FinalValue)
        {
            damage *= stats.CritDamage.FinalValue;
        }

        return damage;
    }
    public void EnableHitbox()
    {
        damagedTargets.Clear();
        if (hitboxCollider != null) hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        damagedTargets.Clear();
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }
    public void SetDamageData(DamageData data) => hitDamage = data;
}
