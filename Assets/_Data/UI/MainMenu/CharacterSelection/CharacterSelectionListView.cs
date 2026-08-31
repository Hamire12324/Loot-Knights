using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionListView : BaseMonoBehaviour
{
    [SerializeField] private CharacterSelectionSlotView characterSlotTemplate;
    [SerializeField] private Button createCharacterButton;

    public event Action<CreatedCharacterData> OnCharacterSelected;
    public event Action<CreatedCharacterData> OnCharacterDeleteRequested;
    public event Action OnCreateCharacterRequested;

    private readonly List<CharacterSelectionSlotView> spawnedSlots = new();
    protected override void OnEnable()
    {
        if (createCharacterButton != null)
            createCharacterButton.onClick.AddListener(RequestCreateCharacter);
    }

    protected override void OnDisable()
    {
        if (createCharacterButton != null)
            createCharacterButton.onClick.RemoveListener(RequestCreateCharacter);
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        characterSlotTemplate ??= GetComponentInChildren<CharacterSelectionSlotView>(true);
        createCharacterButton ??= FindDirectChildButton("CreateCharacterButton");
    }
    public void ShowCharacters(IReadOnlyList<CreatedCharacterData> characters, bool canCreateCharacter, Func<CreatedCharacterData, int> getLevel)
    {
        if (characterSlotTemplate == null)
        {
            Debug.LogError("CharacterSelectionListView: Missing CharacterSlotTemplate.", gameObject);
            return;
        }

        characterSlotTemplate.gameObject.SetActive(false);
        ClearSlots();

        foreach (CreatedCharacterData character in characters)
        {
            if (character == null) continue;

            CharacterSelectionSlotView slot = Instantiate(characterSlotTemplate, transform);
            slot.name = $"CharacterSlot_{character.CharacterName}";
            slot.gameObject.SetActive(true);

            if (createCharacterButton != null && createCharacterButton.transform.parent == transform)
                slot.transform.SetSiblingIndex(createCharacterButton.transform.GetSiblingIndex());

            slot.Bind(
                character,
                getLevel(character),
                () => OnCharacterSelected?.Invoke(character),
                () => OnCharacterDeleteRequested?.Invoke(character));
            spawnedSlots.Add(slot);
        }

        if (createCharacterButton != null)
            createCharacterButton.gameObject.SetActive(canCreateCharacter);
    }

    private void RequestCreateCharacter()
    {
        OnCreateCharacterRequested?.Invoke();
    }

    private void ClearSlots()
    {
        foreach (CharacterSelectionSlotView slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }

    private Button FindDirectChildButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == objectName && button.transform.parent == transform)
                return button;
        }

        return null;
    }
}
