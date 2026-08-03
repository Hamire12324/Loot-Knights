using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSkillController : CharacterAbstract
{
    private const float AnimationEventFallbackDelay = 0.4f;
    private const float MinAttackSpeedMultiplier = 0.1f;

    [Header("Basic Attack")]
    [SerializeField] protected CharacterSkillDefinition basicAttack;

    [Header("Active Skills")]
    [SerializeField] protected CharacterSkillDefinition[] equippedSkills = new CharacterSkillDefinition[4];

    [Header("Special Skill")]
    [SerializeField] protected CharacterSkillDefinition specialSkill;
    [SerializeField] protected bool cancelBasicAttackOnCast = true;

    private readonly CharacterSkillLoadout loadout = new();
    private Coroutine castingRoutine;
    private CharacterSkillRuntime currentCastingRuntime;
    private Vector2 pendingAimDirection;
    private Transform pendingTarget;
    private bool waitingForAnimationHit;
    private bool currentCastEffectsExecuted;
    private CharacterSkillAnimationDriver animationDriver;
    private CharacterSkillFacing facing;

    public CharacterSkillRuntime BasicAttackRuntime => loadout.BasicAttackRuntime;
    public CharacterSkillRuntime SpecialSkillRuntime => loadout.SpecialSkillRuntime;
    public bool IsCasting => castingRoutine != null;
    public bool IsAttackVisualActive => animationDriver != null && animationDriver.IsAttackVisualActive();
    public IReadOnlyList<CharacterSkillRuntime> RuntimeSkills => loadout.RuntimeSkills;

    protected override void Awake()
    {
        base.Awake();

        animationDriver = new CharacterSkillAnimationDriver(characterCtrl);
        facing = new CharacterSkillFacing(characterCtrl);
        RebuildRuntimeSkills();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        facing?.RestoreWhenAttackVisualEnds(animationDriver != null && animationDriver.IsAttackVisualActive());
    }

    public CharacterSkillRuntime GetSkill(int index)
    {
        return loadout.GetSkill(index);
    }

    public CharacterSkillRuntime GetSpecialSkill()
    {
        return loadout.SpecialSkillRuntime;
    }

    public bool TryCast(int index)
    {
        CharacterSkillRuntime runtime = GetSkill(index);
        if (runtime == null || !runtime.CanCast(this)) return false;

        castingRoutine = StartCoroutine(CastRoutine(runtime));
        return true;
    }

    public bool TryCastSpecialSkill()
    {
        CharacterSkillRuntime runtime = GetSpecialSkill();
        if (runtime == null || !runtime.CanCast(this)) return false;
        if (!CanCastSpecialSkillRuntime(runtime)) return false;

        castingRoutine = StartCoroutine(CastRoutine(runtime));
        return true;
    }

    protected virtual bool CanCastSpecialSkillRuntime(CharacterSkillRuntime runtime)
    {
        return true;
    }

    public virtual bool TryCastBasicAttack()
    {
        if (BasicAttackRuntime == null)
        {
            Debug.LogWarning(
                $"{nameof(CharacterSkillController)} basic attack runtime is null; using fallback combat attack.",
                gameObject);
            return TryFallbackBasicAttack();
        }

        if (!BasicAttackRuntime.CanCast(this))
        {
            Debug.LogWarning(
                $"{nameof(CharacterSkillController)} cannot cast basic attack '{BasicAttackRuntime.Definition?.name ?? "null"}'. {GetBasicAttackBlockedReason(BasicAttackRuntime)}",
                gameObject);
            return false;
        }

        castingRoutine = StartCoroutine(CastRoutine(BasicAttackRuntime));
        return true;
    }

    private string GetBasicAttackBlockedReason(CharacterSkillRuntime runtime)
    {
        if (runtime == null)
            return "Runtime is null.";

        if (runtime.Definition == null)
            return "Definition is null.";

        if (!runtime.IsUnlocked)
            return "Skill is locked.";

        if (!runtime.Cooldown.IsReady)
            return $"Cooldown remaining={runtime.Cooldown.Remaining:0.00}s.";

        if (IsCasting)
            return "Controller is already casting.";

        if (characterCtrl == null)
            return "CharacterCtrl is null.";

        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsDead)
            return "Character is dead.";

        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsHitStunned)
            return "Character is hit-stunned.";

        return "Unknown reason.";
    }

    public void CancelCast(bool force = false)
    {
        if (castingRoutine == null) return;
        if (!force && GetCurrentDefinition()?.CanBeInterrupted == false) return;

        StopCoroutine(castingRoutine);
        castingRoutine = null;
        currentCastingRuntime = null;
        ClearPendingCast();
        facing?.RestoreOriginalScale();
        SetMovementLocked(false);
    }

    public void SetEquippedSkill(int index, CharacterSkillDefinition definition)
    {
        if (index < 0) return;

        if (index >= equippedSkills.Length)
        {
            CharacterSkillDefinition[] resized = new CharacterSkillDefinition[index + 1];
            equippedSkills.CopyTo(resized, 0);
            equippedSkills = resized;
        }

        equippedSkills[index] = definition;
        RebuildRuntimeSkills();
    }

    public void SetBasicAttack(CharacterSkillDefinition definition)
    {
        basicAttack = definition;
        RebuildRuntimeSkills();
    }

    public void SetSpecialSkill(CharacterSkillDefinition definition)
    {
        specialSkill = definition;
        RebuildRuntimeSkills();
    }

    protected virtual IEnumerator CastRoutine(CharacterSkillRuntime runtime)
    {
        CharacterSkillDefinition definition = runtime.Definition;
        if (definition == null || characterCtrl == null ||
            (characterCtrl.CharacterStat != null && !characterCtrl.CharacterStat.TrySpendMana(definition.ManaCost)))
        {
            castingRoutine = null;
            yield break;
        }

        currentCastingRuntime = runtime;
        runtime.StartCooldown(GetCooldownDuration(runtime, definition));

        if (cancelBasicAttackOnCast && runtime != BasicAttackRuntime)
            characterCtrl.CharacterCombatController?.CancelAttack(force: true);

        Transform target = CharacterSkillTargeting.FindTarget(characterCtrl);
        Vector2 aimDirection = CharacterSkillTargeting.GetAimDirection(characterCtrl, target);
        pendingAimDirection = aimDirection;
        pendingTarget = target;
        currentCastEffectsExecuted = false;

        facing?.FaceCastDirection(aimDirection);
        animationDriver?.PlayCastAnimation(definition);
        CharacterSkillFeedbackPlayer.PlayCastFeedback(characterCtrl, definition, aimDirection);

        if (definition.LockMovementWhileCasting)
            SetMovementLocked(true);

        if (ShouldExecuteOnAnimationHit(definition))
        {
            waitingForAnimationHit = true;
            float fallbackDelay = Mathf.Max(definition.CastTime, AnimationEventFallbackDelay);
            float elapsed = 0f;

            while (!currentCastEffectsExecuted && elapsed < fallbackDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            ExecuteCurrentCastEffects();
        }
        else
        {
            if (definition.CastTime > 0f)
                yield return new WaitForSeconds(definition.CastTime);

            ExecuteCurrentCastEffects();
        }

        float holdTime = definition.CastType == CharacterSkillCastType.Duration
            ? definition.Duration
            : 0f;

        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        SetMovementLocked(false);
        castingRoutine = null;
        currentCastingRuntime = null;
        ClearPendingCast();
    }

    public bool OnAttackHitAnimationEvent()
    {
        if (!waitingForAnimationHit)
        {
            return animationDriver != null && animationDriver.ConsumeAttackHitAnimationEvent();
        }

        ExecuteCurrentCastEffects();
        return true;
    }

    public bool PlaySkillAttackAnimation()
    {
        return animationDriver != null && animationDriver.PlaySkillAttackAnimation();
    }

    protected virtual void ExecuteEffects(CharacterSkillRuntime runtime, Vector2 aimDirection, Transform target)
    {
        CharacterSkillDefinition definition = runtime.Definition;
        CharacterSkillExecutionContext context = new(this, runtime, aimDirection, target);

        foreach (CharacterSkillEffectDefinition effect in definition.Effects)
        {
            if (effect == null) continue;

            effect.Execute(context);
        }
    }

    private void ExecuteCurrentCastEffects()
    {
        if (currentCastEffectsExecuted || currentCastingRuntime == null)
            return;

        currentCastEffectsExecuted = true;
        waitingForAnimationHit = false;
        ExecuteEffects(currentCastingRuntime, pendingAimDirection, pendingTarget);
    }

    private static bool ShouldExecuteOnAnimationHit(CharacterSkillDefinition definition)
    {
        return definition != null &&
               definition.ExecuteEffectsOnAnimationHit &&
               !string.IsNullOrWhiteSpace(definition.TriggerName);
    }

    private void ClearPendingCast()
    {
        pendingAimDirection = Vector2.zero;
        pendingTarget = null;
        waitingForAnimationHit = false;
        currentCastEffectsExecuted = false;
    }

    protected virtual void SetMovementLocked(bool locked)
    {
        if (characterCtrl != null && characterCtrl.Rb != null && locked)
            characterCtrl.Rb.linearVelocity = Vector2.zero;
    }

    private CharacterSkillDefinition GetCurrentDefinition()
    {
        return currentCastingRuntime != null ? currentCastingRuntime.Definition : null;
    }

    protected void RebuildRuntimeSkills()
    {
        loadout.Rebuild(basicAttack, equippedSkills, specialSkill);
    }

    private float GetCooldownDuration(CharacterSkillRuntime runtime, CharacterSkillDefinition definition)
    {
        float cooldownDuration = definition != null ? definition.Cooldown : 0f;
        if (runtime != BasicAttackRuntime)
            return cooldownDuration;

        StatValue attackSpeed = characterCtrl != null && characterCtrl.CharacterStat != null
            ? characterCtrl.CharacterStat.GetStat(StatType.AttackSpeed)
            : null;

        float multiplier = 1f + (attackSpeed != null ? attackSpeed.FinalValue : 0f);
        return cooldownDuration / Mathf.Max(MinAttackSpeedMultiplier, multiplier);
    }

    private bool TryFallbackBasicAttack()
    {
        if (IsCasting) return false;
        if (characterCtrl == null || characterCtrl.CharacterCombatController == null) return false;
        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsDead) return false;
        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsHitStunned) return false;

        characterCtrl.CharacterCombatController.Attack();
        return true;
    }
}
