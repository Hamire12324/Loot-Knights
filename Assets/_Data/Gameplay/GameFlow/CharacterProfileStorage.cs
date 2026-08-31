using System;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterProfileStorage
{
    public const string NameKey = "LootKnights.Character.Name";
    public const string ClassKey = "LootKnights.Character.Class";
    public const int MaxCharacters = 4;

    private const string ProfilesKey = "LootKnights.Characters";
    private const string SelectedCharacterIdKey = "LootKnights.SelectedCharacterId";
    private const string MigrationKey = "LootKnights.Characters.Migrated";
    private const string LegacyProgressOwnerCharacterIdKey = "LootKnights.LegacyProgressOwnerCharacterId";

    [Serializable]
    private class CharacterCollection
    {
        public List<CreatedCharacterData> Characters = new();
    }

    public static bool HasCharacter()
    {
        return LoadAll().Count > 0;
    }

    public static CreatedCharacterData Load()
    {
        List<CreatedCharacterData> characters = LoadAll();
        if (characters.Count == 0) return null;

        string selectedId = PlayerPrefs.GetString(SelectedCharacterIdKey);
        CreatedCharacterData selected = characters.Find(character => character != null && character.CharacterId == selectedId);
        if (selected != null) return selected;

        Select(characters[0]);
        return characters[0];
    }

    public static void Save(CreatedCharacterData data)
    {
        if (data == null) return;

        RegisterLegacyProgressOwner(Load());

        List<CreatedCharacterData> characters = LoadAll();
        if (string.IsNullOrEmpty(data.CharacterId))
            data.CharacterId = Guid.NewGuid().ToString("N");

        int existingIndex = characters.FindIndex(character => character.CharacterId == data.CharacterId);
        if (existingIndex >= 0)
            characters[existingIndex] = data;
        else if (characters.Count < MaxCharacters)
            characters.Add(data);
        else
            return;

        SaveAll(characters);
        Select(data);
    }

    public static IReadOnlyList<CreatedCharacterData> GetAll()
    {
        return LoadAll();
    }

    public static bool CanCreateCharacter()
    {
        return LoadAll().Count < MaxCharacters;
    }

    public static void Select(CreatedCharacterData character)
    {
        if (character == null || string.IsNullOrEmpty(character.CharacterId)) return;

        PlayerPrefs.SetString(SelectedCharacterIdKey, character.CharacterId);
        PlayerPrefs.Save();
    }

    public static string GetCurrentCharacterKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        CreatedCharacterData character = Load();
        return character == null || string.IsNullOrEmpty(character.CharacterId)
            ? key
            : key + "." + character.CharacterId;
    }

    public static void RegisterLegacyProgressOwner(CreatedCharacterData character)
    {
        if (character == null || string.IsNullOrEmpty(character.CharacterId) || PlayerPrefs.HasKey(LegacyProgressOwnerCharacterIdKey)) return;

        PlayerPrefs.SetString(LegacyProgressOwnerCharacterIdKey, character.CharacterId);
        PlayerPrefs.Save();
    }

    public static bool IsLegacyProgressOwnedByCurrentCharacter()
    {
        CreatedCharacterData character = Load();
        RegisterLegacyProgressOwner(character);
        return character != null && character.CharacterId == PlayerPrefs.GetString(LegacyProgressOwnerCharacterIdKey, string.Empty);
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
            + "Characters: " + LoadAll().Count + " / " + MaxCharacters + "\n"
            + "Level: " + PlayerExperienceStorage.Level + "\n"
            + "Experience: " + PlayerExperienceStorage.Experience + "\n"
            + "PlayerPrefs Keys:\n"
            + "- " + NameKey + "\n"
            + "- " + ClassKey + "\n"
            + "- " + PlayerExperienceStorage.ExperienceKey;
    }

    public static void Delete()
    {
        Delete(Load());
    }

    public static void Delete(CreatedCharacterData character)
    {
        if (character == null) return;

        List<CreatedCharacterData> characters = LoadAll();
        characters.RemoveAll(savedCharacter => savedCharacter.CharacterId == character.CharacterId);
        SaveAll(characters);

        PlayerPrefs.DeleteKey(PlayerExperienceStorage.GetExperienceKey(character.CharacterId));
        if (PlayerPrefs.GetString(SelectedCharacterIdKey) == character.CharacterId)
        {
            if (characters.Count > 0)
                PlayerPrefs.SetString(SelectedCharacterIdKey, characters[0].CharacterId);
            else
                PlayerPrefs.DeleteKey(SelectedCharacterIdKey);
        }

        PlayerPrefs.Save();
    }

    private static List<CreatedCharacterData> LoadAll()
    {
        MigrateLegacyProfile();

        string json = PlayerPrefs.GetString(ProfilesKey, string.Empty);
        CharacterCollection collection = string.IsNullOrEmpty(json)
            ? new CharacterCollection()
            : JsonUtility.FromJson<CharacterCollection>(json);

        List<CreatedCharacterData> characters = collection != null && collection.Characters != null
            ? collection.Characters
            : new List<CreatedCharacterData>();

        foreach (CreatedCharacterData character in characters)
        {
            if (character != null && string.IsNullOrEmpty(character.CharacterId))
                character.CharacterId = Guid.NewGuid().ToString("N");
        }

        return characters;
    }

    private static void SaveAll(List<CreatedCharacterData> characters)
    {
        CharacterCollection collection = new() { Characters = characters };
        PlayerPrefs.SetString(ProfilesKey, JsonUtility.ToJson(collection));
        PlayerPrefs.Save();
    }

    private static void MigrateLegacyProfile()
    {
        if (PlayerPrefs.HasKey(MigrationKey)) return;

        if (PlayerPrefs.HasKey(NameKey) && PlayerPrefs.HasKey(ClassKey) && !PlayerPrefs.HasKey(ProfilesKey))
        {
            CreatedCharacterData legacyCharacter = new(
                PlayerPrefs.GetString(NameKey),
                (CharacterClass)PlayerPrefs.GetInt(ClassKey));
            SaveAll(new List<CreatedCharacterData> { legacyCharacter });
            PlayerPrefs.SetString(SelectedCharacterIdKey, legacyCharacter.CharacterId);

            if (PlayerPrefs.HasKey(PlayerExperienceStorage.ExperienceKey))
            {
                PlayerPrefs.SetInt(
                    PlayerExperienceStorage.GetExperienceKey(legacyCharacter.CharacterId),
                    PlayerPrefs.GetInt(PlayerExperienceStorage.ExperienceKey));
            }
        }

        PlayerPrefs.SetInt(MigrationKey, 1);
        PlayerPrefs.Save();
    }
}
