using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenuEquipmentPanel : BaseMonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image heroImage;
    [SerializeField] private CharacterClassAvatarCatalog avatarCatalog;

    [Header("Equipment")]
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private List<CharacterEquipmentSlotUI> equipmentSlots = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        LoadComponents();
        Subscribe();
        Refresh();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadManagers();
        LoadCharacterNameText();
        LoadHeroImage();
        LoadAvatarCatalog();
        LoadEquipmentSlots();
    }
    private void LoadManagers()
    {
        if (this.equipmentManager == null)
        {
            this.equipmentManager = FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);
        }

        if (this.inventoryManager == null)
        {
            this.inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        }
    }
    private void LoadCharacterNameText()
    {
        if(this.characterNameText != null) return;
        this.characterNameText ??= transform
            .Find("CharacternameTxt")
            ?.GetComponentInChildren<TextMeshProUGUI>(true);
    }
    private void LoadHeroImage()
    {
        if(this.heroImage != null) return;
        this.heroImage ??= transform
            .Find("HeroImage")
            ?.GetComponentInChildren<Image>(true);
    }

    private void LoadAvatarCatalog()
    {
        avatarCatalog ??= Resources.Load<CharacterClassAvatarCatalog>("CharacterClasses/CharacterClassAvatarCatalog");
    }

    private void LoadEquipmentSlots()
    {
        equipmentSlots.Clear();

        Dictionary<EquipmentSlotType, CharacterEquipmentSlotUI> slotsByType = new();

        foreach (CharacterEquipmentSlotUI slot in GetComponentsInChildren<CharacterEquipmentSlotUI>(true))
        {
            if (slot == null) continue;

            if (!TryGetSlotTypeFromName(slot.gameObject.name, out EquipmentSlotType slotType))
                continue;

            slotsByType[slotType] = slot;
        }

        foreach (EquipmentSlotType slotType in GetSlotTypes())
        {
            if (!slotsByType.TryGetValue(slotType, out CharacterEquipmentSlotUI slot))
                continue;

            slot.Configure(slotType, equipmentManager, inventoryManager);
            equipmentSlots.Add(slot);
        }
    }
    public void Refresh()
    {
        LoadComponents();

        CreatedCharacterData character = CharacterProfileStorage.Load();
        string characterName = character != null && !string.IsNullOrWhiteSpace(character.CharacterName)
            ? character.CharacterName
            : "Hero";

        if (characterNameText != null)
            characterNameText.text = characterName;

        RefreshHeroImage(character);
        RefreshEquipmentSlots();
    }

    private void RefreshHeroImage(CreatedCharacterData character)
    {
        if (heroImage == null) return;

        Sprite sprite = avatarCatalog != null && character != null
            ? avatarCatalog.GetAvatar(character.CharacterClass)
            : avatarCatalog != null ? avatarCatalog.GetAvatar(CharacterClass.Knight) : heroImage.sprite;
        if (sprite != null)
            heroImage.sprite = sprite;

        heroImage.enabled = heroImage.sprite != null;
        heroImage.preserveAspect = true;
    }

    private void RefreshEquipmentSlots()
    {
        for (int i = 0; i < equipmentSlots.Count; i++)
            equipmentSlots[i]?.Refresh();
    }
    private static bool TryGetSlotTypeFromName(string objectName, out EquipmentSlotType slotType)
    {
        slotType = default;

        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        string lowerName = objectName.ToLowerInvariant();

        foreach (EquipmentSlotType type in GetSlotTypes())
        {
            string typeName = type.ToString().ToLowerInvariant();

            if (lowerName == typeName ||
                lowerName.Contains(typeName) ||
                lowerName == $"equipmentslot_{typeName}" ||
                lowerName == $"btn_prop{typeName}")
            {
                slotType = type;
                return true;
            }
        }

        return false;
    }
    private static EquipmentSlotType[] GetSlotTypes()
    {
        return (EquipmentSlotType[])Enum.GetValues(typeof(EquipmentSlotType));
    }

    private void Subscribe()
    {
        if (equipmentManager == null) return;

        equipmentManager.OnEquipmentChanged -= Refresh;
        equipmentManager.OnEquipmentChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (equipmentManager == null) return;

        equipmentManager.OnEquipmentChanged -= Refresh;
    }
}
