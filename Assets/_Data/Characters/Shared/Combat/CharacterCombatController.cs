using UnityEngine;

public class CharacterCombatController : CharacterAbstract
{
    private const float MinAttackSpeedMultiplier = 0.1f;

    [Header("Attack")]
    [SerializeField] protected float attackDuration = 0.8f;
    [SerializeField] protected bool canAttackBeInterrupted = true;
    [SerializeField] protected bool isAttacking;

    public bool IsAttacking => isAttacking;

    private Vector3 originalScale;
    private float attackEndTime;

    protected override void Awake()
    {
        base.Awake();
        originalScale = characterCtrl.transform.localScale;
    }

    protected override void Update()
    {
        base.Update();

        if (isAttacking && Time.time >= attackEndTime)
            EndAttack();
    }

    public virtual void Attack()
    {
        if (isAttacking || IsHitStunned()) return;

        Transform target =
            characterCtrl.CharacterTargetFinder?.FindClosestTarget();

        isAttacking = true;
        attackEndTime = Time.time + GetScaledAttackDuration();

        FaceAttackDirection(target);
        characterCtrl.CharacterTargetFinder?.SetTarget(target);
        characterCtrl.CharacterAnimation?.PlayAttackAnimation();
    }

    public virtual void EndAttack()
    {
        isAttacking = false;
        characterCtrl.CharacterDamSender?.DisableHitbox();
    }

    public virtual void CancelAttack(bool force = false)
    {
        if (isAttacking && !force && !canAttackBeInterrupted) return;

        EndAttack();
    }

    public virtual void OnAttackHitAnimationEvent()
    {
        characterCtrl.CharacterVFXController?.PlayAttackVFX();

        CharacterDamSender damageSender = characterCtrl.CharacterDamSender;
        if (damageSender == null) return;

        damageSender.EnableHitbox();
        damageSender.DealHitboxDamage();
        damageSender.DisableHitbox();
    }

    protected virtual void FaceAttackDirection(Transform target)
    {
        float direction = target != null
            ? target.position.x - characterCtrl.transform.position.x
            : characterCtrl.CharacterMovement.LookDirection.x;

        FaceHorizontalDirection(direction);
    }

    protected virtual void FaceHorizontalDirection(float direction)
    {
        if (Mathf.Abs(direction) <= 0.01f) return;

        direction = Mathf.Sign(direction);

        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(scale.x) * direction;

        characterCtrl.transform.localScale = scale;
        characterCtrl.CharacterMovement?.SetLookDirection(
            new Vector2(direction, 0f)
        );
    }

    private bool IsHitStunned()
    {
        return characterCtrl.CharacterDamReceiver != null &&
               characterCtrl.CharacterDamReceiver.IsHitStunned;
    }

    private float GetScaledAttackDuration()
    {
        StatValue attackSpeed =
            characterCtrl.CharacterStat?.GetStat(StatType.AttackSpeed);

        float multiplier =
            1f + (attackSpeed?.FinalValue ?? 0f);

        return Mathf.Max(attackDuration, 0.01f) /
               Mathf.Max(multiplier, MinAttackSpeedMultiplier);
    }
}
