using System.Collections.Generic;
using UnityEngine;

public static class EnemySpawnTable
{
    public static int GetMinimumCost(
        IReadOnlyList<EnemySpawnEntry> localEntries,
        IReadOnlyList<EnemySpawnEntry> stageEntries,
        PoolObj fallbackPrefab,
        int level)
    {
        IReadOnlyList<EnemySpawnEntry> entries = ChooseEntries(localEntries, stageEntries);
        int minCost = int.MaxValue;

        foreach (EnemySpawnEntry entry in entries)
        {
            if (!IsCandidate(entry, int.MaxValue, level)) continue;
            minCost = Mathf.Min(minCost, entry.Cost);
        }

        if (minCost == int.MaxValue && !HasEntries(entries) && fallbackPrefab != null)
            minCost = 1;

        return minCost == int.MaxValue ? 0 : minCost;
    }

    public static EnemySpawnEntry Pick(
        IReadOnlyList<EnemySpawnEntry> localEntries,
        IReadOnlyList<EnemySpawnEntry> stageEntries,
        PoolObj fallbackPrefab,
        int remainingBudget,
        int level)
    {
        List<EnemySpawnEntry> candidates = GetCandidates(
            localEntries,
            stageEntries,
            fallbackPrefab,
            remainingBudget,
            level);

        if (candidates.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (EnemySpawnEntry candidate in candidates)
            totalWeight += candidate.Weight;

        int roll = Random.Range(0, totalWeight);

        foreach (EnemySpawnEntry candidate in candidates)
        {
            roll -= candidate.Weight;

            if (roll < 0)
                return candidate;
        }

        return candidates[0];
    }

    private static List<EnemySpawnEntry> GetCandidates(
        IReadOnlyList<EnemySpawnEntry> localEntries,
        IReadOnlyList<EnemySpawnEntry> stageEntries,
        PoolObj fallbackPrefab,
        int remainingBudget,
        int level)
    {
        IReadOnlyList<EnemySpawnEntry> entries = ChooseEntries(localEntries, stageEntries);
        List<EnemySpawnEntry> candidates = new();

        foreach (EnemySpawnEntry entry in entries)
        {
            if (!IsCandidate(entry, remainingBudget, level)) continue;
            candidates.Add(entry);
        }

        if (candidates.Count == 0 && !HasEntries(entries) && fallbackPrefab != null && remainingBudget >= 1)
            candidates.Add(new EnemySpawnEntry(fallbackPrefab));

        return candidates;
    }

    private static IReadOnlyList<EnemySpawnEntry> ChooseEntries(
        IReadOnlyList<EnemySpawnEntry> localEntries,
        IReadOnlyList<EnemySpawnEntry> stageEntries)
    {
        return stageEntries != null && stageEntries.Count > 0 ? stageEntries : localEntries;
    }

    private static bool IsCandidate(EnemySpawnEntry entry, int remainingBudget, int level)
    {
        if (entry == null || entry.Prefab == null) return false;
        if (entry.Cost > remainingBudget) return false;
        if (!entry.IsAllowedAtDifficulty(level)) return false;

        return true;
    }

    private static bool HasEntries(IReadOnlyList<EnemySpawnEntry> entries)
    {
        return entries != null && entries.Count > 0;
    }
}
