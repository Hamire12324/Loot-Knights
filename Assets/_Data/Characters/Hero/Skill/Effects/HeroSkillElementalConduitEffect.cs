using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillElementalConduitEffect", menuName = "Loot Knights/Hero/Skill Effects/Elemental Conduit")]
public sealed class HeroSkillElementalConduitEffect : CharacterSkillEffectDefinition
{
    [Serializable]
    private struct ElementalDamageSettings
    {
        [SerializeField, Min(0f)] private float multiplier;
        [SerializeField] private bool canCrit;

        [Header("Hit Stun")]
        [SerializeField] private bool causesHitStun;
        [SerializeField, Min(0f)] private float hitStunDuration;
        [SerializeField, Min(0f)] private float hitStunImmunityDuration;
        [SerializeField] private bool interruptsAttack;

        public ElementalDamageSettings(float multiplier, bool canCrit)
        {
            this.multiplier = Mathf.Max(0f, multiplier);
            this.canCrit = canCrit;
            causesHitStun = false;
            hitStunDuration = 0.18f;
            hitStunImmunityDuration = 0.5f;
            interruptsAttack = true;
        }

        public DamageData ToDamageData(
            ElementType element,
            float elementalPower,
            float statusDuration)
        {
            DamageData damageData = new(Mathf.Max(0f, multiplier), canCrit)
            {
                CausesHitStun = causesHitStun,
                HitStunDuration = Mathf.Max(0f, hitStunDuration),
                HitStunImmunityDuration = Mathf.Max(0f, hitStunImmunityDuration),
                InterruptsAttack = interruptsAttack
            };

            return damageData.CloneWithElement(
                element,
                Mathf.Max(0f, elementalPower),
                Mathf.Max(0f, statusDuration),
                true);
        }

        public void Validate()
        {
            multiplier = Mathf.Max(0f, multiplier);
            hitStunDuration = Mathf.Max(0f, hitStunDuration);
            hitStunImmunityDuration = Mathf.Max(0f, hitStunImmunityDuration);
        }
    }

    [Serializable]
    private struct ReactionVfxOverride
    {
        [SerializeField] private ElementalReactionType reaction;
        [SerializeField] private VFXDefinition impactVfx;
        [SerializeField] private SFXDefinition impactSfx;

        public ElementalReactionType Reaction => reaction;
        public VFXDefinition ImpactVfx => impactVfx;
        public SFXDefinition ImpactSfx => impactSfx;
    }

    [Header("Skill Tree")]
    [SerializeField] private SkillTreeDefinition fallbackSkillTree;
    [SerializeField] private string conduitNodeId = "common.elemental_conduit";
    [SerializeField, Min(1)] private int fallbackRank = 1;
    [SerializeField] private bool requireElementUnlocks = true;
    [SerializeField] private bool requireReactionUnlocks = true;
    [SerializeField] private bool unlockAllReactionsForTesting = false;

    [Header("Absorb")]
    [SerializeField, Min(0.1f)] private float baseAbsorbRadius = 3f;
    [SerializeField, Min(0f)] private float absorbRadiusPerRank = 0.35f;
    [SerializeField, Min(1)] private int baseStoredElements = 4;
    [SerializeField, Min(1)] private int ranksPerExtraStoredElement = 2;
    [SerializeField, Min(1)] private int maxStoredElements = 4;
    [SerializeField, Min(1)] private int maxStacksPerElement = 3;
    [SerializeField, Min(0f)] private float stackPowerGain = 0.5f;
    [SerializeField] private bool allowAbsorbingLockedElements = true;

    [Header("Release Shape")]
    [SerializeField, Min(0.05f)] private float baseReleaseRadius = 1.45f;
    [SerializeField, Min(0f)] private float releaseRadiusPerRank = 0.18f;
    [SerializeField, Range(1f, 360f)] private float angle = 120f;
    [SerializeField] private float forwardOffset = 0.75f;
    [SerializeField] private float sideOffset;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private ElementalDamageSettings primaryDamage = new(1.1f, true);
    [SerializeField] private ElementalDamageSettings reactionPrimerDamage = new(0.35f, false);
    [SerializeField, Min(0f)] private float primaryMultiplierPerRank = 0.12f;
    [SerializeField, Min(0f)] private float primerMultiplierPerRank = 0.04f;
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float flatBonusDamagePerRank = 2f;
    [SerializeField, Min(0f)] private float baseElementalPower = 1f;
    [SerializeField, Min(0f)] private float elementalPowerPerRank = 0.15f;
    [SerializeField, Min(0f)] private float primerPowerScale = 0.75f;
    [SerializeField, Min(0f)] private float damageBonusPerStack = 0.25f;
    [SerializeField, Min(0f)] private float baseStatusDuration = 4f;
    [SerializeField, Min(0f)] private float statusDurationPerRank = 0.35f;

    [Header("Feedback")]
    [SerializeField] private ReactionVfxOverride[] reactionVfxOverrides;

    [Header("VFX Damage Collider")]
    [SerializeField] private bool useImpactVfxColliderForDamage = true;

    [Header("Debug")]
    [SerializeField] private bool debugHitArea;
    [SerializeField] private bool debugLogHits;
    [SerializeField, Min(0.02f)] private float debugDrawDuration = 0.45f;
    [SerializeField] private Color debugHitColor = new(0f, 1f, 1f, 1f);
    [SerializeField] private Color debugRejectedColor = new(1f, 0.15f, 0.05f, 1f);

    public override void Execute(CharacterSkillExecutionContext context)
    {
        ReleaseStored(context);
    }

    public bool AbsorbShards(CharacterCtrl caster, Vector2 direction)
    {
        if (caster == null)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        int rank = GetConduitRank(runtime);
        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null)
            return false;

        int absorbed = ElementalShardPickup.AbsorbNearby(
            caster.transform.position,
            GetAbsorbRadius(rank),
            caster.transform,
            shard => allowAbsorbingLockedElements ||
                     ElementalConduitUnlocks.IsElementAvailable(
                         runtime,
                         shard.Element,
                         requireElementUnlocks),
            shard =>
            {
                if (shard == null)
                    return;

                StoreAbsorbedShard(conduitState, shard, rank);
            });

        return absorbed > 0;
    }

    public bool AbsorbShard(CharacterCtrl caster, ElementalShardPickup shard, Vector2 direction)
    {
        if (caster == null || shard == null || !shard.IsAvailable)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        if (!allowAbsorbingLockedElements &&
            !ElementalConduitUnlocks.IsElementAvailable(
                runtime,
                shard.Element,
                requireElementUnlocks))
        {
            return false;
        }

        int rank = GetConduitRank(runtime);
        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null)
            return false;

        shard.BeginAbsorb(
            caster.transform,
            collected =>
            {
                if (collected == null || conduitState == null)
                    return;

                StoreAbsorbedShard(conduitState, collected, rank);
            });
        return true;
    }

    public bool CollectShard(CharacterCtrl caster, ElementalShardPickup shard)
    {
        if (caster == null || shard == null || !shard.IsAvailable)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        if (!allowAbsorbingLockedElements &&
            !ElementalConduitUnlocks.IsElementAvailable(
                runtime,
                shard.Element,
                requireElementUnlocks))
        {
            return false;
        }

        int rank = GetConduitRank(runtime);
        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null)
            return false;

        StoreAbsorbedShard(conduitState, shard, rank);
        return true;
    }

    public bool AddAllElementsForTesting(CharacterCtrl caster)
    {
        if (caster == null)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        int rank = GetConduitRank(runtime);
        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null)
            return false;

        int capacity = Mathf.Max(4, GetStoredElementCapacity(rank));
        float power = GetElementalPower(rank);
        conduitState.Store(ElementType.Fire, power, capacity, maxStacksPerElement, stackPowerGain);
        conduitState.Store(ElementType.Frost, power, capacity, maxStacksPerElement, stackPowerGain);
        conduitState.Store(ElementType.Lightning, power, capacity, maxStacksPerElement, stackPowerGain);
        conduitState.Store(ElementType.Poison, power, capacity, maxStacksPerElement, stackPowerGain);
        return true;
    }

    public bool CanRelease(CharacterCtrl caster)
    {
        return CanRelease(caster, out _);
    }

    public bool CanRelease(CharacterCtrl caster, out string reason)
    {
        ElementalConduitState state = caster != null ? caster.GetComponent<ElementalConduitState>() : null;
        if (state == null || !state.HasStoredElements)
        {
            reason = "No stored elements.";
            return false;
        }

        if (state.SelectedSlotCount < 2)
        {
            reason = "Select two stored elements.";
            return false;
        }

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        if (!TryGetReleasePreview(caster, requireUnlockedReaction: false, out ElementalConduitReleasePayload preview) ||
            preview.Reaction == ElementalReactionType.None)
        {
            reason = "Selected elements do not create a reaction.";
            return false;
        }

        if (!IsReactionUnlocked(runtime, preview.Reaction))
        {
            reason = $"{preview.Reaction} reaction is locked in the elemental skill tree.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryGetReleasePreview(CharacterCtrl caster, out ElementalConduitReleasePayload preview)
    {
        return TryGetReleasePreview(caster, requireUnlockedReaction: true, out preview);
    }

    public bool PrepareRelease(CharacterCtrl caster)
    {
        if (caster == null)
            return false;

        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null || !conduitState.HasStoredElements)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;

        return conduitState.TryPrepareRelease(
            true,
            reaction => IsReactionUnlocked(runtime, reaction),
            out _);
    }

    private bool ReleaseStored(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null)
            return false;

        ElementalConduitState conduitState = GetOrCreateState(caster);
        if (conduitState == null || (!conduitState.HasStoredElements && !conduitState.HasPreparedRelease))
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        int rank = GetConduitRank(runtime);

        if (!conduitState.TryConsumeForRelease(
                true,
                reaction => IsReactionUnlocked(runtime, reaction),
                out ElementalConduitReleasePayload release))
        {
            return false;
        }

        if (!release.HasPrimary || !release.HasPrimer || release.Reaction == ElementalReactionType.None)
            return false;

        ElementalReactionType reaction = release.Reaction;
        VFXDefinition selectedImpactVfx = ResolveImpactVfx(reaction);
        SFXDefinition selectedImpactSfx = ResolveImpactSfx(reaction);

        ElementalConduitPulse.Release(new ElementalConduitPulseRequest
        {
            Context = context,
            Rank = rank,
            PrimaryElement = release.PrimaryElement,
            PrimaryPower = Mathf.Max(GetElementalPower(rank), release.PrimaryPower),
            PrimaryStacks = release.PrimaryStacks,
            UsePrimer = true,
            PrimerElement = release.PrimerElement,
            Reaction = reaction,
            PrimerPower = Mathf.Max(GetElementalPower(rank) * primerPowerScale, release.PrimerPower),
            PrimerStacks = release.PrimerStacks,
            PrimaryDamageData = primaryDamage.ToDamageData(
                release.PrimaryElement,
                Mathf.Max(GetElementalPower(rank), release.PrimaryPower),
                GetStatusDuration(rank)),
            ReactionPrimerDamageData = reactionPrimerDamage.ToDamageData(
                release.PrimerElement,
                Mathf.Max(GetElementalPower(rank) * primerPowerScale, release.PrimerPower),
                GetStatusDuration(rank)),
            PrimaryMultiplierPerRank = primaryMultiplierPerRank,
            PrimerMultiplierPerRank = primerMultiplierPerRank,
            FlatBonusDamage = flatBonusDamage,
            FlatBonusDamagePerRank = flatBonusDamagePerRank,
            DamageBonusPerStack = damageBonusPerStack,
            StatusDuration = GetStatusDuration(rank),
            ReleaseRadius = GetReleaseRadius(rank),
            Angle = angle,
            ForwardOffset = forwardOffset,
            SideOffset = sideOffset,
            TargetLayer = targetLayer,
            ImpactVfx = selectedImpactVfx,
            ImpactSfx = selectedImpactSfx,
            UseImpactVfxColliderForDamage = useImpactVfxColliderForDamage,
            DebugHitArea = debugHitArea,
            DebugLogHits = debugLogHits,
            DebugDrawDuration = debugDrawDuration,
            DebugHitColor = debugHitColor,
            DebugRejectedColor = debugRejectedColor
        });

        return true;
    }

    private SkillTreeDefinition ResolveSkillTree(CharacterCtrl caster)
    {
        HeroSkillLoadoutPhotonSync loadoutSync = caster != null
            ? caster.GetComponent<HeroSkillLoadoutPhotonSync>()
            : null;

        SkillTreeDefinition conduitTree = loadoutSync != null
            ? loadoutSync.FindSkillTreeContainingNode(conduitNodeId)
            : null;

        if (conduitTree != null)
            return conduitTree;

        return fallbackSkillTree;
    }

    private bool TryGetReleasePreview(
        CharacterCtrl caster,
        bool requireUnlockedReaction,
        out ElementalConduitReleasePayload preview)
    {
        preview = default;

        ElementalConduitState state = caster != null ? caster.GetComponent<ElementalConduitState>() : null;
        if (state == null)
            return false;

        SkillTreeDefinition tree = ResolveSkillTree(caster);
        SkillTreeRuntime runtime = tree != null ? new SkillTreeRuntime(tree) : null;
        return state.TryGetReleasePreview(
                   reaction => !requireUnlockedReaction || IsReactionUnlocked(runtime, reaction),
                   out preview) &&
               preview.HasPrimary &&
               preview.HasPrimer &&
               preview.Reaction != ElementalReactionType.None;
    }

    private bool IsReactionUnlocked(SkillTreeRuntime runtime, ElementalReactionType reaction)
    {
        return unlockAllReactionsForTesting ||
               ElementalConduitUnlocks.IsReactionUnlocked(runtime, reaction, requireReactionUnlocks);
    }

    private int GetConduitRank(SkillTreeRuntime runtime)
    {
        int rank = runtime != null ? runtime.GetRank(conduitNodeId) : 0;
        if (rank <= 0)
            rank = fallbackRank;

        return Mathf.Max(1, rank);
    }

    private ElementalConduitState GetOrCreateState(CharacterCtrl caster)
    {
        ElementalConduitState state = caster.GetComponent<ElementalConduitState>();
        if (state == null)
            state = caster.gameObject.AddComponent<ElementalConduitState>();

        return state;
    }

    private float GetAbsorbRadius(int rank)
    {
        return baseAbsorbRadius + Mathf.Max(0, rank - 1) * absorbRadiusPerRank;
    }

    private float GetReleaseRadius(int rank)
    {
        return baseReleaseRadius + Mathf.Max(0, rank - 1) * releaseRadiusPerRank;
    }

    private int GetStoredElementCapacity(int rank)
    {
        int extra = Mathf.Max(0, rank - 1) / Mathf.Max(1, ranksPerExtraStoredElement);
        return Mathf.Clamp(baseStoredElements + extra, 1, Mathf.Max(1, maxStoredElements));
    }

    private float GetElementalPower(int rank)
    {
        return baseElementalPower + Mathf.Max(0, rank - 1) * elementalPowerPerRank;
    }

    private float GetStatusDuration(int rank)
    {
        return baseStatusDuration + Mathf.Max(0, rank - 1) * statusDurationPerRank;
    }

    private void StoreAbsorbedShard(ElementalConduitState conduitState, ElementalShardPickup shard, int rank)
    {
        if (conduitState == null || shard == null)
            return;

        float skillPower = GetElementalPower(rank);
        float absorbedPower = Mathf.Max(skillPower, shard.Power);
        conduitState.Store(
            shard.Element,
            absorbedPower,
            GetStoredElementCapacity(rank),
            maxStacksPerElement,
            stackPowerGain);
    }

    private VFXDefinition ResolveImpactVfx(ElementalReactionType reaction)
    {
        if (reaction != ElementalReactionType.None)
        {
            foreach (ReactionVfxOverride entry in reactionVfxOverrides ?? Array.Empty<ReactionVfxOverride>())
            {
                if (entry.Reaction == reaction && entry.ImpactVfx != null)
                    return entry.ImpactVfx;
            }
        }

        return null;
    }

    private SFXDefinition ResolveImpactSfx(ElementalReactionType reaction)
    {
        if (reaction != ElementalReactionType.None)
        {
            foreach (ReactionVfxOverride entry in reactionVfxOverrides ?? Array.Empty<ReactionVfxOverride>())
            {
                if (entry.Reaction == reaction && entry.ImpactSfx != null)
                    return entry.ImpactSfx;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        baseAbsorbRadius = Mathf.Max(0.1f, baseAbsorbRadius);
        baseReleaseRadius = Mathf.Max(0.05f, baseReleaseRadius);
        baseStoredElements = Mathf.Max(1, baseStoredElements);
        ranksPerExtraStoredElement = Mathf.Max(1, ranksPerExtraStoredElement);
        maxStoredElements = Mathf.Max(baseStoredElements, maxStoredElements);
        maxStacksPerElement = Mathf.Max(1, maxStacksPerElement);
        stackPowerGain = Mathf.Max(0f, stackPowerGain);
        fallbackRank = Mathf.Max(1, fallbackRank);
        damageBonusPerStack = Mathf.Max(0f, damageBonusPerStack);
        debugDrawDuration = Mathf.Max(0.02f, debugDrawDuration);

        primaryDamage.Validate();
        reactionPrimerDamage.Validate();
    }
}
