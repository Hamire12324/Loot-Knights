using System;
using UnityEngine;

public static class PlayerExperienceStorage
{
    public const string ExperienceKey = "LootKnights.Character.Experience";

    private const int DefaultExperience = 0;
    private const int MaxPlayerLevel = 99;
    private const int BaseExperienceToNextLevel = 100;
    private const int ExperienceGrowthPerLevel = 50;

    public static event Action<int> OnExperienceChanged;
    public static event Action<PlayerLevelSnapshot> OnLevelSnapshotChanged;
    public static event Action<int, int> OnLevelChanged;

    public static int Experience => PlayerPrefs.GetInt(ExperienceKey, DefaultExperience);
    public static int Level => Snapshot.Level;
    public static int MaxLevel => MaxPlayerLevel;
    public static PlayerLevelSnapshot Snapshot => CreateSnapshot(Experience);

    public static PlayerLevelRewardResult Set(int amount)
    {
        PlayerLevelSnapshot before = Snapshot;
        int safeAmount = Mathf.Max(0, amount);

        PlayerPrefs.SetInt(ExperienceKey, safeAmount);
        PlayerPrefs.Save();

        PlayerLevelSnapshot after = Snapshot;
        PublishChanges(before, after);

        return new PlayerLevelRewardResult(before, after, after.TotalExperience - before.TotalExperience);
    }

    public static PlayerLevelRewardResult Add(int amount)
    {
        if (amount <= 0)
        {
            PlayerLevelSnapshot snapshot = Snapshot;
            return new PlayerLevelRewardResult(snapshot, snapshot, 0);
        }

        return Set(Experience + amount);
    }

    public static void Delete()
    {
        PlayerLevelSnapshot before = Snapshot;
        PlayerPrefs.DeleteKey(ExperienceKey);
        PlayerPrefs.Save();

        PlayerLevelSnapshot after = Snapshot;
        PublishChanges(before, after);
    }

    public static int GetExperienceToNextLevel(int level)
    {
        int safeLevel = Mathf.Clamp(level, 1, MaxPlayerLevel);
        if (safeLevel >= MaxPlayerLevel) return 0;

        return BaseExperienceToNextLevel + (safeLevel - 1) * ExperienceGrowthPerLevel;
    }

    public static int GetTotalExperienceForLevel(int level)
    {
        int safeLevel = Mathf.Clamp(level, 1, MaxPlayerLevel);
        int totalExperience = 0;

        for (int currentLevel = 1; currentLevel < safeLevel; currentLevel++)
            totalExperience += GetExperienceToNextLevel(currentLevel);

        return totalExperience;
    }

    public static int GetLevelForExperience(int totalExperience)
    {
        int safeExperience = Mathf.Max(0, totalExperience);
        int level = 1;

        while (level < MaxPlayerLevel &&
               safeExperience >= GetTotalExperienceForLevel(level + 1))
        {
            level++;
        }

        return level;
    }

    private static PlayerLevelSnapshot CreateSnapshot(int totalExperience)
    {
        int safeExperience = Mathf.Max(0, totalExperience);
        int level = GetLevelForExperience(safeExperience);
        int levelStartExperience = GetTotalExperienceForLevel(level);
        int nextLevelExperience = level >= MaxPlayerLevel
            ? levelStartExperience
            : GetTotalExperienceForLevel(level + 1);

        return new PlayerLevelSnapshot(
            level,
            MaxPlayerLevel,
            safeExperience,
            levelStartExperience,
            nextLevelExperience);
    }

    private static void PublishChanges(PlayerLevelSnapshot before, PlayerLevelSnapshot after)
    {
        OnExperienceChanged?.Invoke(after.TotalExperience);
        OnLevelSnapshotChanged?.Invoke(after);

        if (before.Level != after.Level)
            OnLevelChanged?.Invoke(before.Level, after.Level);
    }
}

public readonly struct PlayerLevelSnapshot
{
    public int Level { get; }
    public int MaxLevel { get; }
    public int TotalExperience { get; }
    public int LevelStartExperience { get; }
    public int NextLevelExperience { get; }
    public int ExperienceIntoLevel { get; }
    public int ExperienceToNextLevel { get; }
    public float Progress01 { get; }
    public bool IsMaxLevel => Level >= MaxLevel;

    public PlayerLevelSnapshot(
        int level,
        int maxLevel,
        int totalExperience,
        int levelStartExperience,
        int nextLevelExperience)
    {
        Level = Mathf.Max(1, level);
        MaxLevel = Mathf.Max(1, maxLevel);
        TotalExperience = Mathf.Max(0, totalExperience);
        LevelStartExperience = Mathf.Max(0, levelStartExperience);
        NextLevelExperience = Mathf.Max(LevelStartExperience, nextLevelExperience);

        ExperienceIntoLevel = Mathf.Max(0, TotalExperience - LevelStartExperience);
        ExperienceToNextLevel = Mathf.Max(0, NextLevelExperience - LevelStartExperience);
        Progress01 = ExperienceToNextLevel > 0
            ? Mathf.Clamp01((float)ExperienceIntoLevel / ExperienceToNextLevel)
            : 1f;
    }
}

public readonly struct PlayerLevelRewardResult
{
    public PlayerLevelSnapshot Before { get; }
    public PlayerLevelSnapshot After { get; }
    public int ExperienceDelta { get; }
    public int LevelGained => After.Level - Before.Level;
    public bool LeveledUp => LevelGained > 0;

    public PlayerLevelRewardResult(
        PlayerLevelSnapshot before,
        PlayerLevelSnapshot after,
        int experienceDelta)
    {
        Before = before;
        After = after;
        ExperienceDelta = experienceDelta;
    }
}
