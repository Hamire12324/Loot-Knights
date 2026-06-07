using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenuEquipmentPanel : BaseMonoBehaviour
{
    private const string LegacyEquipmentSlotPrefix = "btn_prop";
    private const string EquipmentSlotPrefix = "equipmentslot_";

    [Header("Profile")]
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image heroImage;
    [SerializeField] private Sprite defaultHeroImage;
    [SerializeField] private CharacterClassAvatar[] heroImages;

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
        LoadTexts();
        LoadHeroImage();
        LoadEquipmentSlots();
    }

    public void Refresh()
    {
        LoadComponents();

        CreatedCharacterData character = CharacterProfileStorage.Load();
        string characterName = character != null && !string.IsNullOrWhiteSpace(character.CharacterName)
            ? character.CharacterName
            : "Hero";
        string className = character != null ? character.CharacterClass.ToString() : string.Empty;

        if (characterNameText != null)
            characterNameText.text = characterName;

        RefreshHeroImage(character);
        RefreshEquipmentSlots();
    }

    private void RefreshHeroImage(CreatedCharacterData character)
    {
        if (heroImage == null) return;

        Sprite sprite = character != null ? GetHeroImage(character.CharacterClass) : defaultHeroImage;
        if (sprite != null)
            heroImage.sprite = sprite;
        else if (heroImage.sprite == null)
            heroImage.sprite = defaultHeroImage;

        heroImage.enabled = heroImage.sprite != null;
        heroImage.preserveAspect = true;
    }

    private void RefreshEquipmentSlots()
    {
        for (int i = 0; i < equipmentSlots.Count; i++)
            equipmentSlots[i]?.Refresh();
    }

    private Sprite GetHeroImage(CharacterClass characterClass)
    {
        if (heroImages != null)
        {
            foreach (CharacterClassAvatar avatar in heroImages)
            {
                if (avatar != null && avatar.CharacterClass == characterClass)
                    return avatar.Avatar;
            }
        }

        return defaultHeroImage;
    }

    private void LoadManagers()
    {
        if (equipmentManager == null)
            equipmentManager = PlayerEquipmentManager.InstanceOrNull;

        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);

        if (equipmentManager == null)
            equipmentManager = CreateEquipmentManager();

        if (inventoryManager == null)
            inventoryManager = PlayerInventoryManager.InstanceOrNull;

        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
    }

    private PlayerEquipmentManager CreateEquipmentManager()
    {
        GameObject host = PlayerInventoryManager.InstanceOrNull != null
            ? PlayerInventoryManager.InstanceOrNull.gameObject
            : new GameObject("PlayerEquipmentManager");

        PlayerEquipmentManager manager = host.GetComponent<PlayerEquipmentManager>();
        if (manager == null)
            manager = host.AddComponent<PlayerEquipmentManager>();

        return manager;
    }

    private void LoadTexts()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;

            string textName = text.name.ToLowerInvariant();

            if (characterNameText == null && (textName.Contains("rolename") || textName.Contains("charactername") || textName.Contains("name")))
            {
                characterNameText = text;
                continue;
            }
        }
    }

    private void LoadHeroImage()
    {
        if (heroImage != null) return;

        Transform imageTransform = transform.Find("Image");
        if (imageTransform != null)
        {
            heroImage = imageTransform.GetComponent<Image>();
            if (heroImage != null)
            {
                defaultHeroImage = heroImage.sprite;
                return;
            }
        }

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image == null || image.transform == transform) continue;

            string imageName = image.name.ToLowerInvariant();
            if (imageName.Contains("hero") || imageName.Contains("character") || imageName.Contains("avatar") || imageName.Contains("portrait"))
            {
                heroImage = image;
                defaultHeroImage = heroImage.sprite;
                return;
            }
        }
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

    private static bool IsEquipmentSlotObjectName(string objectName)
    {
        return !string.IsNullOrWhiteSpace(objectName) &&
               (objectName.StartsWith(EquipmentSlotPrefix) ||
                objectName.StartsWith(LegacyEquipmentSlotPrefix));
    }

    private static int ParseSlotIndex(string objectName)
    {
        int enumIndex = ParseSlotTypeIndex(objectName);
        if (enumIndex >= 0)
            return enumIndex;

        int open = objectName.LastIndexOf('(');
        int close = objectName.LastIndexOf(')');

        if (open < 0 || close <= open)
            return 0;

        string indexText = objectName.Substring(open + 1, close - open - 1);
        return int.TryParse(indexText, out int index) ? Mathf.Max(0, index) : 0;
    }

    private static int ParseSlotTypeIndex(string objectName)
    {
        EquipmentSlotType[] slotTypes = GetSlotTypes();

        for (int i = 0; i < slotTypes.Length; i++)
        {
            if (objectName.Contains(slotTypes[i].ToString().ToLowerInvariant()))
                return i;
        }

        return -1;
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

    private void OnValidate()
    {
        equipmentSlots ??= new List<CharacterEquipmentSlotUI>();
    }
}
