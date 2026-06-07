using System;
using UnityEngine;

public static class PlayerExperienceStorage
{
    public const string ExperienceKey = "LootKnights.Character.Experience";

    private const int DefaultExperience = 0;

    public static event Action<int> OnExperienceChanged;
    public static event Action<PlayerLevelSnapshot> OnLevelSnapshotChanged;
    public static event Action<int, int> OnLevelChanged;

    public static int Experience => PlayerPrefs.GetInt(ExperienceKey, DefaultExperience);
    public static int Level => Snapshot.Level;
    public static int MaxLevel => PlayerLevel.MaxLevel;
    public static PlayerLevelSnapshot Snapshot => PlayerLevel.CreateSnapshot(Experience);

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

    private static void PublishChanges(PlayerLevelSnapshot before, PlayerLevelSnapshot after)
    {
        OnExperienceChanged?.Invoke(after.TotalExperience);
        OnLevelSnapshotChanged?.Invoke(after);

        if (before.Level != after.Level)
            OnLevelChanged?.Invoke(before.Level, after.Level);
    }
}
