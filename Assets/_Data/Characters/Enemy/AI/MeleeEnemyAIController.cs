using UnityEngine;

public class MeleeEnemyAIController : EnemyAIController
{
    [Header("Melee Ranges")]
    [SerializeField] private float desiredAttackDistance = 0.75f;
    [SerializeField] private float attackHorizontalRange = 1.05f;
    [SerializeField] private float attackVerticalRange = 0.35f;

    [Header("Melee Decision")]
    [SerializeField] private float minStateTime = 0.15f;
    [SerializeField] private int maxAttackersPerTarget = 2;

    [Header("Equipped Attack Skill")]
    [Tooltip("Set to an equipped-skill index to use it after the configured number of basic attacks; -1 uses only the basic attack.")]
    [SerializeField] private int equippedAttackSkillIndex = -1;
    [SerializeField, Min(1)] private int basicAttacksBeforeEquippedSkill = 2;
    [SerializeField, Range(0f, 1f)] private float equippedAttackSkillChance = 0.35f;

    [Header("Melee Debug")]
    [SerializeField] private EnemyState state = EnemyState.Idle;

    private readonly EnemyAttackSlotCoordinator attackSlots = new();
    private float stateEnterTime;
    private int basicAttacksSinceEquippedSkill;
    private float defaultDesiredAttackDistance;
    private float defaultAttackHorizontalRange;

    protected override void Awake()
    {
        base.Awake();
        defaultDesiredAttackDistance = desiredAttackDistance;
        defaultAttackHorizontalRange = attackHorizontalRange;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        state = EnemyState.Idle;
        stateEnterTime = Time.time;
        basicAttacksSinceEquippedSkill = 0;
    }

    protected override void OnDisable()
    {
        attackSlots.Release();
        base.OnDisable();
    }

    protected override void UpdateBrain()
    {
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
        }
    }

    protected override void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
        base.HandleDeath();
    }

    private void UpdateIdle()
    {
        movement.Stop();

        if (target != null)
            ChangeState(EnemyState.Chase);
    }

    private void UpdateChase()
    {
        if (!HasUsableTarget())
        {
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

        if (!HasUsableTarget())
        {
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

        if (Time.time >= nextAttackTime && !IsAttacking() &&
            (characterCtrl.CharacterSkillController == null || !characterCtrl.CharacterSkillController.IsCasting))
        {
            if (basicAttacksSinceEquippedSkill >= basicAttacksBeforeEquippedSkill && TryCastEquippedAttack())
            {
                basicAttacksSinceEquippedSkill = 0;
            }
            else
            {
                TryAttack();
                basicAttacksSinceEquippedSkill++;
            }
        }
    }

    private void ChangeState(EnemyState nextState)
    {
        if (state == nextState) return;

        if (state == EnemyState.Attack && nextState != EnemyState.Attack)
            attackSlots.Release();

        state = nextState;
        stateEnterTime = Time.time;
    }

    private Vector2 GetAttackPosition()
    {
        Vector2 selfPosition = characterCtrl.Rb.position;
        Vector2 targetPosition = target.position;
        float side = selfPosition.x <= targetPosition.x ? -1f : 1f;

        return new Vector2(targetPosition.x + side * desiredAttackDistance, targetPosition.y);
    }

    private bool IsTargetInAttackWindow()
    {
        if (target == null) return false;

        Vector2 delta = (Vector2)target.position - (Vector2)characterCtrl.transform.position;
        bool closeEnoughX = Mathf.Abs(delta.x) <= attackHorizontalRange;
        bool alignedEnoughY = Mathf.Abs(delta.y) <= attackVerticalRange;
        bool targetInFront = Mathf.Sign(delta.x) == Mathf.Sign(GetHorizontalLookDirection().x);

        return closeEnoughX && alignedEnoughY && targetInFront;
    }

    private bool TryReserveAttackSlot()
    {
        CharacterDamReceiver receiver = GetTargetReceiver();
        if (receiver == null)
            return CharacterSkillAfterimageDecoy.IsValidTarget(target);

        return attackSlots.TryReserve(receiver, maxAttackersPerTarget);
    }

    private bool CanChangeState()
    {
        return Time.time >= stateEnterTime + minStateTime;
    }

    private bool TryCastEquippedAttack()
    {
        if (equippedAttackSkillIndex < 0)
            return false;

        if (Random.value > equippedAttackSkillChance)
            return false;

        CharacterSkillController skillController = characterCtrl.CharacterSkillController;
        if (skillController == null || skillController.GetSkill(equippedAttackSkillIndex) == null)
            return false;

        if (!skillController.TryCast(equippedAttackSkillIndex))
            return false;

        nextAttackTime = Time.time + attackCooldown;
        return true;
    }

    public void ConfigureCombatDistances(float nextDesiredAttackDistance, float nextAttackHorizontalRange)
    {
        desiredAttackDistance = Mathf.Max(0f, nextDesiredAttackDistance);
        attackHorizontalRange = Mathf.Max(0.01f, nextAttackHorizontalRange);
    }

    public void RestoreDefaultCombatDistances()
    {
        desiredAttackDistance = defaultDesiredAttackDistance;
        attackHorizontalRange = defaultAttackHorizontalRange;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(attackHorizontalRange * 2f, attackVerticalRange * 2f, 0f)
        );
    }
}
