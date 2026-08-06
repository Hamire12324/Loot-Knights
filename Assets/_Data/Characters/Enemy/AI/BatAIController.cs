using UnityEngine;

public class BatAIController : EnemyAIController
{
    private enum BatState
    {
        Idle,
        Orbit,
        Dive,
        Bite,
        Retreat,
        Dead
    }

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 1.8f;
    [SerializeField] private float orbitAngularSpeed = 180f;
    [SerializeField] private float orbitPointLead = 0.35f;
    [SerializeField] private float orbitHeightWave = 0.25f;
    [SerializeField] private float orbitHeightSpeed = 5f;
    [SerializeField] private float preDiveTime = 1.1f;

    [Header("Dive Bite")]
    [SerializeField] private float biteDistance = 0.45f;
    [SerializeField] private float diveSpeedMultiplier = 2.2f;
    [SerializeField] private float diveTimeout = 0.7f;

    [Header("Blood Drain")]
    [SerializeField, Range(0f, 1f)] private float bloodDrainHealthThreshold = 0.75f;

    [Header("Retreat")]
    [SerializeField] private float retreatDistance = 2.4f;
    [SerializeField] private float retreatSpeedMultiplier = 1.4f;
    [SerializeField] private float retreatTimeout = 0.55f;

    [Header("Bat Debug")]
    [SerializeField] private BatState batState = BatState.Idle;

    private float orbitAngle;
    private float batStateEnterTime;
    private Vector2 retreatPosition;

    protected override void OnEnable()
    {
        base.OnEnable();
        orbitAngle = Random.Range(0f, 360f);
        batState = BatState.Idle;
        batStateEnterTime = Time.time;
    }

    protected override void UpdateBrain()
    {
        switch (batState)
        {
            case BatState.Idle:
                UpdateBatIdle();
                break;

            case BatState.Orbit:
                UpdateBatOrbit();
                break;

            case BatState.Dive:
                UpdateBatDive();
                break;

            case BatState.Bite:
                UpdateBatBite();
                break;

            case BatState.Retreat:
                UpdateBatRetreat();
                break;

            case BatState.Dead:
                movement.Stop();
                enabled = false;
                break;
        }
    }

    protected override void HandleDeath()
    {
        ChangeBatState(BatState.Dead);
        base.HandleDeath();
    }

    private void UpdateBatIdle()
    {
        movement.Stop();

        if (target != null)
            ChangeBatState(BatState.Orbit);
    }

    private void UpdateBatOrbit()
    {
        if (!EnsureTarget())
            return;

        movement.SetSpeedMultiplier(1f);
        movement.MoveTo(GetOrbitPosition());
        FaceTarget();

        if (Time.time >= nextAttackTime && Time.time >= batStateEnterTime + preDiveTime)
            ChangeBatState(BatState.Dive);
    }

    private void UpdateBatDive()
    {
        if (!EnsureTarget())
            return;

        movement.SetSpeedMultiplier(diveSpeedMultiplier);
        movement.MoveTo(target.position);
        FaceTarget();

        if (GetTargetDistance() <= biteDistance)
        {
            if (!TryCastBloodDrain())
                TryAttack();

            ChangeBatState(BatState.Bite);
            return;
        }

        if (Time.time >= batStateEnterTime + diveTimeout)
            BeginRetreat();
    }

    private void UpdateBatBite()
    {
        movement.Stop();
        FaceTarget();

        if (!IsExecutingAttack())
            BeginRetreat();
    }

    private void UpdateBatRetreat()
    {
        if (!EnsureTarget())
            return;

        movement.SetSpeedMultiplier(retreatSpeedMultiplier);
        movement.MoveTo(retreatPosition);

        if (movement.HasArrived || Time.time >= batStateEnterTime + retreatTimeout)
            ChangeBatState(BatState.Orbit);
    }

    private void BeginRetreat()
    {
        if (target == null)
        {
            ChangeBatState(BatState.Idle);
            return;
        }

        Vector2 away = ((Vector2)characterCtrl.transform.position - (Vector2)target.position).normalized;
        if (away.sqrMagnitude <= 0.01f)
            away = movement.LookDirection.sqrMagnitude > 0.01f ? movement.LookDirection : Vector2.right;

        retreatPosition = (Vector2)target.position + away * retreatDistance;
        nextAttackTime = Time.time + attackCooldown;
        ChangeBatState(BatState.Retreat);
    }

    private bool TryCastBloodDrain()
    {
        if (!ShouldUseBloodDrain())
            return false;

        CharacterSkillController skillController = characterCtrl.CharacterSkillController;
        if (skillController != null && skillController.GetSkill(0) != null)
            return skillController.TryCast(0);

        return false;
    }

    private bool ShouldUseBloodDrain()
    {
        CharacterStat stats = characterCtrl != null ? characterCtrl.CharacterStat : null;
        float maxHealth = stats?.MaxHealth?.FinalValue ?? 0f;
        if (maxHealth <= 0f)
            return false;

        return stats.CurrentHealth / maxHealth <= bloodDrainHealthThreshold;
    }

    private bool EnsureTarget()
    {
        if (HasUsableTarget()) return true;

        ChangeBatState(BatState.Idle);
        return false;
    }

    private bool IsExecutingAttack()
    {
        return IsAttacking() ||
               (characterCtrl.CharacterSkillController != null &&
                (characterCtrl.CharacterSkillController.IsCasting ||
                 characterCtrl.CharacterSkillController.IsAttackVisualActive));
    }

    private Vector2 GetOrbitPosition()
    {
        orbitAngle += orbitAngularSpeed * Time.deltaTime;
        float radians = orbitAngle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
        offset.y += Mathf.Sin(Time.time * orbitHeightSpeed) * orbitHeightWave;

        Vector2 targetPosition = target.position;
        Vector2 toCurrent = ((Vector2)characterCtrl.transform.position - targetPosition).normalized;
        if (toCurrent.sqrMagnitude > 0.01f)
            offset += toCurrent * orbitPointLead;

        return targetPosition + offset;
    }

    private void ChangeBatState(BatState nextState)
    {
        if (batState == nextState) return;

        batState = nextState;
        batStateEnterTime = Time.time;

        if (nextState != BatState.Dive && nextState != BatState.Retreat)
            movement?.SetSpeedMultiplier(1f);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, biteDistance);
    }
}
