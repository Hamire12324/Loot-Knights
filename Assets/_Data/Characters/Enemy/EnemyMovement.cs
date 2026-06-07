using UnityEngine;

public class EnemyMovement : CharacterMovement
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private float stoppingDistance = 0.15f;
    [SerializeField] private float arriveSlowdownDistance = 1f;

    [Header("Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 0.7f;
    [SerializeField] private float obstacleAvoidWeight = 1.5f;

    [Header("Separation")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 0.6f;
    [SerializeField] private float separationWeight = 1.2f;

    [SerializeField] private Vector2 targetPosition;
    public Vector2 TargetPosition => targetPosition;

    [SerializeField] private bool hasTarget;
    public bool HasTarget => hasTarget;
    [SerializeField] private Vector2 desiredVelocity;
    public bool HasArrived => 
        hasTarget && Vector2.Distance(characterCtrl.Rb.position, targetPosition) <= stoppingDistance;
    protected override void FixedUpdate()
    {
        if (characterCtrl == null || characterCtrl.Rb == null) return;

        if (characterCtrl.CharacterDamReceiver != null &&
            characterCtrl.CharacterDamReceiver.IsHitStunned)
        {
            Stop();
            characterCtrl.Rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdateMoveInput();
        ApplyMovement();
        UpdateLookDirection();
    }

    public void MoveTo(Vector2 worldPosition)
    {
        targetPosition = worldPosition;
        hasTarget = true;
    }

    public void MoveDirection(Vector2 direction)
    {
        hasTarget = false;
        moveInput = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.zero;
    }

    public void Stop()
    {
        hasTarget = false;
        moveInput = Vector2.zero;
        desiredVelocity = Vector2.zero;
    }

    public void ConfigureObstacleLayer(LayerMask layerMask)
    {
        obstacleLayer = layerMask;
    }

    private void UpdateMoveInput()
    {
        if (!hasTarget) return;

        Vector2 currentPosition = characterCtrl.Rb.position;
        Vector2 toTarget = targetPosition - currentPosition;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 direction = toTarget.normalized;
        float speedFactor = Mathf.Clamp01(distance / Mathf.Max(arriveSlowdownDistance, stoppingDistance));

        Vector2 separation = GetSeparationDirection();
        Vector2 obstacleAvoidance = GetObstacleAvoidance(direction);

        Vector2 finalDirection =
            direction +
            separation * separationWeight +
            obstacleAvoidance * obstacleAvoidWeight;

        moveInput = finalDirection.sqrMagnitude > 0.01f
            ? finalDirection.normalized
            : direction;

        moveInput *= speedFactor;
    }

    private void ApplyMovement()
    {
        desiredVelocity = moveInput * moveSpeed;
        float rate = desiredVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration;

        characterCtrl.Rb.linearVelocity = Vector2.MoveTowards(
            characterCtrl.Rb.linearVelocity,
            desiredVelocity,
            rate * Time.fixedDeltaTime
        );
    }

    private void UpdateLookDirection()
    {
        if (moveInput.sqrMagnitude > 0.01f)
            SetLookDirection(moveInput);
    }

    private Vector2 GetSeparationDirection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            characterCtrl.Rb.position,
            separationRadius,
            enemyLayer
        );

        Vector2 separation = Vector2.zero;

        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody == characterCtrl.Rb) continue;

            Vector2 away = characterCtrl.Rb.position - (Vector2)hit.transform.position;
            float distance = away.magnitude;

            if (distance > 0.01f)
                separation += away.normalized / distance;
        }

        return separation;
    }

    private Vector2 GetObstacleAvoidance(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            characterCtrl.Rb.position,
            direction,
            obstacleCheckDistance,
            obstacleLayer
        );

        if (!hit.collider) return Vector2.zero;

        Vector2 left = new Vector2(-direction.y, direction.x);
        Vector2 right = new Vector2(direction.y, -direction.x);

        bool leftBlocked = Physics2D.Raycast(characterCtrl.Rb.position, left, obstacleCheckDistance, obstacleLayer);
        bool rightBlocked = Physics2D.Raycast(characterCtrl.Rb.position, right, obstacleCheckDistance, obstacleLayer);

        if (!leftBlocked) return left;
        if (!rightBlocked) return right;

        return -direction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)(lookDirection * obstacleCheckDistance)
        );
    }
}
