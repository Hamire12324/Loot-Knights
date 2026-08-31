using System;
using UnityEngine;

public static class CharacterRoleRepository
{
    public static CharacterRoleDefinition[] LoadFromResources(string resourcesPath)
    {
        CharacterRoleDefinition[] roles = Resources.LoadAll<CharacterRoleDefinition>(resourcesPath);

        if (roles == null || roles.Length == 0)
        {
            return Array.Empty<CharacterRoleDefinition>();
        }

        Array.Sort(roles, (left, right) => left.CharacterClass.CompareTo(right.CharacterClass));
        return roles;
    }
}
