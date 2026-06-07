using System;

[Serializable]
public class CreatedCharacterData
{
    public string CharacterName;
    public CharacterClass CharacterClass;

    public CreatedCharacterData(string characterName, CharacterClass characterClass)
    {
        CharacterName = characterName;
        CharacterClass = characterClass;
    }
}
