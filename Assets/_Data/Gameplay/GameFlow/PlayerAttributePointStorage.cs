using System;
using UnityEngine;

public static class PlayerAttributePointStorage
{
    private const string AvailablePointsKey = "LootKnights.Character.AttributePoints.Available";
    private const string HighestRewardedLevelKey = "LootKnights.Character.AttributePoints.HighestRewardedLevel";
    private const string SpentPointKeyPrefix = "LootKnights.Character.AttributePoints.Spent.";

    public const int PointsPerLevel = 3;
    public const float AttackPerPoint = 2f;
    public const float MaxHealthPerPoint = 10f;
    public const float ArmorPerPoint = 0.5f;
    public const float CritChancePerPoint = 0.005f;
    public const float CritDamagePerPoint = 0.02f;

    public static event Action OnPointsChanged;

    public static int AvailablePoints
    {
        get
        {
            NormalizeSavedPoints();
            return Mathf.Max(0, PlayerPrefs.GetInt(GetCharacterKey(AvailablePointsKey), 0));
        }
    }
    public static int HighestRewardedLevel => Mathf.Max(1, PlayerPrefs.GetInt(GetCharacterKey(HighestRewardedLevelKey), 1));

    public static void EnsureLevelRewarded(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        int highestRewardedLevel = HighestRewardedLevel;

        if (safeLevel <= highestRewardedLevel) return;

        int gainedLevels = safeLevel - highestRewardedLevel;
        PlayerPrefs.SetInt(GetCharacterKey(AvailablePointsKey), AvailablePoints + gainedLevels * PointsPerLevel);
        PlayerPrefs.SetInt(GetCharacterKey(HighestRewardedLevelKey), safeLevel);
        PlayerPrefs.Save();
        OnPointsChanged?.Invoke();
    }

    public static bool TrySpendPoint(StatType statType)
    {
        if (!CanSpendOn(statType) || AvailablePoints <= 0)
            return false;

        PlayerPrefs.SetInt(GetCharacterKey(AvailablePointsKey), AvailablePoints - 1);
        PlayerPrefs.SetInt(GetSpentPointKey(statType), GetSpentPoints(statType) + 1);
        PlayerPrefs.Save();
        OnPointsChanged?.Invoke();
        return true;
    }

    public static bool TryRefundPoint(StatType statType)
    {
        if (!CanSpendOn(statType)) return false;

        int spentPoints = GetSpentPoints(statType);
        if (spentPoints <= 0) return false;

        PlayerPrefs.SetInt(GetCharacterKey(AvailablePointsKey), AvailablePoints + 1);
        PlayerPrefs.SetInt(GetSpentPointKey(statType), spentPoints - 1);
        PlayerPrefs.Save();
        OnPointsChanged?.Invoke();
        return true;
    }

    public static int GetSpentPoints(StatType statType)
    {
        NormalizeSavedPoints();
        return CanSpendOn(statType)
            ? Mathf.Max(0, PlayerPrefs.GetInt(GetSpentPointKey(statType), 0))
            : 0;
    }

    public static float GetBonusValue(StatType statType)
    {
        int points = GetSpentPoints(statType);

        return statType switch
        {
            StatType.Attack => points * AttackPerPoint,
            StatType.MaxHealth => points * MaxHealthPerPoint,
            StatType.Armor => points * ArmorPerPoint,
            StatType.CritChance => points * CritChancePerPoint,
            StatType.CritDamage => points * CritDamagePerPoint,
            _ => 0f
        };
    }

    public static void ResetSpentPoints()
    {
        int refundedPoints = 0;

        foreach (StatType statType in GetSpendableStats())
        {
            refundedPoints += GetSpentPoints(statType);
            PlayerPrefs.DeleteKey(GetSpentPointKey(statType));
        }

        if (refundedPoints > 0)
            PlayerPrefs.SetInt(GetCharacterKey(AvailablePointsKey), AvailablePoints + refundedPoints);

        PlayerPrefs.Save();
        OnPointsChanged?.Invoke();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(GetCharacterKey(AvailablePointsKey));
        PlayerPrefs.DeleteKey(GetCharacterKey(HighestRewardedLevelKey));

        foreach (StatType statType in GetSpendableStats())
            PlayerPrefs.DeleteKey(GetSpentPointKey(statType));

        PlayerPrefs.Save();
        OnPointsChanged?.Invoke();
    }

    public static bool CanSpendOn(StatType statType)
    {
        return statType == StatType.Attack ||
               statType == StatType.MaxHealth ||
               statType == StatType.Armor ||
               statType == StatType.CritChance ||
               statType == StatType.CritDamage;
    }

    public static StatType[] GetSpendableStats()
    {
        return new[]
        {
            StatType.Attack,
            StatType.MaxHealth,
            StatType.Armor,
            StatType.CritChance,
            StatType.CritDamage
        };
    }

    /// <summary>
    /// Saved PlayerPrefs can outlive balancing changes or be manually edited.
    /// Keep total allocated and available attribute points within the number
    /// actually earned by the current character's level.
    /// </summary>
    private static void NormalizeSavedPoints()
    {
        int remaining = Mathf.Max(0, (PlayerExperienceStorage.Level - 1) * PointsPerLevel);
        bool changed = false;

        foreach (StatType statType in GetSpendableStats())
        {
            string key = GetSpentPointKey(statType);
            int saved = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
            int allowed = Mathf.Min(saved, remaining);

            if (saved != allowed)
            {
                PlayerPrefs.SetInt(key, allowed);
                changed = true;
            }

            remaining -= allowed;
        }

        string availableKey = GetCharacterKey(AvailablePointsKey);
        int savedAvailable = Mathf.Max(0, PlayerPrefs.GetInt(availableKey, 0));
        int allowedAvailable = Mathf.Min(savedAvailable, remaining);
        if (savedAvailable != allowedAvailable)
        {
            PlayerPrefs.SetInt(availableKey, allowedAvailable);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }

    private static string GetSpentPointKey(StatType statType)
    {
        return GetCharacterKey(SpentPointKeyPrefix + statType);
    }

    private static string GetCharacterKey(string key)
    {
        return CharacterProfileStorage.GetCurrentCharacterKey(key);
    }
}
