using System;
using UnityEngine;

public static class PlayerExperienceStorage
{
    public const string ExperienceKey = "LootKnights.Character.Experience";

    public static event Action<int> OnExperienceChanged;
    public static event Action<PlayerLevelSnapshot> OnLevelSnapshotChanged;
    public static event Action<int, int> OnLevelChanged;

    public static int Experience => Mathf.Max(0, PlayerPrefs.GetInt(GetExperienceKey(), 0));
    public static int Level => Snapshot.Level;
    public static int MaxLevel => PlayerLevel.MaxLevel;
    public static PlayerLevelSnapshot Snapshot => PlayerLevel.CreateSnapshot(Experience);

    public static PlayerLevelRewardResult Set(int amount)
    {
        PlayerLevelSnapshot before = Snapshot;
        int safeAmount = Mathf.Max(0, amount);

        PlayerPrefs.SetInt(GetExperienceKey(), safeAmount);
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
        PlayerPrefs.DeleteKey(GetExperienceKey());
        PlayerAttributePointStorage.Clear();
        PlayerSkillTreeManager.Service.ClearAllProgress();
        PlayerPrefs.Save();

        PlayerLevelSnapshot after = Snapshot;
        PublishChanges(before, after);
    }

    private static void PublishChanges(PlayerLevelSnapshot before, PlayerLevelSnapshot after)
    {
        PlayerAttributePointStorage.EnsureLevelRewarded(after.Level);

        OnExperienceChanged?.Invoke(after.TotalExperience);
        OnLevelSnapshotChanged?.Invoke(after);

        if (before.Level != after.Level)
            OnLevelChanged?.Invoke(before.Level, after.Level);
    }

    public static string GetExperienceKey(string characterId = null)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            CreatedCharacterData selectedCharacter = CharacterProfileStorage.Load();
            characterId = selectedCharacter != null ? selectedCharacter.CharacterId : null;
        }

        return string.IsNullOrEmpty(characterId)
            ? ExperienceKey
            : ExperienceKey + "." + characterId;
    }
}
