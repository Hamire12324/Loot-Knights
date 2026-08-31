public static class CharacterCreationDataFactory
{
    public static bool TryCreate(string enteredName, CharacterRoleDefinition role, out CreatedCharacterData characterData)
    {
        string characterName = enteredName != null ? enteredName.Trim() : string.Empty;

        if (string.IsNullOrEmpty(characterName))
        {
            characterName = role != null ? role.RoleName : string.Empty;
        }

        if (string.IsNullOrEmpty(characterName))
        {
            characterData = default;
            return false;
        }

        CharacterClass characterClass = role != null ? role.CharacterClass : CharacterClass.Knight;
        characterData = new CreatedCharacterData(characterName, characterClass);
        return true;
    }
}
