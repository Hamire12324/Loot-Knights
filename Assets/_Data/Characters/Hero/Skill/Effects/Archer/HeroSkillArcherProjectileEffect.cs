using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroSkillArcherProjectileEffect", menuName = "Loot Knights/Hero/Skill Effects/Archer Projectile")]
public sealed class HeroSkillArcherProjectileEffect : CharacterSkillEffectDefinition
{
    private enum ArcherSkillPattern
    {
        SimpleProjectile,
        RecallArrow,
        ShockbindArrow,
        Arrowstep,
        HeartseekerVolley
    }

    private sealed class ArrowstepAnchor
    {
        public Vector3 Position;
        public Vector3 Origin;
        public Vector2 Direction;
        public CharacterCtrl Target;
        public float ExpireTime;
        public PoolObj Marker;
    }

    private sealed class ArrowstepMarkerResult
    {
        public Vector3 Position;
        public CharacterCtrl Target;
    }

    private static readonly Dictionary<CharacterCtrl, ArrowstepAnchor> ArrowstepAnchors = new();

    private const string LongshotNodeId = "archer.recall_arrow.keen_edge";
    private const string PowerShotNodeId = "archer.power_shot";
    private const string PiercingMasteryNodeId = "archer.heartseeker.fated_arrow";
    private const string TrickArrowNodeId = "archer.arrowstep";
    private const string ArrowstepAfterimageNodeId = "archer.arrowstep.hunter_afterimage";
    private const string ArrowstepAmbushNodeId = "archer.arrowstep.ambush_stance";
    private const string ArrowstepSilentFootworkNodeId = "archer.arrowstep.silent_footwork";
    private const string ArrowstepSwapNodeId = "archer.arrowstep.swap";
    private const string ArrowstepDecoyBombNodeId = "archer.arrowstep.decoy_bomb";
    private const string ArrowstepTrajectoryDashNodeId = "archer.arrowstep.trajectory_dash";
    private const string ArrowstepDeathPathNodeId = "archer.arrowstep.death_path";
    private const string ShockbindStunDurationNodeId = "archer.shockbind_arrow.deep_stun";
    private const string ShockbindChainRadiusNodeId = "archer.shockbind_arrow.chain_radius";
    private const string ShockbindChainTargetsNodeId = "archer.shockbind_arrow.chain_targets";

    [Header("Projectile")]
    [SerializeField, Min(0.05f)] private float length = 5.5f;
    [SerializeField, Min(0.05f)] private float width = 0.45f;
    [SerializeField, Min(0f)] private float startOffset = 0.55f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private ArcherSkillPattern skillPattern = ArcherSkillPattern.SimpleProjectile;

    [Header("Damage")]
    [SerializeField] private DamageData damageData = new(0.9f, true);
    [SerializeField] private float flatBonusDamage;
    [SerializeField, Min(0f)] private float damageMultiplierPerPowerShotRank = 0.08f;
    [SerializeField, Min(0f)] private float piercingDamageMultiplierPerRank = 0.06f;
    [SerializeField, Min(0f)] private float trickArrowHitStunPerRank = 0.08f;

    [Header("Feedback")]
    [SerializeField] private VFXDefinition projectileVfx;
    [SerializeField, Min(0f)] private float projectileSpeed = 12f;
    [SerializeField] private float projectileRotationOffset;
    [SerializeField, Min(0)] private int projectilePenetration;
    [SerializeField, Min(1)] private int projectileCount = 1;
    [SerializeField, Min(0f)] private float spreadAngle = 0f;
    [SerializeField, Min(0f)] private float sideSpacing = 0f;
    [SerializeField, Min(0.1f)] private float arrowMemoryLifetime = 6f;
    [SerializeField, Min(0.1f)] private float recallOutboundDuration = 1.2f;
    [SerializeField, Min(0.1f)] private float recallDelay = 1f;
    [SerializeField, Min(0.1f)] private float huntMarkDuration = 6f;
    [SerializeField, Min(0.1f)] private float trapDuration = 6f;
    [SerializeField, Min(0.1f)] private float trapTickInterval = 0.35f;
    [SerializeField, Min(0f)] private float trapRadius = 2.4f;
    [SerializeField, Min(0f)] private float shockbindStunDuration = 0.85f;
    [SerializeField, Min(0f)] private float shockbindStunDurationPerRank = 0.25f;
    [SerializeField, Min(0f)] private float shockbindChainRadiusPerRank = 0.35f;
    [SerializeField, Min(0)] private int shockbindChainTargets = 2;
    [SerializeField, Min(0)] private int shockbindChainTargetsPerRank = 1;
    [SerializeField, Min(0f)] private float shockbindChainDamageMultiplier = 0f;
    [SerializeField, Min(0f)] private float teleportWindow = 4f;
    [SerializeField, Min(0.1f)] private float afterimageDuration = 2f;
    [SerializeField, Min(0f)] private float ambushDamageBonus = 0.35f;
    [SerializeField, Range(0f, 1f)] private float ambushCritChanceBonus = 0.35f;
    [SerializeField, Min(0.1f)] private float ambushDuration = 4f;
    [SerializeField, Min(0.1f)] private float silentFootworkDuration = 1.5f;
    [SerializeField, Min(0f)] private float silentFootworkMoveSpeedBonus = 0.35f;
    [SerializeField, Min(0f)] private float bossBehindOffset = 0.85f;
    [SerializeField, Min(0.1f)] private float decoyBombDelay = 1f;
    [SerializeField, Min(0f)] private float decoyBombRadius = 2f;
    [SerializeField, Min(0f)] private float decoyBombDamageMultiplier = 1.25f;
    [SerializeField, Min(0.1f)] private float trajectoryDashSpeed = 16f;
    [SerializeField, Min(0.05f)] private float trajectoryDashTickInterval = 0.18f;
    [SerializeField, Min(0f)] private float trajectoryDashSideRadius = 1.2f;
    [SerializeField, Min(0f)] private float trajectoryDashSideDamageMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float trajectoryDashFinalRadius = 2f;
    [SerializeField, Min(0f)] private float trajectoryDashFinalDamageMultiplier = 1f;
    [SerializeField, Min(1)] private int ultimateChargeMax = 5;
    [SerializeField, Min(1)] private int ultimateArrowsPerCharge = 1;
    [SerializeField, Min(1)] private int shadowVolleyChargeGain = 1;
    [SerializeField, Min(1)] private int deathPathChargeGainPerPierce = 1;
    [SerializeField, Min(0f)] private float ultimateFinalRadius = 1.8f;
    [SerializeField] private bool logProjectileDebug;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null || caster.CharacterStat == null)
        {
            LogProjectileDebug("Execute skipped because caster or stats are missing.");
            return;
        }

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;

        switch (skillPattern)
        {
            case ArcherSkillPattern.RecallArrow:
                caster.StartCoroutine(RecallArrowRoutine(caster, direction));
                return;
            case ArcherSkillPattern.ShockbindArrow:
                caster.StartCoroutine(ShockbindArrowRoutine(caster, direction));
                return;
            case ArcherSkillPattern.Arrowstep:
                ExecuteArrowstep(caster, direction);
                return;
            case ArcherSkillPattern.HeartseekerVolley:
                caster.StartCoroutine(HeartseekerVolleyRoutine(caster, direction));
                return;
        }

        int longshotRank = SkillTreeRankResolver.GetRank(caster, LongshotNodeId);
        int powerShotRank = SkillTreeRankResolver.GetRank(caster, PowerShotNodeId);
        int piercingMasteryRank = IsPiercingShot(context.Definition)
            ? SkillTreeRankResolver.GetRank(caster, PiercingMasteryNodeId)
            : 0;
        int trickArrowRank = SkillTreeRankResolver.GetRank(caster, TrickArrowNodeId);

        float effectiveLength = length + longshotRank * 0.55f;
        float effectiveSpeed = projectileSpeed + longshotRank * 0.75f;
        DamageData hitDamageData = GetHitDamageData(trickArrowRank);

        int count = Mathf.Max(1, projectileCount);
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = -spreadAngle * 0.5f;
        Vector2 side = new(-direction.y, direction.x);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float sideOffset = (i - (count - 1) * 0.5f) * sideSpacing;
            Vector2 projectileDirection = angle == 0f ? direction : Rotate(direction, angle);

            PlayProjectile(
                caster,
                projectileDirection,
                side * sideOffset,
                effectiveLength,
                effectiveSpeed,
                hitDamageData,
                GetMultiplierBonus(powerShotRank, piercingMasteryRank),
                GetPenetration(piercingMasteryRank));
        }
    }

    private DamageData GetHitDamageData(int trickArrowRank)
    {
        if (damageData == null || trickArrowRank <= 0 || trickArrowHitStunPerRank <= 0f)
            return damageData;

        DamageData upgradedDamageData = damageData.CloneWithElement(damageData.Element);
        upgradedDamageData.CausesHitStun = true;
        upgradedDamageData.HitStunDuration = Mathf.Max(upgradedDamageData.HitStunDuration, trickArrowRank * trickArrowHitStunPerRank);
        upgradedDamageData.HitStunImmunityDuration = Mathf.Max(upgradedDamageData.HitStunImmunityDuration, 0.6f);
        upgradedDamageData.InterruptsAttack = true;
        return upgradedDamageData;
    }

    private float GetMultiplierBonus(int powerShotRank, int piercingMasteryRank)
    {
        return powerShotRank * damageMultiplierPerPowerShotRank +
               piercingMasteryRank * piercingDamageMultiplierPerRank;
    }

    private int GetPenetration(int piercingMasteryRank)
    {
        return Mathf.Max(0, projectilePenetration + piercingMasteryRank);
    }

    private void PlayProjectile(
        CharacterCtrl caster,
        Vector2 direction,
        Vector2 positionOffset,
        float effectiveLength,
        float effectiveSpeed,
        DamageData hitDamageData,
        float multiplierBonus,
        int penetration)
    {
        if (projectileVfx == null)
        {
            LogProjectileDebug("Projectile VFX is not assigned.");
            return;
        }

        if (!VFXManager.HasInstance)
        {
            LogProjectileDebug("VFXManager instance is missing.");
            return;
        }

        Vector3 position = caster.transform.position + (Vector3)(direction * startOffset + positionOffset);
        PoolObj projectile = VFXManager.InstanceOrNull.Play(projectileVfx, position, direction);
        if (projectile == null)
        {
            LogProjectileDebug($"VFXManager returned null. definition={projectileVfx.name}.");
            return;
        }

        HeroSkillProjectileDamage projectileDamage = projectile.GetComponent<HeroSkillProjectileDamage>();
        if (projectileDamage == null)
            projectileDamage = projectile.gameObject.AddComponent<HeroSkillProjectileDamage>();

        projectileDamage.Configure(
            caster,
            targetLayer,
            hitDamageData,
            flatBonusDamage,
            multiplierBonus,
            penetration,
            width / 0.45f);

        VFXProjectileMover mover = projectile.GetComponent<VFXProjectileMover>();
        if (mover == null)
            mover = projectile.gameObject.AddComponent<VFXProjectileMover>();

        mover.Play(direction, effectiveLength, effectiveSpeed, projectileRotationOffset);
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos).normalized;
    }

    private IEnumerator RecallArrowRoutine(CharacterCtrl caster, Vector2 direction)
    {
        Vector3 start = caster.transform.position + (Vector3)(direction * startOffset);
        Vector3 outEnd = start + (Vector3)(direction * length);
        HashSet<CharacterCtrl> outboundHits = new();
        HashSet<CharacterCtrl> returnHits = new();
        float recallSpeed = length / Mathf.Max(0.1f, recallOutboundDuration);
        PoolObj recallProjectile = SpawnTravelProjectile(start, direction);

        ArcherArrowMemory memory = ArcherArrowMemory.Create(caster, start, direction, ArcherArrowMemoryKind.Flying, arrowMemoryLifetime);
        Transform recallVisual = recallProjectile != null ? recallProjectile.transform : memory.transform;

        yield return TravelSegment(
            caster,
            recallVisual,
            start,
            outEnd,
            recallSpeed,
            outboundHits,
            1f,
            projectilePenetration,
            projectileOverride: recallProjectile,
            returnProjectileOnArrival: false,
            pierceAll: true);

        yield return new WaitForSeconds(recallDelay);
        yield return TravelSegment(
            caster,
            recallVisual,
            outEnd,
            caster.transform.position,
            recallSpeed,
            returnHits,
            1.15f,
            projectilePenetration,
            true,
            recallProjectile,
            true,
            true);

        foreach (CharacterCtrl target in outboundHits)
        {
            if (target != null && returnHits.Contains(target))
                ArcherHuntMark.Apply(caster, target, huntMarkDuration);
        }

        if (memory != null)
            Destroy(memory.gameObject);

        ArcherUltimateCharge.Add(caster, 1, ultimateChargeMax);
    }

    private IEnumerator ShockbindArrowRoutine(CharacterCtrl caster, Vector2 direction)
    {
        int stunDurationRank = SkillTreeRankResolver.GetRank(caster, ShockbindStunDurationNodeId);
        int chainRadiusRank = SkillTreeRankResolver.GetRank(caster, ShockbindChainRadiusNodeId);
        int chainTargetsRank = SkillTreeRankResolver.GetRank(caster, ShockbindChainTargetsNodeId);

        float stunDuration = shockbindStunDuration + stunDurationRank * shockbindStunDurationPerRank;
        float chainRadius = trapRadius + chainRadiusRank * shockbindChainRadiusPerRank;
        int chainTargetLimit = shockbindChainTargets + chainTargetsRank * shockbindChainTargetsPerRank;

        Vector3 start = caster.transform.position + (Vector3)(direction * startOffset);
        Vector3 end = start + (Vector3)(direction * length);
        PoolObj arrow = SpawnTravelProjectile(start, direction);
        Transform arrowVisual = arrow != null ? arrow.transform : null;
        CharacterCtrl primaryTarget = null;

        yield return TravelUntilFirstHit(caster, arrowVisual, start, end, projectileSpeed, stunDuration, target => primaryTarget = target);

        if (primaryTarget != null)
        {
            StunNearbyTargets(caster, primaryTarget, chainRadius, chainTargetLimit, stunDuration);
            ArcherUltimateCharge.Add(caster, 1, ultimateChargeMax);
        }

        if (arrow != null)
            arrow.ReturnToPool();
    }

    private void ExecuteArrowstep(CharacterCtrl caster, Vector2 direction)
    {
        caster.StartCoroutine(ShadowVolleyDashRoutine(caster, direction));
    }

    private IEnumerator ShadowVolleyDashRoutine(CharacterCtrl caster, Vector2 direction)
    {
        Vector3 oldPosition = caster.transform.position;
        bool hasCloseVolley = SkillTreeRankResolver.GetRank(caster, ArrowstepSwapNodeId) > 0;
        bool hasDeathPath = SkillTreeRankResolver.GetRank(caster, ArrowstepDeathPathNodeId) > 0;
        bool hasTrajectoryDash = SkillTreeRankResolver.GetRank(caster, ArrowstepTrajectoryDashNodeId) > 0;

        float dashDistance = hasTrajectoryDash
            ? Mathf.Max(2f, length * 0.85f)
            : Mathf.Max(1.5f, length * 0.55f);
        float dashSpeed = hasTrajectoryDash ? trajectoryDashSpeed : Mathf.Max(trajectoryDashSpeed * 0.85f, 10f);
        float duration = dashDistance / Mathf.Max(0.01f, dashSpeed);
        int arrowShots = Mathf.Max(3, projectileCount) + (hasCloseVolley ? 2 : 0);
        float nextShotStep = duration / Mathf.Max(1, arrowShots);
        float nextShotTime = 0f;
        int firedShots = 0;
        float elapsed = 0f;
        Vector3 start = caster.transform.position;
        Vector3 end = start + (Vector3)(direction.normalized * dashDistance);
        HashSet<CharacterCtrl> piercedTargets = new();
        bool previousInvincible = caster.CharacterDamReceiver != null && caster.CharacterDamReceiver.IsInvincible;

        if (hasTrajectoryDash && caster.CharacterDamReceiver != null)
            caster.CharacterDamReceiver.SetInvincible(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            MoveCharacter(caster, position);

            if (elapsed >= nextShotTime)
            {
                nextShotTime += nextShotStep;
                FireShadowVolleyShot(caster, direction, hasCloseVolley, firedShots, arrowShots);
                firedShots++;
            }

            if (hasTrajectoryDash)
            {
                foreach (CharacterCtrl target in FindTargets(position, width))
                {
                    if (target == null || piercedTargets.Contains(target) || !FactionManager.CanAttack(caster.Faction, target.Faction))
                        continue;

                    piercedTargets.Add(target);
                    DealDamage(caster, target, 0.65f);

                    if (hasDeathPath)
                        ArcherUltimateCharge.Add(caster, deathPathChargeGainPerPierce, ultimateChargeMax);
                }
            }

            yield return null;
        }

        MoveCharacter(caster, end);
        FireShadowVolleyFinisher(caster, direction, hasTrajectoryDash);
        ArcherUltimateCharge.Add(caster, shadowVolleyChargeGain, ultimateChargeMax);

        if (hasTrajectoryDash && caster.CharacterDamReceiver != null)
            caster.CharacterDamReceiver.SetInvincible(previousInvincible);

        ApplyArrowstepPostMoveEffects(caster, oldPosition, end);

        if (SkillTreeRankResolver.GetRank(caster, ArrowstepDecoyBombNodeId) > 0)
            caster.StartCoroutine(DecoyBombRoutine(caster, oldPosition, null));
    }

    private void FireShadowVolleyShot(CharacterCtrl caster, Vector2 fallbackDirection, bool preferMarkedTargets, int shotIndex, int totalShots)
    {
        CharacterCtrl target = preferMarkedTargets ? ArcherHuntMark.FindBestTarget(caster) : null;
        target ??= FindNearestTarget(caster, Mathf.Max(8f, length + 2f));

        Vector2 aimDirection = fallbackDirection.normalized;
        if (aimDirection == Vector2.zero)
            aimDirection = Vector2.down;

        Vector3 casterPosition = caster.transform.position;
        Vector3 end = target != null
            ? target.transform.position
            : casterPosition + (Vector3)(aimDirection * length);

        Vector2 targetDirection = (Vector2)(end - casterPosition);
        if (targetDirection.sqrMagnitude > 0.01f)
            aimDirection = targetDirection.normalized;

        float angle = shotIndex * 37f;
        float radius = totalShots > 4 && shotIndex < totalShots - 1 ? 1.35f : 1.05f;
        Vector3 start = casterPosition + (Vector3)(Rotate(aimDirection, angle) * radius);
        HashSet<CharacterCtrl> hitTargets = new();
        caster.StartCoroutine(TravelSegment(caster, null, start, end, projectileSpeed, hitTargets, 0.65f, projectilePenetration));
    }

    private void FireShadowVolleyFinisher(CharacterCtrl caster, Vector2 direction, bool hasTrajectoryDash)
    {
        float radius = hasTrajectoryDash ? trajectoryDashFinalRadius : Mathf.Max(1.2f, trajectoryDashFinalRadius * 0.65f);
        float damageMultiplier = hasTrajectoryDash ? trajectoryDashFinalDamageMultiplier : 0.75f;
        ApplyDamageAt(caster, caster.transform.position, damageMultiplier, radius, true);
    }

    private IEnumerator FireArrowstepMarker(CharacterCtrl caster, Vector2 direction)
    {
        Vector3 start = caster.transform.position + (Vector3)(direction * startOffset);
        Vector3 end = start + (Vector3)(direction * length);
        PoolObj marker = SpawnTravelProjectile(start, direction);
        Transform markerVisual = marker != null ? marker.transform : null;
        CharacterCtrl hitTarget = null;
        Vector3 markerPosition = end;

        yield return TravelMarkerUntilHit(caster, markerVisual, start, end, projectileSpeed, result =>
        {
            markerPosition = result.Position;
            hitTarget = result.Target;
        });

        ArrowstepAnchors[caster] = new ArrowstepAnchor
        {
            Origin = caster.transform.position,
            Position = markerPosition,
            Direction = direction,
            Target = hitTarget,
            ExpireTime = Time.time + teleportWindow,
            Marker = marker
        };

        ArcherArrowMemory.Create(
            caster,
            markerPosition,
            direction,
            hitTarget != null ? ArcherArrowMemoryKind.Enemy : ArcherArrowMemoryKind.Ground,
            Mathf.Min(arrowMemoryLifetime, teleportWindow),
            hitTarget);

        caster.StartCoroutine(CleanupArrowstepAnchor(caster, marker));
    }

    private IEnumerator CleanupArrowstepAnchor(CharacterCtrl caster, PoolObj marker)
    {
        yield return new WaitForSeconds(teleportWindow);

        if (!ArrowstepAnchors.TryGetValue(caster, out ArrowstepAnchor anchor) || anchor.Marker != marker)
            yield break;

        ArrowstepAnchors.Remove(caster);
        if (marker != null)
            marker.ReturnToPool();
    }

    private IEnumerator ResolveArrowstepRecast(CharacterCtrl caster, ArrowstepAnchor anchor)
    {
        if (anchor == null)
            yield break;

        if (SkillTreeRankResolver.GetRank(caster, ArrowstepTrajectoryDashNodeId) > 0)
            yield return TrajectoryDash(caster, anchor);
        else
            ResolveArrowstepTeleport(caster, anchor);

        if (anchor.Marker != null)
            anchor.Marker.ReturnToPool();
    }

    private void ResolveArrowstepTeleport(CharacterCtrl caster, ArrowstepAnchor anchor)
    {
        Vector3 oldPosition = caster.transform.position;
        Vector3 destination = anchor.Position;
        bool hasSwap = SkillTreeRankResolver.GetRank(caster, ArrowstepSwapNodeId) > 0;
        bool swappedTarget = false;

        if (hasSwap && anchor.Target != null)
        {
            if (IsBossTarget(anchor.Target))
            {
                Vector2 behindDirection = anchor.Direction == Vector2.zero ? Vector2.down : -anchor.Direction.normalized;
                destination = anchor.Target.transform.position + (Vector3)(behindDirection * bossBehindOffset);
            }
            else
            {
                destination = anchor.Target.transform.position;
                MoveCharacter(anchor.Target, oldPosition);
                swappedTarget = true;
            }
        }

        MoveCharacter(caster, destination);
        ApplyArrowstepPostMoveEffects(caster, oldPosition, destination);

        if (swappedTarget && SkillTreeRankResolver.GetRank(caster, ArrowstepDecoyBombNodeId) > 0)
            caster.StartCoroutine(DecoyBombRoutine(caster, oldPosition, anchor.Target));
    }

    private void ApplyArrowstepPostMoveEffects(CharacterCtrl caster, Vector3 oldPosition, Vector3 newPosition)
    {
        if (SkillTreeRankResolver.GetRank(caster, ArrowstepAfterimageNodeId) > 0)
            ArcherAfterimageDecoy.Create(caster, oldPosition, afterimageDuration);

        if (SkillTreeRankResolver.GetRank(caster, ArrowstepAmbushNodeId) > 0)
            ArcherAmbushStance.Apply(caster, ambushDamageBonus, ambushCritChanceBonus, ambushDuration);

        if (SkillTreeRankResolver.GetRank(caster, ArrowstepSilentFootworkNodeId) > 0)
            caster.StartCoroutine(SilentFootworkRoutine(caster));
    }

    private void MoveCharacter(CharacterCtrl target, Vector3 position)
    {
        if (target == null)
            return;

        target.transform.position = position;
        if (target.Rb != null)
            target.Rb.position = position;
    }

    private IEnumerator HeartseekerVolleyRoutine(CharacterCtrl caster, Vector2 direction)
    {
        int consumedCharges = ArcherUltimateCharge.ConsumeAll(caster);
        CharacterCtrl target = ArcherHuntMark.FindBestTarget(caster);
        target ??= FindNearestTarget(caster, 12f);

        if (target == null)
        {
            FireProjectileSpread(caster, direction, Mathf.Max(3, projectileCount), 0.8f);
            yield break;
        }

        int baseArrowCount = Mathf.Max(1, projectileCount);
        int bonusArrowCount = consumedCharges * Mathf.Max(1, ultimateArrowsPerCharge);
        int arrowCount = Mathf.Clamp(baseArrowCount + bonusArrowCount, baseArrowCount, baseArrowCount + ultimateChargeMax * Mathf.Max(1, ultimateArrowsPerCharge));
        for (int i = 0; i < arrowCount; i++)
        {
            Vector3 start = i < bonusArrowCount
                ? caster.transform.position + (Vector3)(Rotate(direction, i * 37f) * 1.4f)
                : caster.transform.position + (Vector3)(Rotate(direction, i * 35f) * 1.2f);

            HashSet<CharacterCtrl> hitTargets = new();
            yield return TravelSegment(caster, null, start, target.transform.position, projectileSpeed, hitTargets, 0.55f, projectilePenetration);
        }

        ApplyDamageAt(caster, target.transform.position, 2.2f + arrowCount * 0.12f, ultimateFinalRadius, true);
    }

    private void FireProjectileSpread(CharacterCtrl caster, Vector2 direction, int count, float damageMultiplier)
    {
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = -spreadAngle * 0.5f;
        Vector2 side = new(-direction.y, direction.x);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float sideOffset = (i - (count - 1) * 0.5f) * sideSpacing;
            Vector2 projectileDirection = angle == 0f ? direction : Rotate(direction, angle);

            PlayProjectile(caster, projectileDirection, side * sideOffset, length, projectileSpeed, GetScaledDamageData(damageMultiplier), 0f, projectilePenetration);
        }
    }

    private IEnumerator TravelSegment(
        CharacterCtrl caster,
        Transform visual,
        Vector3 start,
        Vector3 end,
        float speed,
        HashSet<CharacterCtrl> hitTargets,
        float damageMultiplier,
        int penetration,
        bool pullTowardCaster = false,
        PoolObj projectileOverride = null,
        bool returnProjectileOnArrival = true,
        bool pierceAll = false)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        Vector2 direction = distance > 0.01f ? delta.normalized : Vector2.down;
        PoolObj projectile = projectileOverride;

        if (projectile == null)
            projectile = SpawnTravelProjectile(start, direction);

        Transform mover = visual != null ? visual : projectile != null ? projectile.transform : null;
        if (mover != null)
        {
            mover.position = start;
            RotateVisual(mover, direction);
        }

        float duration = distance / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        int remainingPenetration = penetration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            if (mover != null)
                mover.position = position;

            foreach (CharacterCtrl target in FindTargets(position, width))
            {
                if (target == null || hitTargets.Contains(target) || !FactionManager.CanAttack(caster.Faction, target.Faction))
                    continue;

                hitTargets.Add(target);
                DealDamage(caster, target, damageMultiplier);

                if (pullTowardCaster && target.Rb != null)
                    target.Rb.MovePosition(Vector2.MoveTowards(target.Rb.position, caster.transform.position, 0.45f));

                if (pierceAll)
                    continue;

                if (remainingPenetration <= 0)
                    elapsed = duration;
                else
                    remainingPenetration--;
            }

            yield return null;
        }

        if (projectile != null && returnProjectileOnArrival)
            projectile.ReturnToPool();
    }

    private IEnumerator TravelUntilFirstHit(
        CharacterCtrl caster,
        Transform visual,
        Vector3 start,
        Vector3 end,
        float speed,
        float stunDuration,
        System.Action<CharacterCtrl> onHit)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        Vector2 direction = distance > 0.01f ? delta.normalized : Vector2.down;

        if (visual != null)
        {
            visual.position = start;
            RotateVisual(visual, direction);
        }

        float duration = distance / Mathf.Max(0.01f, speed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            if (visual != null)
                visual.position = position;

            CharacterCtrl target = FindFirstAttackableTarget(caster, position, width);
            if (target != null)
            {
                DealDamage(caster, target, CreateStunDamageData(1f, stunDuration), flatBonusDamage);
                onHit?.Invoke(target);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator TravelMarkerUntilHit(
        CharacterCtrl caster,
        Transform visual,
        Vector3 start,
        Vector3 end,
        float speed,
        System.Action<ArrowstepMarkerResult> onComplete)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        Vector2 direction = distance > 0.01f ? delta.normalized : Vector2.down;

        if (visual != null)
        {
            visual.position = start;
            RotateVisual(visual, direction);
        }

        float duration = distance / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        ArrowstepMarkerResult result = new()
        {
            Position = end
        };

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            if (visual != null)
                visual.position = position;

            CharacterCtrl target = FindFirstAttackableTarget(caster, position, width);
            if (target != null)
            {
                result.Position = target.transform.position;
                result.Target = target;
                onComplete?.Invoke(result);
                yield break;
            }

            yield return null;
        }

        if (visual != null)
            visual.position = end;

        onComplete?.Invoke(result);
    }

    private IEnumerator TrajectoryDash(CharacterCtrl caster, ArrowstepAnchor anchor)
    {
        Vector3 start = caster.transform.position;
        Vector3 end = anchor.Target != null ? anchor.Target.transform.position : anchor.Position;
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        Vector2 direction = distance > 0.01f ? delta.normalized : anchor.Direction;
        float duration = distance / Mathf.Max(0.01f, trajectoryDashSpeed);
        float elapsed = 0f;
        float nextSideShotTime = 0f;
        bool createsEchoArrows = SkillTreeRankResolver.GetRank(caster, ArrowstepDeathPathNodeId) > 0;
        HashSet<CharacterCtrl> piercedTargets = new();
        bool previousInvincible = caster.CharacterDamReceiver != null && caster.CharacterDamReceiver.IsInvincible;
        Vector3 oldPosition = start;

        if (caster.CharacterDamReceiver != null)
            caster.CharacterDamReceiver.SetInvincible(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
            MoveCharacter(caster, position);

            foreach (CharacterCtrl target in FindTargets(position, width))
            {
                if (target == null || piercedTargets.Contains(target) || !FactionManager.CanAttack(caster.Faction, target.Faction))
                    continue;

                piercedTargets.Add(target);
                DealDamage(caster, target, 0.75f);

                if (createsEchoArrows)
                    ArcherUltimateCharge.Add(caster, deathPathChargeGainPerPierce, ultimateChargeMax);
            }

            if (Time.time >= nextSideShotTime)
            {
                nextSideShotTime = Time.time + trajectoryDashTickInterval;
                Vector2 side = new(-direction.y, direction.x);
                ApplyDamageAt(caster, position + (Vector3)(side * trajectoryDashSideRadius), trajectoryDashSideDamageMultiplier, trajectoryDashSideRadius, false);
                ApplyDamageAt(caster, position - (Vector3)(side * trajectoryDashSideRadius), trajectoryDashSideDamageMultiplier, trajectoryDashSideRadius, false);
            }

            yield return null;
        }

        MoveCharacter(caster, end);
        ApplyDamageAt(caster, end, trajectoryDashFinalDamageMultiplier, trajectoryDashFinalRadius, true);

        if (caster.CharacterDamReceiver != null)
            caster.CharacterDamReceiver.SetInvincible(previousInvincible);

        ApplyArrowstepPostMoveEffects(caster, oldPosition, end);
    }

    private IEnumerator SilentFootworkRoutine(CharacterCtrl caster)
    {
        StatValue moveSpeed = caster != null && caster.CharacterStat != null
            ? caster.CharacterStat.GetStat(StatType.MoveSpeed)
            : null;
        StatModifier speedModifier = null;

        if (moveSpeed != null && silentFootworkMoveSpeedBonus > 0f)
        {
            speedModifier = new StatModifier(StatType.MoveSpeed, ModifierType.Flat, silentFootworkMoveSpeedBonus, this, silentFootworkDuration);
            moveSpeed.AddBuffModifier(speedModifier);
            moveSpeed.NotifyValueChanged();
        }

        List<Collider2D> ignoredColliders = new();
        Collider2D casterCollider = caster != null ? caster.Collider2D : null;
        if (casterCollider != null)
        {
            foreach (CharacterCtrl target in FindTargets(caster.transform.position, Mathf.Max(length, trapRadius)))
            {
                if (target == null || target.Collider2D == null || !FactionManager.CanAttack(caster.Faction, target.Faction))
                    continue;

                Physics2D.IgnoreCollision(casterCollider, target.Collider2D, true);
                ignoredColliders.Add(target.Collider2D);
            }
        }

        yield return new WaitForSeconds(silentFootworkDuration);

        if (casterCollider != null)
        {
            for (int i = 0; i < ignoredColliders.Count; i++)
            {
                if (ignoredColliders[i] != null)
                    Physics2D.IgnoreCollision(casterCollider, ignoredColliders[i], false);
            }
        }

        if (moveSpeed != null)
            moveSpeed.NotifyValueChanged();
    }

    private IEnumerator DecoyBombRoutine(CharacterCtrl caster, Vector3 position, CharacterCtrl swappedTarget)
    {
        ArcherAfterimageDecoy.Create(caster, position, decoyBombDelay);
        yield return new WaitForSeconds(decoyBombDelay);

        ApplyDamageAt(caster, position, decoyBombDamageMultiplier, decoyBombRadius, true);

        if (swappedTarget != null)
        {
            DealDamage(caster, swappedTarget, decoyBombDamageMultiplier);
            ArcherHuntMark.Apply(caster, swappedTarget, huntMarkDuration);
        }

        if (swappedTarget != null)
        {
            foreach (CharacterCtrl target in FindTargets(position, decoyBombRadius))
            {
                if (target != null && target != swappedTarget && FactionManager.CanAttack(caster.Faction, target.Faction))
                    ArcherHuntMark.Apply(caster, target, huntMarkDuration);
            }
        }
    }

    private bool IsBossTarget(CharacterCtrl target)
    {
        if (target == null)
            return false;

        string targetName = target.name.ToLowerInvariant();
        return targetName.Contains("boss");
    }

    private CharacterCtrl FindFirstAttackableTarget(CharacterCtrl caster, Vector3 center, float radius)
    {
        foreach (CharacterCtrl target in FindTargets(center, radius))
        {
            if (target != null && FactionManager.CanAttack(caster.Faction, target.Faction))
                return target;
        }

        return null;
    }

    private void StunNearbyTargets(CharacterCtrl caster, CharacterCtrl primaryTarget, float radius, int targetLimit, float stunDuration)
    {
        if (targetLimit <= 0)
            return;

        int stunnedCount = 0;
        foreach (CharacterCtrl target in FindTargets(primaryTarget.transform.position, radius))
        {
            if (target == null || target == primaryTarget || !FactionManager.CanAttack(caster.Faction, target.Faction))
                continue;

            DealDamage(caster, target, CreateStunDamageData(shockbindChainDamageMultiplier, stunDuration), 0f);
            stunnedCount++;

            if (stunnedCount >= targetLimit)
                break;
        }
    }

    private PoolObj SpawnTravelProjectile(Vector3 position, Vector2 direction)
    {
        if (projectileVfx == null || !VFXManager.HasInstance)
            return null;

        PoolObj projectile = VFXManager.InstanceOrNull.Play(projectileVfx, position, direction);
        if (projectile != null)
            RotateVisual(projectile.transform, direction);

        return projectile;
    }

    private void RotateVisual(Transform visual, Vector2 direction)
    {
        if (visual == null)
            return;

        if (direction == Vector2.zero)
            direction = Vector2.down;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Euler(0f, 0f, angle + projectileRotationOffset);
    }

    private void ApplyDamageAt(CharacterCtrl caster, Vector3 center, float multiplier, float radius, bool markTargets)
    {
        foreach (CharacterCtrl target in FindTargets(center, radius))
        {
            if (target == null || !FactionManager.CanAttack(caster.Faction, target.Faction))
                continue;

            DealDamage(caster, target, multiplier);

            if (markTargets)
                ArcherHuntMark.Apply(caster, target, huntMarkDuration);
        }
    }

    private void DealDamage(CharacterCtrl caster, CharacterCtrl target, float multiplier)
    {
        if (caster == null || target == null || target.CharacterDamReceiver == null || caster.CharacterStat == null)
            return;

        DamageData hitData = GetScaledDamageData(multiplier);
        DealDamage(caster, target, hitData, flatBonusDamage);
    }

    private void DealDamage(CharacterCtrl caster, CharacterCtrl target, DamageData hitData, float bonusDamage)
    {
        if (caster == null || target == null || target.CharacterDamReceiver == null || caster.CharacterStat == null || hitData == null)
            return;

        float damage = caster.CharacterStat.Attack.FinalValue * hitData.Multiplier + bonusDamage;
        target.CharacterDamReceiver.ReceiveDamage(damage, caster.transform, hitData);
    }

    private DamageData GetScaledDamageData(float multiplier)
    {
        DamageData result = damageData != null ? damageData.CloneWithElement(damageData.Element) : new DamageData(1f, true);
        result.Multiplier *= multiplier;
        return result;
    }

    private DamageData CreateStunDamageData(float multiplier, float stunDuration)
    {
        DamageData result = GetScaledDamageData(multiplier);
        result.CausesHitStun = true;
        result.HitStunDuration = Mathf.Max(0f, stunDuration);
        result.HitStunImmunityDuration = Mathf.Max(result.HitStunImmunityDuration, 0.6f);
        result.InterruptsAttack = true;
        return result;
    }

    private CharacterCtrl FindNearestTarget(CharacterCtrl caster, float radius)
    {
        CharacterCtrl nearest = null;
        float bestDistance = float.MaxValue;
        foreach (CharacterCtrl target in FindTargets(caster.transform.position, radius))
        {
            if (target == null || !FactionManager.CanAttack(caster.Faction, target.Faction))
                continue;

            float distance = Vector2.Distance(caster.transform.position, target.transform.position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = target;
        }

        return nearest;
    }

    private IEnumerable<CharacterCtrl> FindTargets(Vector3 center, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0.05f, radius), targetLayer);
        foreach (Collider2D hit in hits)
        {
            CharacterCtrl target = hit != null ? hit.GetComponentInParent<CharacterCtrl>() : null;
            if (target != null && target.CharacterDamReceiver != null && !target.CharacterDamReceiver.IsDead)
                yield return target;
        }
    }

    private void PlayStationaryArrow(Vector3 position, Vector2 direction)
    {
        if (projectileVfx == null || !VFXManager.HasInstance)
            return;

        VFXManager.InstanceOrNull.Play(projectileVfx, position, direction);
    }

    private static bool IsPiercingShot(CharacterSkillDefinition definition)
    {
        return definition != null && definition.SkillId == "ranger.piercing_shot";
    }

    private void LogProjectileDebug(string message)
    {
        if (!logProjectileDebug)
            return;

        Debug.Log($"{nameof(HeroSkillArcherProjectileEffect)} ({name}): {message}", this);
    }

    private void OnValidate()
    {
        length = Mathf.Max(0.05f, length);
        width = Mathf.Max(0.05f, width);
        startOffset = Mathf.Max(0f, startOffset);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectilePenetration = Mathf.Max(0, projectilePenetration);
        projectileCount = Mathf.Max(1, projectileCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        sideSpacing = Mathf.Max(0f, sideSpacing);
        arrowMemoryLifetime = Mathf.Max(0.1f, arrowMemoryLifetime);
        recallOutboundDuration = Mathf.Max(0.1f, recallOutboundDuration);
        recallDelay = Mathf.Max(0.1f, recallDelay);
        huntMarkDuration = Mathf.Max(0.1f, huntMarkDuration);
        trapDuration = Mathf.Max(0.1f, trapDuration);
        trapTickInterval = Mathf.Max(0.1f, trapTickInterval);
        trapRadius = Mathf.Max(0f, trapRadius);
        shockbindStunDuration = Mathf.Max(0f, shockbindStunDuration);
        shockbindStunDurationPerRank = Mathf.Max(0f, shockbindStunDurationPerRank);
        shockbindChainRadiusPerRank = Mathf.Max(0f, shockbindChainRadiusPerRank);
        shockbindChainTargets = Mathf.Max(0, shockbindChainTargets);
        shockbindChainTargetsPerRank = Mathf.Max(0, shockbindChainTargetsPerRank);
        shockbindChainDamageMultiplier = Mathf.Max(0f, shockbindChainDamageMultiplier);
        teleportWindow = Mathf.Max(0f, teleportWindow);
        ultimateChargeMax = Mathf.Max(1, ultimateChargeMax);
        ultimateArrowsPerCharge = Mathf.Max(1, ultimateArrowsPerCharge);
        shadowVolleyChargeGain = Mathf.Max(1, shadowVolleyChargeGain);
        deathPathChargeGainPerPierce = Mathf.Max(1, deathPathChargeGainPerPierce);
        ultimateFinalRadius = Mathf.Max(0f, ultimateFinalRadius);
        damageMultiplierPerPowerShotRank = Mathf.Max(0f, damageMultiplierPerPowerShotRank);
        piercingDamageMultiplierPerRank = Mathf.Max(0f, piercingDamageMultiplierPerRank);
        trickArrowHitStunPerRank = Mathf.Max(0f, trickArrowHitStunPerRank);

        if (damageData == null)
            damageData = new DamageData(1f, false);
    }
}
