using System.Collections.Generic;
using UnityEngine;

public static class CharacterSkillResource
{
    private static readonly Dictionary<CharacterCtrl, Dictionary<string, int>> ValuesByOwner = new();

    public static int GetValue(CharacterCtrl owner, string resourceId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(resourceId) ||
            !ValuesByOwner.TryGetValue(owner, out Dictionary<string, int> values))
            return 0;

        return values.TryGetValue(resourceId, out int value) ? Mathf.Max(0, value) : 0;
    }

    public static void Add(CharacterCtrl owner, string resourceId, int amount, int maximumValue)
    {
        if (owner == null || string.IsNullOrWhiteSpace(resourceId) || amount <= 0)
            return;

        if (!ValuesByOwner.TryGetValue(owner, out Dictionary<string, int> values))
        {
            values = new Dictionary<string, int>();
            ValuesByOwner[owner] = values;
        }

        values[resourceId] = Mathf.Clamp(GetValue(owner, resourceId) + amount, 0, Mathf.Max(1, maximumValue));
    }

    public static int ConsumeAll(CharacterCtrl owner, string resourceId)
    {
        int value = GetValue(owner, resourceId);
        if (value <= 0 || !ValuesByOwner.TryGetValue(owner, out Dictionary<string, int> values))
            return 0;

        values.Remove(resourceId);
        if (values.Count == 0)
            ValuesByOwner.Remove(owner);

        return value;
    }
}
