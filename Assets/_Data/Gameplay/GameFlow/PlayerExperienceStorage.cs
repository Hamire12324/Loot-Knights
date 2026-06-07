using System;
using UnityEngine;

public static class PlayerExperienceStorage
{
    public const string ExperienceKey = "LootKnights.Character.Experience";

    private const int DefaultExperience = 0;

    public static event Action<int> OnExperienceChanged;

    public static int Experience => PlayerPrefs.GetInt(ExperienceKey, DefaultExperience);

    public static void Set(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        PlayerPrefs.SetInt(ExperienceKey, safeAmount);
        PlayerPrefs.Save();
        OnExperienceChanged?.Invoke(safeAmount);
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;

        Set(Experience + amount);
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(ExperienceKey);
        PlayerPrefs.Save();
        OnExperienceChanged?.Invoke(Experience);
    }
}
