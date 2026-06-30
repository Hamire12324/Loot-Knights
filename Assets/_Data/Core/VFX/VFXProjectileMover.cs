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

    public void Play(Vector2 direction, float distance, float speed, float rotationOffsetDegrees = 0f)
    {
        StopMove();

        if (direction == Vector2.zero)
            direction = Vector2.down;

        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);

        moveCoroutine = StartCoroutine(MoveRoutine(direction, distance, speed));
    }

    private IEnumerator MoveRoutine(Vector2 direction, float distance, float speed)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + (Vector3)(direction * distance);

        if (distance <= 0f || speed <= 0f)
        {
            transform.position = endPosition;
            moveCoroutine = null;
            yield break;
        }

        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        transform.position = endPosition;
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
