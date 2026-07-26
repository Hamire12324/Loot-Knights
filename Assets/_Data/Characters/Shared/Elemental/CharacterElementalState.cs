using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterElementalState : CharacterAbstract
{
    [Serializable]
    private sealed class ElementalStatus
    {
        public ElementType Element;
        public float Power;
        public float EndTime;

        public bool IsExpired => Time.time >= EndTime;
    }

    private const float MinStatusDuration = 0.1f;
    private static readonly Collider2D[] OverloadHits = new Collider2D[24];

    [Header("Reaction Tuning")]
    [SerializeField, Min(0f)] private float shatterBonusDamageMultiplier = 0.65f;
    [SerializeField, Min(0f)] private float overloadBonusDamageMultiplier = 0.45f;
    [SerializeField, Min(1)] private int overloadMaxHits = 3;
    [SerializeField, Min(0f)] private float overloadChainRadius = 2f;
    [SerializeField, Min(0.01f)] private float overloadHitInterval = 0.12f;
    [SerializeField, Min(0f)] private float overloadHitStunDuration = 0.35f;
    [SerializeField, Min(0f)] private float superconductArmorReduction = 0.25f;
    [SerializeField, Min(0f)] private float superconductDuration = 3f;
    [SerializeField, Min(0f)] private float burnoutTickMultiplier = 0.2f;
    [SerializeField, Min(0.05f)] private float burnoutTickInterval = 0.5f;
    [SerializeField, Min(0f)] private float burnoutDuration = 3f;
    [SerializeField, Min(0f)] private float neuroshockDamageMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float neuroshockStunDuration = 0.45f;
    [SerializeField, Min(0f)] private float brittleToxinInitialArmorReduction = 0.15f;
    [SerializeField, Min(0f)] private float brittleToxinRampArmorReduction = 0.3f;
    [SerializeField, Min(0f)] private float brittleToxinDuration = 4f;

    private readonly List<ElementalStatus> statuses = new();
    private Coroutine burnoutRoutine;
    private Coroutine overloadRoutine;
    private Coroutine brittleToxinRoutine;

    public static void ApplyElementalHit(
        CharacterCtrl target,
        CharacterCtrl attacker,
        float finalDamage,
        DamageData damageData)
    {
        if (target == null || damageData == null || damageData.Element == ElementType.None)
            return;

        CharacterElementalState state = target.GetComponentInChildren<CharacterElementalState>();
        if (state == null)
            state = target.gameObject.AddComponent<CharacterElementalState>();

        state.ApplyHit(attacker, finalDamage, damageData);
    }

    public bool HasStatus(ElementType element)
    {
        RemoveExpiredStatuses();
        return statuses.Exists(status => status.Element == element);
    }

    public bool TryGetStrongestStatus(out ElementType element, out float power)
    {
        RemoveExpiredStatuses();

        ElementalStatus strongest = null;
        foreach (ElementalStatus status in statuses)
        {
            if (status == null || status.Element == ElementType.None)
                continue;

            if (strongest == null ||
                status.Power > strongest.Power ||
                Mathf.Approximately(status.Power, strongest.Power) && status.EndTime > strongest.EndTime)
            {
                strongest = status;
            }
        }

        if (strongest == null)
        {
            element = ElementType.None;
            power = 0f;
            return false;
        }

        element = strongest.Element;
        power = strongest.Power;
        return true;
    }

    private void ApplyHit(CharacterCtrl attacker, float finalDamage, DamageData damageData)
    {
        RemoveExpiredStatuses();

        ElementType incomingElement = damageData.Element;
        ElementalStatus existing = statuses.Find(status => status.Element != incomingElement);

        if (existing != null)
        {
            ResolveReaction(existing.Element, incomingElement, attacker, finalDamage, damageData.ElementalPower);

            if (damageData.ConsumesElementOnReaction)
                statuses.Remove(existing);
        }

        AddOrRefreshStatus(
            incomingElement,
            damageData.ElementalPower,
            damageData.ElementalStatusDuration);
    }

    private void AddOrRefreshStatus(ElementType element, float power, float duration)
    {
        if (element == ElementType.None) return;

        ElementalStatus status = statuses.Find(item => item.Element == element);
        if (status == null)
        {
            status = new ElementalStatus { Element = element };
            statuses.Add(status);
        }

        status.Power = Mathf.Max(status.Power, power);
        status.EndTime = Time.time + Mathf.Max(MinStatusDuration, duration);
    }

    private void ResolveReaction(
        ElementType existing,
        ElementType incoming,
        CharacterCtrl attacker,
        float finalDamage,
        float incomingPower)
    {
        ElementalReactionType reaction = GetReaction(existing, incoming);
        if (reaction == ElementalReactionType.None) return;

        float baseReactionDamage = Mathf.Max(1f, finalDamage) * Mathf.Max(0f, incomingPower);

        switch (reaction)
        {
            case ElementalReactionType.Shatter:
                float shatterDamage = baseReactionDamage * shatterBonusDamageMultiplier;
                DealReactionDamage(attacker, shatterDamage);
                break;

            case ElementalReactionType.Overload:
                float overloadDamage = baseReactionDamage * overloadBonusDamageMultiplier;
                float overloadHoldDuration = GetOverloadHoldDuration();
                ApplyHitStun(attacker, overloadHoldDuration);
                StartOverloadChain(attacker, overloadDamage);
                break;

            case ElementalReactionType.Superconduct:
                ApplyArmorReduction(superconductArmorReduction, superconductDuration);
                break;

            case ElementalReactionType.Burnout:
                float burnoutTickDamage = baseReactionDamage * burnoutTickMultiplier;
                StartBurnout(attacker, burnoutTickDamage);
                break;

            case ElementalReactionType.Neuroshock:
                float neuroshockDamage = baseReactionDamage * neuroshockDamageMultiplier;
                DealReactionDamage(attacker, neuroshockDamage);
                ApplyHitStun(attacker, neuroshockStunDuration);
                break;

            case ElementalReactionType.BrittleToxin:
                StartBrittleToxin();
                break;
        }
    }

    public static ElementalReactionType GetReaction(ElementType a, ElementType b)
    {
        if (IsPair(a, b, ElementType.Fire, ElementType.Frost))
            return ElementalReactionType.Shatter;

        if (IsPair(a, b, ElementType.Fire, ElementType.Lightning))
            return ElementalReactionType.Overload;

        if (IsPair(a, b, ElementType.Frost, ElementType.Lightning))
            return ElementalReactionType.Superconduct;

        if (IsPair(a, b, ElementType.Fire, ElementType.Poison))
            return ElementalReactionType.Burnout;

        if (IsPair(a, b, ElementType.Lightning, ElementType.Poison))
            return ElementalReactionType.Neuroshock;

        if (IsPair(a, b, ElementType.Frost, ElementType.Poison))
            return ElementalReactionType.BrittleToxin;

        return ElementalReactionType.None;
    }

    private static bool IsPair(ElementType a, ElementType b, ElementType x, ElementType y)
    {
        return a == x && b == y || a == y && b == x;
    }

    private void DealReactionDamage(CharacterCtrl attacker, float damage)
    {
        DealReactionDamageTo(characterCtrl, attacker, damage);
    }

    private static void DealReactionDamageTo(CharacterCtrl target, CharacterCtrl attacker, float damage)
    {
        if (damage <= 0f || target == null || target.CharacterDamReceiver == null)
            return;

        Transform attackerTransform = attacker != null ? attacker.transform : null;
        target.CharacterDamReceiver.ReceiveDamage(
            damage,
            attackerTransform,
            new DamageData(1f, false));
    }

    private void ApplyArmorReduction(float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f || characterCtrl == null || characterCtrl.CharacterStat == null)
            return;

        StatValue armor = characterCtrl.CharacterStat.Armor;
        if (armor == null) return;

        armor.AddBuffModifier(new StatModifier(
            StatType.Armor,
            ModifierType.PercentAdd,
            -amount,
            this,
            duration));

        armor.NotifyValueChanged();
        characterCtrl.CharacterStat.NotifyAllStatsChanged();
    }

    private void StartBrittleToxin()
    {
        if (brittleToxinDuration <= 0f ||
            characterCtrl == null ||
            characterCtrl.CharacterStat == null ||
            characterCtrl.CharacterStat.Armor == null)
        {
            return;
        }

        if (brittleToxinRoutine != null)
            StopCoroutine(brittleToxinRoutine);

        brittleToxinRoutine = StartCoroutine(BrittleToxinRoutine());
    }

    private IEnumerator BrittleToxinRoutine()
    {
        ApplyArmorReduction(brittleToxinInitialArmorReduction, brittleToxinDuration);

        float rampDelay = brittleToxinDuration * 0.5f;
        if (rampDelay > 0f)
            yield return new WaitForSeconds(rampDelay);

        if (characterCtrl != null &&
            characterCtrl.CharacterStat != null &&
            characterCtrl.CharacterStat.Armor != null &&
            characterCtrl.CharacterDamReceiver != null &&
            !characterCtrl.CharacterDamReceiver.IsDead)
        {
            ApplyArmorReduction(
                brittleToxinRampArmorReduction,
                Mathf.Max(0.05f, brittleToxinDuration - rampDelay));
        }

        brittleToxinRoutine = null;
    }

    private void StartOverloadChain(CharacterCtrl attacker, float damage)
    {
        if (damage <= 0f || overloadMaxHits <= 0)
            return;

        if (overloadRoutine != null)
            StopCoroutine(overloadRoutine);

        overloadRoutine = StartCoroutine(OverloadChainRoutine(attacker, damage));
    }

    private float GetOverloadHoldDuration()
    {
        int maxHits = Mathf.Max(1, overloadMaxHits);
        float chainDuration = Mathf.Max(0.01f, overloadHitInterval) * Mathf.Max(0, maxHits - 1);
        return Mathf.Max(overloadHitStunDuration, chainDuration + 0.05f);
    }

    private IEnumerator OverloadChainRoutine(CharacterCtrl attacker, float damage)
    {
        int maxHits = Mathf.Max(1, overloadMaxHits);
        WaitForSeconds wait = new(Mathf.Max(0.01f, overloadHitInterval));
        HashSet<CharacterCtrl> hitTargets = new();
        CharacterCtrl currentTarget = characterCtrl;

        for (int hitIndex = 1; hitIndex <= maxHits; hitIndex++)
        {
            if (currentTarget == null ||
                currentTarget.CharacterDamReceiver == null ||
                currentTarget.CharacterDamReceiver.IsDead)
            {
                break;
            }

            hitTargets.Add(currentTarget);
            DealReactionDamageTo(currentTarget, attacker, damage);

            if (hitIndex < maxHits)
            {
                yield return wait;
                currentTarget = FindNextOverloadTarget(currentTarget, attacker, hitTargets);
                if (currentTarget == null)
                {
                    break;
                }

                ApplyHitStunTo(currentTarget, attacker, GetOverloadHoldDuration());
            }
        }

        overloadRoutine = null;
    }

    private CharacterCtrl FindNextOverloadTarget(
        CharacterCtrl fromTarget,
        CharacterCtrl attacker,
        HashSet<CharacterCtrl> hitTargets)
    {
        if (fromTarget == null || overloadChainRadius <= 0f)
            return null;

        Vector2 origin = fromTarget.transform.position;
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            useTriggers = true,
            layerMask = 1 << fromTarget.gameObject.layer
        };
        int count = Physics2D.OverlapCircle(origin, overloadChainRadius, filter, OverloadHits);

        CharacterCtrl bestTarget = null;
        float bestDistanceSqr = Mathf.Infinity;
        int candidateCount = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = OverloadHits[i];
            OverloadHits[i] = null;

            if (!TryGetOverloadBodyTarget(hit, fromTarget, attacker, hitTargets, out CharacterCtrl candidate))
            {
                continue;
            }

            candidateCount++;
            float distanceSqr = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private static bool TryGetOverloadBodyTarget(
        Collider2D hit,
        CharacterCtrl fromTarget,
        CharacterCtrl attacker,
        HashSet<CharacterCtrl> hitTargets,
        out CharacterCtrl target)
    {
        target = hit != null ? hit.GetComponentInParent<CharacterCtrl>() : null;
        if (target == null || target == fromTarget)
            return false;

        if (hitTargets != null && hitTargets.Contains(target))
            return false;

        if (!IsCharacterBodyCollider(hit, target))
            return false;

        if (target.CharacterDamReceiver == null || target.CharacterDamReceiver.IsDead)
            return false;

        return attacker == null || FactionManager.CanAttack(attacker.Faction, target.Faction);
    }

    private static bool IsCharacterBodyCollider(Collider2D hit, CharacterCtrl target)
    {
        if (hit == null || target == null)
            return false;

        if (target.Collider2D != null)
            return hit == target.Collider2D;

        return hit == target.GetComponent<Collider2D>();
    }

    private void StartBurnout(CharacterCtrl attacker, float tickDamage)
    {
        if (tickDamage <= 0f || burnoutDuration <= 0f)
            return;

        if (burnoutRoutine != null)
            StopCoroutine(burnoutRoutine);

        burnoutRoutine = StartCoroutine(BurnoutRoutine(attacker, tickDamage));
    }

    private IEnumerator BurnoutRoutine(CharacterCtrl attacker, float tickDamage)
    {
        float elapsed = 0f;
        WaitForSeconds wait = new(Mathf.Max(0.05f, burnoutTickInterval));

        while (elapsed < burnoutDuration &&
               characterCtrl != null &&
               characterCtrl.CharacterDamReceiver != null &&
               !characterCtrl.CharacterDamReceiver.IsDead)
        {
            characterCtrl.CharacterDamReceiver.ReceiveDamage(
                tickDamage,
                attacker != null ? attacker.transform : null,
                new DamageData(1f, false));

            yield return wait;
            elapsed += burnoutTickInterval;
        }

        burnoutRoutine = null;
    }

    private void ApplyHitStun(CharacterCtrl attacker, float duration)
    {
        ApplyHitStunTo(characterCtrl, attacker, duration);
    }

    private static void ApplyHitStunTo(CharacterCtrl target, CharacterCtrl attacker, float duration)
    {
        if (duration <= 0f || target == null || target.CharacterDamReceiver == null)
            return;

        DamageData stunData = new(0f, false)
        {
            CausesHitStun = true,
            HitStunDuration = duration,
            HitStunImmunityDuration = duration,
            InterruptsAttack = true
        };

        target.CharacterDamReceiver.ReceiveDamage(
            0f,
            attacker != null ? attacker.transform : null,
            stunData);
    }

    private void RemoveExpiredStatuses()
    {
        statuses.RemoveAll(status => status == null || status.IsExpired);
    }
}
