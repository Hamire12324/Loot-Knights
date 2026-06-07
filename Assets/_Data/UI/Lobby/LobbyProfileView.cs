using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyProfileView : BaseMonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private CharacterClassAvatar[] classAvatars;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadTexts();
        LoadAvatarImage();
    }

    public void Refresh()
    {
        CreatedCharacterData character = CharacterProfileStorage.Load();
        string characterName = character != null ? character.CharacterName : "Name";

        SetText(characterName);
        SetAvatar(character);
    }

    private void LoadTexts()
    {
        if (characterNameText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text.name.ToLowerInvariant().Contains("name"))
                {
                    characterNameText = text;
                    break;
                }
            }
        }
    }

    private void LoadAvatarImage()
    {
        if (avatarImage != null) return;

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            string imageName = image.name.ToLowerInvariant();

            if (imageName.Contains("avatar") || imageName.Contains("portrait"))
            {
                avatarImage = image;
                return;
            }
        }
    }

    private void SetText(string characterName)
    {
        if (characterNameText != null)
            characterNameText.text = characterName;
    }

    private void SetAvatar(CreatedCharacterData character)
    {
        if (avatarImage == null) return;

        Sprite avatar = character != null ? GetAvatar(character.CharacterClass) : defaultAvatar;
        avatarImage.sprite = avatar;
        avatarImage.enabled = avatar != null;
    }

    private Sprite GetAvatar(CharacterClass characterClass)
    {
        if (classAvatars != null)
        {
            foreach (CharacterClassAvatar classAvatar in classAvatars)
            {
                if (classAvatar.CharacterClass == characterClass)
                {
                    return classAvatar.Avatar;
                }
            }
        }

        return defaultAvatar;
    }
}
