using UnityEngine;

public static class CharacterProfileStorage
{
    public const string NameKey = "LootKnights.Character.Name";
    public const string ClassKey = "LootKnights.Character.Class";

    public static bool HasCharacter()
    {
        return PlayerPrefs.HasKey(NameKey) && PlayerPrefs.HasKey(ClassKey);
    }

    public static CreatedCharacterData Load()
    {
        if (!HasCharacter()) return null;

        string characterName = PlayerPrefs.GetString(NameKey);
        CharacterClass characterClass = (CharacterClass)PlayerPrefs.GetInt(ClassKey);

        return new CreatedCharacterData(characterName, characterClass);
    }

    public static void Save(CreatedCharacterData data)
    {
        PlayerPrefs.SetString(NameKey, data.CharacterName);
        PlayerPrefs.SetInt(ClassKey, (int)data.CharacterClass);
        PlayerPrefs.Save();
    }

    public static string GetDebugSummary()
    {
        if (!HasCharacter())
        {
            return "No character saved.";
        }

        CreatedCharacterData character = Load();
        return "CharacterName: " + character.CharacterName + "\n"
            + "CharacterClass: " + character.CharacterClass + "\n"
            + "Level: " + PlayerExperienceStorage.Level + "\n"
            + "Experience: " + PlayerExperienceStorage.Experience + "\n"
            + "PlayerPrefs Keys:\n"
            + "- " + NameKey + "\n"
            + "- " + ClassKey + "\n"
            + "- " + PlayerExperienceStorage.ExperienceKey;
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(NameKey);
        PlayerPrefs.DeleteKey(ClassKey);
        PlayerExperienceStorage.Delete();
        PlayerPrefs.Save();
    }
}
