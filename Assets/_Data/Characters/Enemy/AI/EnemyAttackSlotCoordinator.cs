using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackSlotCoordinator
{
    private static readonly Dictionary<CharacterDamReceiver, int> SlotsByTarget = new();

    private CharacterDamReceiver reservedTarget;

    public bool TryReserve(CharacterDamReceiver receiver, int maxAttackers)
    {
        if (receiver == null) return false;

        if (reservedTarget == receiver)
            return true;

        Release();

        maxAttackers = Mathf.Max(1, maxAttackers);
        SlotsByTarget.TryGetValue(receiver, out int currentCount);

        if (currentCount >= maxAttackers)
            return false;

        SlotsByTarget[receiver] = currentCount + 1;
        reservedTarget = receiver;
        receiver.OnDeath += HandleReservedTargetDeath;
        return true;
    }

    public void Release()
    {
        if (reservedTarget == null) return;

        reservedTarget.OnDeath -= HandleReservedTargetDeath;

        if (SlotsByTarget.TryGetValue(reservedTarget, out int currentCount))
        {
            currentCount--;

            if (currentCount <= 0)
                SlotsByTarget.Remove(reservedTarget);
            else
                SlotsByTarget[reservedTarget] = currentCount;
        }

        reservedTarget = null;
    }

    private void HandleReservedTargetDeath(CharacterDamReceiver receiver)
    {
        Release();
    }
}
