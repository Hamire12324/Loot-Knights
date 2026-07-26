using UnityEngine;
using System.Collections.Generic;

public class EnemyAIController : CharacterAbstract
{
    private static readonly Dictionary<CharacterDamReceiver, int> AttackSlotsByTarget = new();

    [SerializeField] private EnemyMovement movement;

    [Header("Ranges")]
    [SerializeField] private float desiredAttackDistance = 0.75f;
    [SerializeField] private float attackHorizontalRange = 1.05f;
    [SerializeField] private float attackVerticalRange = 0.35f;
    [SerializeField] private float loseRange = 8f;

    [Header("Decision")]
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float minStateTime = 0.15f;
    [SerializeField] private int maxAttackersPerTarget = 2;

    [Header("Debug")]
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private Transform target;

    private float nextTargetRefreshTime;
    private float nextAttackTime;
    private float stateEnterTime;
    private CharacterDamReceiver reservedAttackTarget;

    protected override void OnEnable()
    {
        base.OnEnable();
        ChangeState(EnemyState.Idle);
    }

    protected override void OnDisable()
    {
        ReleaseAttackSlot();
        base.OnDisable();
    }

    protected override void Update()
    {
        if (movement == null || characterCtrl == null) return;

        if (characterCtrl.CharacterDamReceiver != null && characterCtrl.CharacterDamReceiver.IsDead)
        {
            ChangeState(EnemyState.Dead);
        }

        RefreshTarget();

        switch (state)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Attack:
                UpdateAttack();
                break;

            case EnemyState.Hit:
                movement.Stop();
                break;

            case EnemyState.Dead:
                movement.Stop();
                enabled = false;
                break;
        }
    }


    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadMovement();
    }

    protected virtual void LoadMovement()
    {
        if (this.movement != null) return;
        this.movement = characterCtrl.CharacterMovement as EnemyMovement;
        if (this.movement != null) return;
        this.movement = GetComponentInChildren<EnemyMovement>();
        Debug.Log(transform.name + ": LoadMovement", gameObject);
    }
    private void UpdateIdle()
    {
        movement.Stop();

        if (target != null)
            ChangeState(EnemyState.Chase);
    }

    private void UpdateChase()
    {
        if (!IsTargetValid())
        {
            ClearTarget();
            ChangeState(EnemyState.Idle);
            return;
        }

        float distance = GetTargetDistance();

        if (distance > loseRange)
        {
            ClearTarget();
            ChangeState(EnemyState.Idle);
            return;
        }

        if (IsTargetInAttackWindow() && CanChangeState() && TryReserveAttackSlot())
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        movement.MoveTo(GetAttackPosition());
    }

    private void UpdateAttack()
    {
        movement.Stop();

        if (!IsTargetValid())
        {
            ClearTarget();
            ChangeState(EnemyState.Idle);
            return;
        }

        if (!IsTargetInAttackWindow())
        {
            if (!IsAttacking())
                ChangeState(EnemyState.Chase);

            return;
        }

        FaceTarget();

        if (Time.time >= nextAttackTime && !IsAttacking())
        {
            nextAttackTime = Time.time + attackCooldown;
            if (characterCtrl.CharacterSkillController == null ||
                !characterCtrl.CharacterSkillController.TryCastBasicAttack())
            {
                characterCtrl.CharacterCombatController?.Attack();
            }
        }
    }

    private void RefreshTarget()
    {
        if (Time.time < nextTargetRefreshTime) return;

        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        if (IsTargetValid() && GetTargetDistance() <= loseRange) return;

        target = characterCtrl.CharacterTargetFinder != null
            ? characterCtrl.CharacterTargetFinder.FindClosestTarget()
            : null;

        characterCtrl.CharacterTargetFinder?.SetTarget(target);
    }

    private void ClearTarget()
    {
        target = null;
        characterCtrl.CharacterTargetFinder?.SetTarget(null);
    }

    private float GetTargetDistance()
    {
        if (target == null) return Mathf.Infinity;

        return Vector2.Distance(characterCtrl.transform.position, target.position);
    }

    private bool IsTargetValid()
    {
        return GetTargetReceiver() != null;
    }

    private CharacterDamReceiver GetTargetReceiver()
    {
        if (target == null || characterCtrl == null) return null;

        CharacterCtrl targetCtrl = target.GetComponentInParent<CharacterCtrl>();
        if (targetCtrl == null) return null;
        if (targetCtrl.CharacterDamReceiver == null || targetCtrl.CharacterDamReceiver.IsDead) return null;
        if (!FactionManager.CanAttack(characterCtrl.Faction, targetCtrl.Faction)) return null;

        return targetCtrl.CharacterDamReceiver;
    }

    private Vector2 GetAttackPosition()
    {
        Vector2 selfPosition = characterCtrl.Rb.position;
        Vector2 targetPosition = target.position;

        float side = selfPosition.x <= targetPosition.x ? -1f : 1f;
        float x = targetPosition.x + side * desiredAttackDistance;

        return new Vector2(x, targetPosition.y);
    }

    private bool IsTargetInAttackWindow()
    {
        if (target == null) return false;

        Vector2 selfPosition = characterCtrl.transform.position;
        Vector2 targetPosition = target.position;
        Vector2 delta = targetPosition - selfPosition;

        bool closeEnoughX = Mathf.Abs(delta.x) <= attackHorizontalRange;
        bool alignedEnoughY = Mathf.Abs(delta.y) <= attackVerticalRange;
        bool targetInFront = Mathf.Sign(delta.x) == Mathf.Sign(GetHorizontalLookDirection().x);

        return closeEnoughX && alignedEnoughY && targetInFront;
    }

    private void FaceTarget()
    {
        if (target == null) return;

        movement.SetLookDirection(GetHorizontalLookDirection());
    }

    private Vector2 GetHorizontalLookDirection()
    {
        if (target == null) return movement.LookDirection.x < 0f ? Vector2.left : Vector2.right;

        return target.position.x < characterCtrl.transform.position.x
            ? Vector2.left
            : Vector2.right;
    }

    private bool IsAttacking()
    {
        return characterCtrl.CharacterCombatController != null &&
               characterCtrl.CharacterCombatController.IsAttacking;
    }

    private void ChangeState(EnemyState nextState)
    {
        if (state == nextState) return;

        if (state == EnemyState.Attack && nextState != EnemyState.Attack)
            ReleaseAttackSlot();

        state = nextState;
        stateEnterTime = Time.time;
    }

    private bool TryReserveAttackSlot()
    {
        CharacterDamReceiver receiver = GetTargetReceiver();
        if (receiver == null) return false;

        if (reservedAttackTarget == receiver)
            return true;

        ReleaseAttackSlot();

        int maxAttackers = Mathf.Max(1, maxAttackersPerTarget);
        AttackSlotsByTarget.TryGetValue(receiver, out int currentCount);

        if (currentCount >= maxAttackers)
            return false;

        AttackSlotsByTarget[receiver] = currentCount + 1;
        reservedAttackTarget = receiver;
        receiver.OnDeath += HandleReservedTargetDeath;
        return true;
    }

    private void ReleaseAttackSlot()
    {
        if (reservedAttackTarget == null) return;

        reservedAttackTarget.OnDeath -= HandleReservedTargetDeath;

        if (AttackSlotsByTarget.TryGetValue(reservedAttackTarget, out int currentCount))
        {
            currentCount--;

            if (currentCount <= 0)
                AttackSlotsByTarget.Remove(reservedAttackTarget);
            else
                AttackSlotsByTarget[reservedAttackTarget] = currentCount;
        }

        reservedAttackTarget = null;
    }

    private void HandleReservedTargetDeath(CharacterDamReceiver receiver)
    {
        ReleaseAttackSlot();
    }

    private bool CanChangeState()
    {
        return Time.time >= stateEnterTime + minStateTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(attackHorizontalRange * 2f, attackVerticalRange * 2f, 0f)
        );

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
