using System.Collections.Generic;
using UnityEngine;

public enum ArcherArrowMemoryKind
{
    Flying,
    Ground,
    Enemy
}

public sealed class ArcherArrowMemory : MonoBehaviour
{
    private static readonly List<ArcherArrowMemory> Active = new();

    public CharacterCtrl Owner { get; private set; }
    public CharacterCtrl AttachedTarget { get; private set; }
    public ArcherArrowMemoryKind Kind { get; private set; }
    public Vector2 Direction { get; private set; }

    private float expireTime;

    public static ArcherArrowMemory Create(
        CharacterCtrl owner,
        Vector3 position,
        Vector2 direction,
        ArcherArrowMemoryKind kind,
        float lifetime,
        CharacterCtrl attachedTarget = null)
    {
        GameObject instance = new("ArcherArrowMemory");
        instance.transform.position = position;

        ArcherArrowMemory memory = instance.AddComponent<ArcherArrowMemory>();
        memory.Owner = owner;
        memory.AttachedTarget = attachedTarget;
        memory.Kind = kind;
        memory.Direction = direction == Vector2.zero ? Vector2.down : direction.normalized;
        memory.expireTime = Time.time + Mathf.Max(0.1f, lifetime);
        Active.Add(memory);
        return memory;
    }

    public static List<ArcherArrowMemory> ConsumeFor(CharacterCtrl owner)
    {
        Active.RemoveAll(memory => memory == null);

        List<ArcherArrowMemory> result = new();
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            ArcherArrowMemory memory = Active[i];
            if (memory == null || memory.Owner != owner)
                continue;

            Active.RemoveAt(i);
            result.Add(memory);
        }

        return result;
    }

    private void LateUpdate()
    {
        if (AttachedTarget != null)
            transform.position = AttachedTarget.transform.position;

        if (Time.time >= expireTime)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }
}
