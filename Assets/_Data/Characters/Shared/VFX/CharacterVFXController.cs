using UnityEngine;

public abstract class CharacterVFXController : CharacterAbstract
{
    private const string DefaultAttackVfxPath = "VFX/Skill_Slash_VFXDefinition";
    private const string DefaultHitVfxPath = "VFX/Hit_Default_VFXDefinition";
    private const string DefaultAttackSfxPath = "SFX/Skill_Slash_SFXDefinition";
    private const string DefaultHitSfxPath = "SFX/Hit_Default_SFXDefinition";

    [Header("Attack VFX")]
    [SerializeField] private VFXDefinition attackVfx;
    [SerializeField] private Transform attackAnchor;
    [SerializeField] private SFXDefinition attackSfx;

    [Header("Hit VFX")]
    [SerializeField] private VFXDefinition defaultHitVfx;
    [SerializeField] private Transform hitAnchor;
    [SerializeField] private bool rotateHitAwayFromAttacker;
    [SerializeField] private bool onlyPlayHitWhenDamagePositive = true;
    [SerializeField] private SFXDefinition defaultHitSfx;

    [Header("Death VFX")]
    [SerializeField] private VFXDefinition defaultDeathVfx;
    [SerializeField] private Transform deathAnchor;
    [SerializeField] private SFXDefinition defaultDeathSfx;
    protected override void OnEnable()
    {
        base.OnEnable();
        Subscribe();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadAnchors();
        this.LoadDefinitions();
    }

    protected virtual void LoadAnchors()
    {
        if (characterCtrl == null) return;

        Transform defaultAnchor = characterCtrl.transform;

        if (hitAnchor == null)
            hitAnchor = defaultAnchor;

        if (attackAnchor == null)
            attackAnchor = defaultAnchor;

        if (deathAnchor == null)
            deathAnchor = defaultAnchor;
    }

    protected virtual void LoadDefinitions()
    {
        if (attackVfx == null)
            attackVfx = Resources.Load<VFXDefinition>(DefaultAttackVfxPath);

        if (defaultHitVfx == null)
            defaultHitVfx = Resources.Load<VFXDefinition>(DefaultHitVfxPath);

        if (attackSfx == null)
            attackSfx = Resources.Load<SFXDefinition>(DefaultAttackSfxPath);

        if (defaultHitSfx == null)
            defaultHitSfx = Resources.Load<SFXDefinition>(DefaultHitSfxPath);
    }

    private void Subscribe()
    {
        CharacterDamReceiver receiver = characterCtrl != null
            ? characterCtrl.CharacterDamReceiver
            : null;

        if (receiver == null)
            return;

        receiver.OnHitDetailed -= HandleHit;
        receiver.OnDeath -= HandleDeath;
        receiver.OnHitDetailed += HandleHit;
        receiver.OnDeath += HandleDeath;
    }

    private void Unsubscribe()
    {
        CharacterDamReceiver receiver = characterCtrl != null
            ? characterCtrl.CharacterDamReceiver
            : null;

        if (receiver == null)
            return;

        receiver.OnHitDetailed -= HandleHit;
        receiver.OnDeath -= HandleDeath;
    }

    public void PlayAttackVFX()
    {
        Transform anchor = GetAttackAnchor();
        PlayDefinition(attackVfx, anchor, GetAttackDirection());
        PlaySfx(attackSfx, anchor);
    }

    private void HandleHit(float damage, Transform attacker, DamageData damageData)
    {
        if (onlyPlayHitWhenDamagePositive && damage <= 0f)
            return;

        bool useDamageOverride = damageData != null && damageData.HitVfx != null;
        VFXDefinition definition = useDamageOverride ? damageData.HitVfx : defaultHitVfx;
        Transform anchor = GetHitAnchor();
        Vector3 direction = rotateHitAwayFromAttacker
            ? GetDirectionAwayFromAttacker(attacker, anchor)
            : Vector3.right;
        Vector3 extraOffset = useDamageOverride ? damageData.HitVfxOffset : Vector3.zero;

        PlayDefinition(definition, anchor, direction, extraOffset);

        bool useSfxOverride = damageData != null && damageData.HitSfx != null;
        SFXDefinition sfx = useSfxOverride ? damageData.HitSfx : defaultHitSfx;
        Vector3 sfxOffset = useSfxOverride ? damageData.HitSfxOffset : Vector3.zero;
        PlaySfx(sfx, anchor, sfxOffset);
    }

    private void HandleDeath(CharacterDamReceiver self)
    {
        Transform anchor = GetDeathAnchor();
        PlayDefinition(defaultDeathVfx, anchor);
        PlaySfx(defaultDeathSfx, anchor);
    }

    private PoolObj PlayDefinition(
        VFXDefinition definition,
        Transform anchor,
        Vector3 direction = default,
        Vector3 extraOffset = default)
    {
        if (definition == null)
            return null;

        Vector3 position = (anchor != null ? anchor.position : transform.position) + extraOffset;

        return VFXManager.InstanceOrNull?.Play(definition, position, direction, anchor);
    }

    private void PlaySfx(
        SFXDefinition definition,
        Transform anchor,
        Vector3 extraOffset = default)
    {
        if (definition == null)
            return;

        Vector3 position = (anchor != null ? anchor.position : transform.position) + extraOffset;
        SFXManager.Play(definition, position);
    }

    private Vector3 GetAttackDirection()
    {
        Transform anchor = GetAttackAnchor();
        Transform target = characterCtrl != null && characterCtrl.CharacterTargetFinder != null
            ? characterCtrl.CharacterTargetFinder.CurrentTarget
            : null;

        if (target != null && anchor != null)
        {
            Vector2 toTarget = target.position - anchor.position;
            if (toTarget.sqrMagnitude > 0.0001f)
                return toTarget.normalized;
        }

        if (characterCtrl != null && characterCtrl.CharacterMovement != null)
        {
            Vector2 lookDirection = characterCtrl.CharacterMovement.LookDirection;
            if (lookDirection.sqrMagnitude > 0.0001f)
                return lookDirection.normalized;
        }

        return Vector3.right;
    }

    private static Vector3 GetDirectionAwayFromAttacker(Transform attacker, Transform anchor)
    {
        if (attacker == null || anchor == null)
            return Vector3.right;

        Vector2 direction = anchor.position - attacker.position;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
    }
    private Transform GetHitAnchor()
    {
        return hitAnchor != null ? hitAnchor : transform;
    }

    private Transform GetAttackAnchor()
    {
        return attackAnchor != null ? attackAnchor : transform;
    }

    private Transform GetDeathAnchor()
    {
        return deathAnchor != null ? deathAnchor : transform;
    }
}
