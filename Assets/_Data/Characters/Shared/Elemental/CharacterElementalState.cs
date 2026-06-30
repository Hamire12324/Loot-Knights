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
    private static readonly Collider2D[] Hits = new Collider2D[24];

    [Header("Reaction Tuning")]
    [SerializeField, Min(0f)] private float shatterBonusDamageMultiplier = 0.65f;
    [SerializeField, Min(0f)] private float overloadBonusDamageMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float overloadRadius = 1.6f;
    [SerializeField, Min(0f)] private float superconductArmorReduction = 0.25f;
    [SerializeField, Min(0f)] private float superconductDuration = 3f;
    [SerializeField, Min(0f)] private float burnoutTickMultiplier = 0.2f;
    [SerializeField, Min(0.05f)] private float burnoutTickInterval = 0.5f;
    [SerializeField, Min(0f)] private float burnoutDuration = 3f;
    [SerializeField, Min(0f)] private float neuroshockDamageMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float neuroshockStunDuration = 0.45f;
    [SerializeField, Min(0f)] private float brittleToxinArmorReduction = 0.15f;
    [SerializeField, Min(0f)] private float brittleToxinDuration = 4f;

    private readonly List<ElementalStatus> statuses = new();
    private Coroutine burnoutRoutine;

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
                DealReactionDamage(attacker, baseReactionDamage * shatterBonusDamageMultiplier);
                break;

            case ElementalReactionType.Overload:
                DealReactionDamage(attacker, baseReactionDamage * overloadBonusDamageMultiplier);
                DealOverloadSplash(attacker, baseReactionDamage * overloadBonusDamageMultiplier);
                break;

            case ElementalReactionType.Superconduct:
                ApplyArmorReduction(superconductArmorReduction, superconductDuration);
                break;

            case ElementalReactionType.Burnout:
                StartBurnout(attacker, baseReactionDamage * burnoutTickMultiplier);
                break;

            case ElementalReactionType.Neuroshock:
                DealReactionDamage(attacker, baseReactionDamage * neuroshockDamageMultiplier);
                ApplyHitStun(attacker, neuroshockStunDuration);
                break;

            case ElementalReactionType.BrittleToxin:
                ApplyArmorReduction(brittleToxinArmorReduction, brittleToxinDuration);
                break;
        }
    }

    private static ElementalReactionType GetReaction(ElementType a, ElementType b)
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
        if (damage <= 0f || characterCtrl == null || characterCtrl.CharacterDamReceiver == null)
            return;

        Transform attackerTransform = attacker != null ? attacker.transform : null;
        characterCtrl.CharacterDamReceiver.ReceiveDamage(
            damage,
            attackerTransform,
            new DamageData(1f, false));
    }

    private void DealOverloadSplash(CharacterCtrl attacker, float damage)
    {
        if (damage <= 0f || characterCtrl == null)
            return;

        ContactFilter2D filter = new()
        {
            useTriggers = true,
            useLayerMask = false
        };

        int count = Physics2D.OverlapCircle(characterCtrl.transform.position, overloadRadius, filter, Hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null) continue;

            CharacterCtrl splashTarget = hit.GetComponentInParent<CharacterCtrl>();
            if (splashTarget == null || splashTarget == characterCtrl || splashTarget == attacker) continue;
            if (splashTarget.CharacterDamReceiver == null || splashTarget.CharacterDamReceiver.IsDead) continue;
            if (attacker != null && !FactionManager.CanAttack(attacker.Faction, splashTarget.Faction)) continue;

            splashTarget.CharacterDamReceiver.ReceiveDamage(
                damage,
                attacker != null ? attacker.transform : null,
                new DamageData(1f, false));
        }
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
        if (duration <= 0f || characterCtrl == null || characterCtrl.CharacterDamReceiver == null)
            return;

        DamageData stunData = new(0f, false)
        {
            CausesHitStun = true,
            HitStunDuration = duration,
            HitStunImmunityDuration = duration,
            InterruptsAttack = true
        };

        characterCtrl.CharacterDamReceiver.ReceiveDamage(
            0f,
            attacker != null ? attacker.transform : null,
            stunData);
    }

    private void RemoveExpiredStatuses()
    {
        statuses.RemoveAll(status => status == null || status.IsExpired);
    }
}
