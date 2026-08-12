using System;
using System.Collections.Generic;
using UnityEngine;

public static class StageEncounterBalance
{
    private const string Slime = "SlimeCtrl";
    private const string PoisonSlime = "PoisonSlimeCtrl";
    private const string Bat = "BatCtrl";
    private const string Skeleton = "SkeletonCtrl";
    private const string SkeletonArcher = "SkeletonArcherCtrl";
    private const string ArmoredSkeleton = "ArmoredSkeletonCtrl";
    private const string GreatswordSkeleton = "GreatswordSkeletonCtrl";
    private const string Orc = "OrcCtrl";
    private const string ArmoredOrc = "ArmoredOrcCtrl";
    private const string EliteOrc = "EliteOrcCtrl";
    private const string OrcRider = "OrcRiderCtrl";
    private const string Werebear = "WerebearCtrl";
    private const string Werewolf = "WerewolfCtrl";
    private const string Necromancer = "NecromancerCtrl";

    public static void Apply(IReadOnlyList<StageConfig> stages)
    {
        if (stages == null || stages.Count == 0)
            return;

        Dictionary<string, PoolObj> prefabs = CollectPrefabs(stages);

        foreach (StageConfig stage in stages)
        {
            if (stage == null || !stage.UseGeneratedEncounterBalance) continue;

            List<string> missingPrefabs = GetMissingPrefabsForStage(stage.StageNumber, prefabs);
            if (missingPrefabs.Count > 0)
            {
                Debug.LogWarning(
                    $"Stage {stage.StageNumber} encounter balance was skipped. Missing enemy prefabs: " +
                    string.Join(", ", missingPrefabs) + ".",
                    stage);
                continue;
            }

            ApplyStage(stage, prefabs);
        }
    }

    private static Dictionary<string, PoolObj> CollectPrefabs(IReadOnlyList<StageConfig> stages)
    {
        Dictionary<string, PoolObj> prefabs = new(StringComparer.Ordinal);
        foreach (StageConfig stage in stages)
        {
            if (stage == null) continue;
            foreach (StageEnemyEntry entry in stage.EnemyRoster)
            {
                if (entry?.Prefab != null)
                    prefabs[entry.Prefab.name] = entry.Prefab;
            }
        }
        return prefabs;
    }

    private static List<string> GetMissingPrefabsForStage(
        int stageNumber,
        IReadOnlyDictionary<string, PoolObj> prefabs)
    {
        string[] required = stageNumber switch
        {
            <= 5 => new[] { Slime, PoisonSlime, Bat },
            <= 10 => new[] { Skeleton, SkeletonArcher, ArmoredSkeleton, GreatswordSkeleton },
            <= 15 => new[] { Orc, ArmoredOrc, EliteOrc, OrcRider },
            _ => new[] { Werebear, Werewolf, Necromancer }
        };

        List<string> missing = new();
        foreach (string prefab in required)
        {
            if (!prefabs.TryGetValue(prefab, out PoolObj value) || value == null)
                missing.Add(prefab);
        }

        return missing;
    }

    private static void ApplyStage(StageConfig stage, IReadOnlyDictionary<string, PoolObj> p)
    {
        int number = stage.StageNumber;
        int opening = 10 + number * 2;
        int firstWave = 14 + number * 2;
        int finalWave = firstWave + 4;
        string stageName;
        Entry[] roster;
        string miniBoss;
        string boss = null;
        BossEncounterConfig bossEncounter = stage.FindBossEncounter();

        if (number <= 5)
        {
            stageName = $"Stage {number} - Slime Meadow";
            roster = number switch
            {
                1 => new[] { E(Slime, 70), E(PoisonSlime, 20), E(Bat, 10) },
                2 => new[] { E(Slime, 55), E(PoisonSlime, 30), E(Bat, 15) },
                3 => new[] { E(Slime, 40), E(PoisonSlime, 35), E(Bat, 25) },
                4 => new[] { E(Slime, 30), E(PoisonSlime, 40), E(Bat, 30) },
                _ => new[] { E(Slime, 25), E(PoisonSlime, 40), E(Bat, 35) }
            };
            miniBoss = PoisonSlime;
            if (number == 5) boss = PoisonSlime;
        }
        else if (number <= 10)
        {
            stageName = $"Stage {number} - Skeleton Crypt";
            roster = number switch
            {
                6 => new[] { E(Skeleton, 70), E(SkeletonArcher, 20), E(ArmoredSkeleton, 10) },
                7 => new[] { E(Skeleton, 55), E(SkeletonArcher, 25), E(ArmoredSkeleton, 20) },
                8 => new[] { E(Skeleton, 40), E(SkeletonArcher, 25), E(ArmoredSkeleton, 20), E(GreatswordSkeleton, 15) },
                9 => new[] { E(Skeleton, 35), E(SkeletonArcher, 25), E(ArmoredSkeleton, 20), E(GreatswordSkeleton, 20) },
                _ => new[] { E(Skeleton, 25), E(SkeletonArcher, 25), E(ArmoredSkeleton, 25), E(GreatswordSkeleton, 25) }
            };
            miniBoss = number < 8 ? ArmoredSkeleton : GreatswordSkeleton;
            if (number == 10) boss = GreatswordSkeleton;
        }
        else if (number <= 15)
        {
            stageName = $"Stage {number} - Orc Warcamp";
            roster = number switch
            {
                11 => new[] { E(Orc, 65), E(ArmoredOrc, 20), E(EliteOrc, 15) },
                12 => new[] { E(Orc, 50), E(ArmoredOrc, 25), E(EliteOrc, 15), E(OrcRider, 10) },
                13 => new[] { E(Orc, 40), E(ArmoredOrc, 25), E(EliteOrc, 20), E(OrcRider, 15) },
                14 => new[] { E(Orc, 30), E(ArmoredOrc, 25), E(EliteOrc, 25), E(OrcRider, 20) },
                _ => new[] { E(Orc, 20), E(ArmoredOrc, 25), E(EliteOrc, 30), E(OrcRider, 25) }
            };
            miniBoss = number == 11 ? EliteOrc : OrcRider;
            if (number == 15) boss = OrcRider;
        }
        else
        {
            stageName = $"Stage {number} - Moonfall Citadel";
            roster = number switch
            {
                16 => new[] { E(Werebear, 50), E(Werewolf, 35), E(Necromancer, 15) },
                17 => new[] { E(Werebear, 40), E(Werewolf, 35), E(Necromancer, 25) },
                18 => new[] { E(Werebear, 35), E(Werewolf, 35), E(Necromancer, 30) },
                19 => new[] { E(Werebear, 30), E(Werewolf, 35), E(Necromancer, 35) },
                _ => new[] { E(Werebear, 25), E(Werewolf, 30), E(Necromancer, 45) }
            };
            miniBoss = Necromancer;
            if (number == 20) boss = Necromancer;
        }

        List<StageEnemyEntry> balancedRoster = ResolveEntries(roster, p);
        List<StageWaveConfig> waves = new()
        {
            Wave(firstWave, 1f, p),
            Wave(1, 1.5f, p, miniBoss),
            Wave(finalWave, 1.25f, p)
        };

        if (boss != null)
            waves.Add(BossWave(2.5f, p, boss, bossEncounter));

        stage.ApplyEncounterBalance(stageName, balancedRoster, Wave(opening, 0f, p), waves);
    }

    private static StageWaveConfig Wave(
        int count,
        float delay,
        IReadOnlyDictionary<string, PoolObj> prefabs,
        string overrideEnemy = null,
        bool boss = false)
    {
        List<StageEnemyEntry> overrides = string.IsNullOrEmpty(overrideEnemy)
            ? null
            : new List<StageEnemyEntry> { new StageEnemyEntry(prefabs[overrideEnemy], 1) };
        return new StageWaveConfig(count, delay, boss, overrides);
    }

    private static StageWaveConfig BossWave(
        float delay,
        IReadOnlyDictionary<string, PoolObj> prefabs,
        string bossPrefab,
        BossEncounterConfig bossEncounter)
    {
        List<StageEnemyEntry> overrides = new()
        {
            new StageEnemyEntry(prefabs[bossPrefab], 1)
        };
        string displayName = bossPrefab.EndsWith("Ctrl", StringComparison.Ordinal)
            ? bossPrefab[..^4]
            : bossPrefab;
        return new StageWaveConfig(
            1,
            delay,
            false,
            overrides,
            bossEncounter ?? new BossEncounterConfig(displayName));
    }

    private static List<StageEnemyEntry> ResolveEntries(IEnumerable<Entry> entries, IReadOnlyDictionary<string, PoolObj> prefabs)
    {
        List<StageEnemyEntry> result = new();
        foreach (Entry entry in entries)
            result.Add(new StageEnemyEntry(prefabs[entry.Name], entry.Weight));
        return result;
    }

    private static Entry E(string name, int weight) => new(name, weight);
    private readonly struct Entry
    {
        public readonly string Name;
        public readonly int Weight;
        public Entry(string name, int weight) { Name = name; Weight = weight; }
    }
}
