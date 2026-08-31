using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class VFXProjectileMover : MonoBehaviour
{
    [SerializeField] private bool returnToPoolOnArrival = true;

    private Coroutine moveCoroutine;

    private void OnDisable()
    {
        StopMove();
    }

    public void Play(
        Vector2 direction,
        float distance,
        float speed,
        float rotationOffsetDegrees = 0f,
        Transform homingTarget = null,
        float targetTurnRate = 0f)
    {
        StopMove();

        if (direction == Vector2.zero)
            direction = Vector2.down;

        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);

        moveCoroutine = StartCoroutine(MoveRoutine(
            direction, distance, speed, rotationOffsetDegrees, homingTarget, targetTurnRate));
    }

    private IEnumerator MoveRoutine(
        Vector2 direction, float distance, float speed, float rotationOffsetDegrees,
        Transform homingTarget, float targetTurnRate)
    {
        if (distance <= 0f || speed <= 0f)
        {
            moveCoroutine = null;
            yield break;
        }

        float travelled = 0f;

        while (travelled < distance)
        {
            if (homingTarget != null && targetTurnRate > 0f)
            {
                Vector2 toTarget = (Vector2)homingTarget.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.001f)
                    direction = Vector2.MoveTowards(
                        direction, toTarget.normalized, targetTurnRate / 180f * Time.deltaTime).normalized;
            }

            float step = Mathf.Min(speed * Time.deltaTime, distance - travelled);
            transform.position += (Vector3)(direction * step);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);
            travelled += step;
            yield return null;
        }

        moveCoroutine = null;
        ReturnToPoolIfNeeded();
    }

    private void StopMove()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }

    private void ReturnToPoolIfNeeded()
    {
        if (!returnToPoolOnArrival)
            return;

        PoolObj poolObj = GetComponent<PoolObj>();
        if (poolObj != null)
            poolObj.ReturnToPool();
        else
            gameObject.SetActive(false);
    }
}
