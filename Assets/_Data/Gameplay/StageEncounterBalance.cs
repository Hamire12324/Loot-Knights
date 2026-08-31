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
            1 => new[] { Slime },
            <= 3 => new[] { Slime, PoisonSlime },
            <= 5 => new[] { Slime, PoisonSlime, Bat },
            6 => new[] { Skeleton },
            7 => new[] { Skeleton, SkeletonArcher },
            8 => new[] { Skeleton, SkeletonArcher, ArmoredSkeleton },
            <= 10 => new[] { Skeleton, SkeletonArcher, ArmoredSkeleton, GreatswordSkeleton },
            11 => new[] { Orc },
            12 => new[] { Orc, ArmoredOrc },
            13 => new[] { Orc, ArmoredOrc, EliteOrc },
            <= 15 => new[] { Orc, ArmoredOrc, EliteOrc, OrcRider },
            16 => new[] { Werebear },
            17 => new[] { Werebear, Werewolf },
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
        // The original progression opened a new player's first stage with 36
        // enemies. Keep the same three-beat structure, but teach positioning
        // and skills before crowd pressure becomes the main challenge.
        int opening = 4 + number;
        int firstWave = 5 + number;
        int finalWave = firstWave + 2;
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
                1 => new[] { E(Slime, 100) },
                2 => new[] { E(Slime, 75), E(PoisonSlime, 25) },
                3 => new[] { E(Slime, 60), E(PoisonSlime, 40) },
                4 => new[] { E(Slime, 50), E(PoisonSlime, 30), E(Bat, 20) },
                _ => new[] { E(Slime, 40), E(PoisonSlime, 35), E(Bat, 25) }
            };
            miniBoss = number == 1 ? Slime : number < 4 ? PoisonSlime : Bat;
            if (number == 5) boss = PoisonSlime;
        }
        else if (number <= 10)
        {
            stageName = $"Stage {number} - Skeleton Crypt";
            roster = number switch
            {
                6 => new[] { E(Skeleton, 100) },
                7 => new[] { E(Skeleton, 75), E(SkeletonArcher, 25) },
                8 => new[] { E(Skeleton, 55), E(SkeletonArcher, 25), E(ArmoredSkeleton, 20) },
                9 => new[] { E(Skeleton, 40), E(SkeletonArcher, 25), E(ArmoredSkeleton, 20), E(GreatswordSkeleton, 15) },
                _ => new[] { E(Skeleton, 25), E(SkeletonArcher, 25), E(ArmoredSkeleton, 25), E(GreatswordSkeleton, 25) }
            };
            miniBoss = number switch
            {
                6 => Skeleton,
                7 => SkeletonArcher,
                8 => ArmoredSkeleton,
                _ => GreatswordSkeleton
            };
            if (number == 10) boss = GreatswordSkeleton;
        }
        else if (number <= 15)
        {
            stageName = $"Stage {number} - Orc Warcamp";
            roster = number switch
            {
                11 => new[] { E(Orc, 100) },
                12 => new[] { E(Orc, 70), E(ArmoredOrc, 30) },
                13 => new[] { E(Orc, 50), E(ArmoredOrc, 30), E(EliteOrc, 20) },
                14 => new[] { E(Orc, 30), E(ArmoredOrc, 25), E(EliteOrc, 25), E(OrcRider, 20) },
                _ => new[] { E(Orc, 20), E(ArmoredOrc, 25), E(EliteOrc, 30), E(OrcRider, 25) }
            };
            miniBoss = number switch
            {
                11 => Orc,
                12 => ArmoredOrc,
                13 => EliteOrc,
                _ => OrcRider
            };
            if (number == 15) boss = OrcRider;
        }
        else
        {
            stageName = $"Stage {number} - Moonfall Citadel";
            roster = number switch
            {
                16 => new[] { E(Werebear, 100) },
                17 => new[] { E(Werebear, 65), E(Werewolf, 35) },
                18 => new[] { E(Werebear, 45), E(Werewolf, 35), E(Necromancer, 20) },
                19 => new[] { E(Werebear, 30), E(Werewolf, 35), E(Necromancer, 35) },
                _ => new[] { E(Werebear, 25), E(Werewolf, 30), E(Necromancer, 45) }
            };
            miniBoss = number == 16 ? Werebear : number == 17 ? Werewolf : Necromancer;
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
