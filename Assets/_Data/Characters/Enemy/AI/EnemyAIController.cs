using UnityEngine;

public abstract class EnemyAIController : CharacterAbstract
{
    [SerializeField] protected EnemyMovement movement;

    [Header("Targeting")]
    [SerializeField] protected float loseRange = 8f;
    [SerializeField] protected float targetRefreshInterval = 0.25f;

    [Header("Combat")]
    [SerializeField] protected float attackCooldown = 1.4f;

    [Header("Debug")]
    [SerializeField] protected Transform target;

    protected float nextTargetRefreshTime;
    protected float nextAttackTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        nextTargetRefreshTime = 0f;
        nextAttackTime = 0f;
        ClearTarget();
    }

    protected override void OnDisable()
    {
        movement?.Stop();
        base.OnDisable();
    }

    protected override void Update()
    {
        if (!CanUpdate()) return;

        RefreshTarget();
        UpdateBrain();
    }

    protected abstract void UpdateBrain();

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadMovement();
    }

    protected virtual void LoadMovement()
    {
        if (movement != null) return;

        movement = characterCtrl.CharacterMovement as EnemyMovement;
        if (movement != null) return;

        movement = GetComponentInChildren<EnemyMovement>();
        Debug.Log(transform.name + ": LoadMovement", gameObject);
    }

    protected virtual bool CanUpdate()
    {
        if (movement == null || characterCtrl == null) return false;

        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsDead)
        {
            HandleDeath();
            return false;
        }

        return true;
    }

    protected virtual void HandleDeath()
    {
        movement.Stop();
        enabled = false;
    }

    protected bool HasUsableTarget()
    {
        if (IsTargetValid() && GetTargetDistance() <= loseRange)
            return true;

        ClearTarget();
        return false;
    }

    protected void RefreshTarget()
    {
        if (Time.time < nextTargetRefreshTime) return;

        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        Transform decoyTarget = CharacterSkillAfterimageDecoy.FindClosest(characterCtrl.transform.position, loseRange);
        if (decoyTarget != null && target != decoyTarget)
        {
            SetTarget(decoyTarget);
            return;
        }

        if (IsTargetValid() && GetTargetDistance() <= loseRange) return;

        SetTarget(characterCtrl.CharacterTargetFinder != null
            ? characterCtrl.CharacterTargetFinder.FindClosestTarget()
            : null);
    }

    protected void SetTarget(Transform nextTarget)
    {
        target = nextTarget;
        characterCtrl?.CharacterTargetFinder?.SetTarget(target);
    }

    protected void ClearTarget()
    {
        SetTarget(null);
    }

    protected float GetTargetDistance()
    {
        return target == null
            ? Mathf.Infinity
            : Vector2.Distance(characterCtrl.transform.position, target.position);
    }

    protected bool IsTargetValid()
    {
        return CharacterSkillAfterimageDecoy.IsValidTarget(target) || GetTargetReceiver() != null;
    }

    protected CharacterDamReceiver GetTargetReceiver()
    {
        if (target == null || characterCtrl == null) return null;

        CharacterCtrl targetCtrl = target.GetComponentInParent<CharacterCtrl>();
        if (targetCtrl == null || targetCtrl.CharacterDamReceiver == null) return null;
        if (targetCtrl.CharacterDamReceiver.IsDead) return null;
        if (!FactionManager.CanAttack(characterCtrl.Faction, targetCtrl.Faction)) return null;

        return targetCtrl.CharacterDamReceiver;
    }

    protected void TryAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        CharacterSkillController skillController = characterCtrl.CharacterSkillController;
        if (skillController != null && skillController.BasicAttackRuntime != null)
        {
            skillController.TryCastBasicAttack();
            return;
        }

        characterCtrl.CharacterCombatController?.Attack();
    }

    protected void FaceTarget()
    {
        if (target != null)
            movement.SetLookDirection(GetHorizontalLookDirection());
    }

    protected bool IsAttacking()
    {
        return characterCtrl.CharacterCombatController != null &&
               characterCtrl.CharacterCombatController.IsAttacking;
    }

    protected Vector2 GetHorizontalLookDirection()
    {
        if (target == null)
            return movement.LookDirection.x < 0f ? Vector2.left : Vector2.right;

        return target.position.x < characterCtrl.transform.position.x
            ? Vector2.left
            : Vector2.right;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
