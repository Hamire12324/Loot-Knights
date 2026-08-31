using System;

[Serializable]
public class CreatedCharacterData
{
    public string CharacterId;
    public string CharacterName;
    public CharacterClass CharacterClass;

    public CreatedCharacterData(string characterName, CharacterClass characterClass)
    {
        CharacterId = Guid.NewGuid().ToString("N");
        CharacterName = characterName;
        CharacterClass = characterClass;
    }
}
