using System;
using UnityEngine;
public class CharacterSelectionPanel : BaseMonoBehaviour
{
    public event Action<CreatedCharacterData> OnCharacterSelected;
    public event Action OnCreateCharacterRequested;
    public event Action OnBackRequested;

    [SerializeField] private CharacterSelectionListView listView;
    [SerializeField] private CharacterSelectionBackButton backButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeViews();
        Refresh();
    }

    protected override void OnDisable()
    {
        UnsubscribeViews();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        listView ??= GetComponentInChildren<CharacterSelectionListView>(true);
        backButton ??= GetComponentInChildren<CharacterSelectionBackButton>(true);
    }

    public void Refresh()
    {
        if (listView == null)
        {
            Debug.LogError("CharacterSelectionPanel: Missing CharacterSelectionListView on Content.", gameObject);
            return;
        }

        listView.ShowCharacters(
            CharacterProfileStorage.GetAll(),
            CharacterProfileStorage.CanCreateCharacter(),
            GetLevel);
    }

    private void SubscribeViews()
    {
        if (listView != null)
        {
            listView.OnCharacterSelected -= SelectCharacter;
            listView.OnCharacterDeleteRequested -= DeleteCharacter;
            listView.OnCreateCharacterRequested -= CreateCharacter;

            listView.OnCharacterSelected += SelectCharacter;
            listView.OnCharacterDeleteRequested += DeleteCharacter;
            listView.OnCreateCharacterRequested += CreateCharacter;
        }

        if (backButton != null)
        {
            backButton.OnClicked -= BackToMainMenu;
            backButton.OnClicked += BackToMainMenu;
        }
    }

    private void UnsubscribeViews()
    {
        if (listView != null)
        {
            listView.OnCharacterSelected -= SelectCharacter;
            listView.OnCharacterDeleteRequested -= DeleteCharacter;
            listView.OnCreateCharacterRequested -= CreateCharacter;
        }

        if (backButton != null)
            backButton.OnClicked -= BackToMainMenu;
    }

    private void SelectCharacter(CreatedCharacterData character)
    {
        OnCharacterSelected?.Invoke(character);
    }

    private void DeleteCharacter(CreatedCharacterData character)
    {
        CharacterProfileStorage.Delete(character);
        Refresh();
    }

    private void CreateCharacter()
    {
        OnCreateCharacterRequested?.Invoke();
    }

    private void BackToMainMenu()
    {
        OnBackRequested?.Invoke();
    }

    private static int GetLevel(CreatedCharacterData character)
    {
        string experienceKey = PlayerExperienceStorage.GetExperienceKey(character.CharacterId);
        int experience = Mathf.Max(PlayerPrefs.GetInt(experienceKey, 0), 0);
        return PlayerLevel.CreateSnapshot(experience).Level;
    }
}
