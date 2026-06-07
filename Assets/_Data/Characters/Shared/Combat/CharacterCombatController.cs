using UnityEngine;

public class CharacterCombatController : CharacterAbstract
{
    private const string AttackStateName = "Attack";

    [Header("Attack")]
    [SerializeField] protected float attackDuration = 0.8f;
    [SerializeField] protected bool canAttackBeInterrupted = true;

    [SerializeField] protected bool isAttacking;
    public bool IsAttacking => isAttacking;

    private Vector3 originalScale;
    private float attackEndTime;
    private bool attackFacingOverrideActive;

    protected override void Awake()
    {
        base.Awake();

        originalScale = characterCtrl.transform.localScale;
    }

    protected override void Update()
    {
        base.Update();

        if (!isAttacking) return;
        if (Time.time < attackEndTime) return;

        EndAttack();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (!attackFacingOverrideActive) return;
        if (IsAttackVisualActive()) return;

        RestoreOriginalScale();
    }

    public virtual void Attack()
    {
        if (isAttacking) return;
        if (characterCtrl.CharacterDamReceiver != null &&
            characterCtrl.CharacterDamReceiver.IsHitStunned)
        {
            return;
        }

        Transform target =
            characterCtrl.CharacterTargetFinder.FindClosestTarget();

        isAttacking = true;
        attackEndTime = Time.time + Mathf.Max(attackDuration, 0.01f);

        FaceAttackDirection(target);
        attackFacingOverrideActive = true;
        characterCtrl.CharacterTargetFinder.SetTarget(target);

        characterCtrl.CharacterAnimation.PlayAttackAnimation();
    }

    protected virtual void FaceAttackDirection(Transform target)
    {
        if (target == null)
        {
            FaceHorizontalDirection(characterCtrl.CharacterMovement.LookDirection.x);
            return;
        }

        float dir =
            target.position.x - characterCtrl.transform.position.x;

        FaceHorizontalDirection(dir);
    }

    public virtual void EndAttack()
    {
        if (!isAttacking) return;

        isAttacking = false;

        characterCtrl.CharacterDamSender?.DisableHitbox();

        if (!attackFacingOverrideActive)
            RestoreOriginalScale();
    }

    public virtual void OnAttackHitAnimationEvent()
    {
        characterCtrl.CharacterVFXController?.PlayAttackVFX();

        CharacterDamSender damageSender = characterCtrl.CharacterDamSender;
        if (damageSender == null)
            return;

        damageSender.EnableHitbox();
        damageSender.DealHitboxDamage();
        damageSender.DisableHitbox();
    }

    public virtual void CancelAttack(bool force = false)
    {
        if (!isAttacking) return;
        if (!force && !canAttackBeInterrupted) return;

        isAttacking = false;

        characterCtrl.CharacterDamSender?.DisableHitbox();

        RestoreOriginalScale();
    }

    private void RestoreOriginalScale()
    {
        characterCtrl.transform.localScale = originalScale;
        attackFacingOverrideActive = false;
    }

    private bool IsAttackVisualActive()
    {
        if (characterCtrl.Animator == null) return false;

        if (!characterCtrl.Animator.IsInTransition(0))
            return characterCtrl.Animator.GetCurrentAnimatorStateInfo(0).IsName(AttackStateName);

        return characterCtrl.Animator.GetNextAnimatorStateInfo(0).IsName(AttackStateName);
    }

    protected virtual void FaceHorizontalDirection(float dir)
    {
        if (Mathf.Abs(dir) <= 0.01f) return;

        Vector3 scale = originalScale;

        scale.x = dir >= 0
            ? Mathf.Abs(scale.x)
            : -Mathf.Abs(scale.x);

        characterCtrl.transform.localScale = scale;
    }
}
