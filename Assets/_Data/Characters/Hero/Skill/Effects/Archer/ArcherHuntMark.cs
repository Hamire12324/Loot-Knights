using System.Collections.Generic;
using UnityEngine;

public sealed class ArcherHuntMark : MonoBehaviour
{
    private static readonly List<ArcherHuntMark> Active = new();

    public CharacterCtrl Owner { get; private set; }
    public CharacterCtrl Target { get; private set; }
    public int Stacks { get; private set; }

    private float expireTime;

    public static void Apply(CharacterCtrl owner, CharacterCtrl target, float duration)
    {
        if (owner == null || target == null)
            return;

        ArcherHuntMark mark = target.GetComponent<ArcherHuntMark>();
        if (mark == null)
            mark = target.gameObject.AddComponent<ArcherHuntMark>();

        mark.Owner = owner;
        mark.Target = target;
        mark.Stacks = Mathf.Min(mark.Stacks + 1, 9);
        mark.expireTime = Time.time + Mathf.Max(0.1f, duration);

        if (!Active.Contains(mark))
            Active.Add(mark);
    }

    public static CharacterCtrl FindBestTarget(CharacterCtrl owner)
    {
        Active.RemoveAll(mark => mark == null);

        ArcherHuntMark best = null;
        foreach (ArcherHuntMark mark in Active)
        {
            if (mark == null || mark.Owner != owner || mark.Target == null)
                continue;

            if (mark.Target.CharacterDamReceiver != null && mark.Target.CharacterDamReceiver.IsDead)
                continue;

            if (best == null || mark.Stacks > best.Stacks)
                best = mark;
        }

        return best != null ? best.Target : null;
    }

    private void LateUpdate()
    {
        if (Time.time >= expireTime)
            Destroy(this);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }
}
