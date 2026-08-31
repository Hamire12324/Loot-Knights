using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterClassAvatarCatalog", menuName = "Loot Knights/Character Class Avatar Catalog")]
public class CharacterClassAvatarCatalog : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        [SerializeField] private CharacterClass characterClass;
        [SerializeField] private Sprite avatar;

        public CharacterClass CharacterClass => characterClass;
        public Sprite Avatar => avatar;
    }

    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private Entry[] avatars;

    public Sprite GetAvatar(CharacterClass characterClass)
    {
        if (avatars == null)
            return defaultAvatar;

        foreach (Entry entry in avatars)
        {
            if (entry.CharacterClass == characterClass && entry.Avatar != null)
                return entry.Avatar;
        }

        return defaultAvatar;
    }
}
