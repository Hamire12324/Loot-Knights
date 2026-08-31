using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyProfileView : BaseMonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Image avatarImage;
    [SerializeField] private CharacterClassAvatarCatalog avatarCatalog;

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerExperienceStorage.OnLevelSnapshotChanged += HandleLevelSnapshotChanged;
        Refresh();
    }

    protected override void OnDisable()
    {
        PlayerExperienceStorage.OnLevelSnapshotChanged -= HandleLevelSnapshotChanged;
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadTexts();
        LoadExperienceSlider();
        LoadAvatarImage();
        LoadAvatarCatalog();
    }

    public void Refresh()
    {
        CreatedCharacterData character = CharacterProfileStorage.Load();
        string characterName = character != null ? character.CharacterName : "Name";
        PlayerLevelSnapshot levelSnapshot = PlayerExperienceStorage.Snapshot;

        SetText(characterName, levelSnapshot);
        SetExperience(levelSnapshot);
        SetAvatar(character);
    }

    private void LoadTexts()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;

            string textName = text.name.ToLowerInvariant();

            if (characterNameText == null && textName.Contains("name"))
            {
                characterNameText = text;
                continue;
            }

            if (levelText == null &&
                (textName.Contains("level") || textName.Contains("lvl")))
            {
                levelText = text;
                continue;
            }

            if (experienceText == null &&
                (textName.Contains("experience") || textName.Contains("exp") || textName.Contains("xp")))
            {
                experienceText = text;
            }
        }
    }

    private void LoadExperienceSlider()
    {
        if (experienceSlider != null) return;

        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            if (slider == null) continue;

            string sliderName = slider.name.ToLowerInvariant();
            if (sliderName.Contains("experience") || sliderName.Contains("exp") || sliderName.Contains("xp"))
            {
                experienceSlider = slider;
                return;
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

    private void LoadAvatarCatalog()
    {
        avatarCatalog ??= Resources.Load<CharacterClassAvatarCatalog>("CharacterClasses/CharacterClassAvatarCatalog");
    }

    private void SetText(string characterName, PlayerLevelSnapshot levelSnapshot)
    {
        if (characterNameText != null)
        {
            characterNameText.text = levelText == null
                ? characterName + " - Lv. " + levelSnapshot.Level
                : characterName;
        }

        if (levelText != null)
            levelText.text = levelSnapshot.Level.ToString();
    }

    private void SetExperience(PlayerLevelSnapshot levelSnapshot)
    {
        if (experienceText != null)
        {
            experienceText.text = levelSnapshot.IsMaxLevel
                ? "MAX"
                : levelSnapshot.ExperienceIntoLevel.ToString("N0")
                  + " / "
                  + levelSnapshot.ExperienceToNextLevel.ToString("N0")
                  + " XP";
        }

        if (experienceSlider == null) return;

        experienceSlider.minValue = 0f;
        experienceSlider.maxValue = 1f;
        experienceSlider.wholeNumbers = false;
        experienceSlider.value = levelSnapshot.Progress01;
    }

    private void SetAvatar(CreatedCharacterData character)
    {
        if (avatarImage == null) return;

        Sprite avatar = avatarCatalog != null
            ? avatarCatalog.GetAvatar(character != null ? character.CharacterClass : CharacterClass.Knight)
            : avatarImage.sprite;
        avatarImage.sprite = avatar;
        avatarImage.enabled = avatar != null;
    }

    private void HandleLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        Refresh();
    }
}
