using System;
using System.Collections.Generic;
using UnityEngine;

public static class ArcherUltimateCharge
{
    private const int DefaultMaxCharges = 5;

    private static readonly Dictionary<CharacterCtrl, int> ChargesByOwner = new();

    public static event Action<CharacterCtrl, int> OnChanged;

    public static int GetCharges(CharacterCtrl owner)
    {
        if (owner == null)
            return 0;

        ChargesByOwner.TryGetValue(owner, out int charges);
        return Mathf.Max(0, charges);
    }

    public static void Add(CharacterCtrl owner, int amount, int maxCharges = DefaultMaxCharges)
    {
        if (owner == null || amount <= 0)
            return;

        int current = GetCharges(owner);
        int next = Mathf.Clamp(current + amount, 0, Mathf.Max(1, maxCharges));
        if (next == current)
            return;

        ChargesByOwner[owner] = next;
        OnChanged?.Invoke(owner, next);
    }

    public static int ConsumeAll(CharacterCtrl owner)
    {
        int charges = GetCharges(owner);
        if (owner == null || charges <= 0)
            return 0;

        ChargesByOwner.Remove(owner);
        OnChanged?.Invoke(owner, 0);
        return charges;
    }
}
