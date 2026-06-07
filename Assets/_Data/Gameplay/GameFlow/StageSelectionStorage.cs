using UnityEngine;

public static class StageSelectionStorage
{
    private const string SelectedStageIndexKey = "LootKnights.SelectedStageIndex";
    private const string HighestUnlockedStageIndexKey = "LootKnights.HighestUnlockedStageIndex";
    private const string OpenStageSelectOnMainMenuKey = "LootKnights.OpenStageSelectOnMainMenu";
    private const string OpenLobbyOnMainMenuKey = "LootKnights.OpenLobbyOnMainMenu";

    public static void SaveSelectedStageIndex(int stageIndex)
    {
        PlayerPrefs.SetInt(SelectedStageIndexKey, Mathf.Max(0, stageIndex));
        PlayerPrefs.Save();
    }

    public static int LoadSelectedStageIndex()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(SelectedStageIndexKey, 0));
    }

    public static int LoadHighestUnlockedStageIndex()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedStageIndexKey, 0));
    }

    public static bool IsStageUnlocked(int stageIndex)
    {
        return Mathf.Max(0, stageIndex) <= LoadHighestUnlockedStageIndex();
    }

    public static void UnlockStage(int stageIndex)
    {
        int safeStageIndex = Mathf.Max(0, stageIndex);
        int highestUnlockedStageIndex = LoadHighestUnlockedStageIndex();

        if (safeStageIndex <= highestUnlockedStageIndex) return;

        PlayerPrefs.SetInt(HighestUnlockedStageIndexKey, safeStageIndex);
        PlayerPrefs.Save();
    }

    public static void RequestOpenStageSelectOnMainMenu()
    {
        PlayerPrefs.SetInt(OpenStageSelectOnMainMenuKey, 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeOpenStageSelectOnMainMenuRequest()
    {
        bool requested = PlayerPrefs.GetInt(OpenStageSelectOnMainMenuKey, 0) == 1;

        if (requested)
        {
            PlayerPrefs.DeleteKey(OpenStageSelectOnMainMenuKey);
            PlayerPrefs.Save();
        }

        return requested;
    }

    public static void RequestOpenLobbyOnMainMenu()
    {
        PlayerPrefs.SetInt(OpenLobbyOnMainMenuKey, 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeOpenLobbyOnMainMenuRequest()
    {
        bool requested = PlayerPrefs.GetInt(OpenLobbyOnMainMenuKey, 0) == 1;

        if (requested)
        {
            PlayerPrefs.DeleteKey(OpenLobbyOnMainMenuKey);
            PlayerPrefs.Save();
        }

        return requested;
    }
}
