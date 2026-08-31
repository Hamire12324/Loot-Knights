using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterEquipmentSlotUI : BaseMonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text upgradeLevelText;

    public EquipmentSlotType SlotType => slotType;

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
        LoadButton();
        LoadIconImage();
        LoadUpgradeLevelText();
        BindButton();
    }

    public void Configure(
        EquipmentSlotType targetSlotType,
        PlayerEquipmentManager targetEquipmentManager,
        PlayerInventoryManager targetInventoryManager)
    {
        slotType = targetSlotType;
        equipmentManager = targetEquipmentManager;
        inventoryManager = targetInventoryManager;

        LoadComponents();
        Subscribe();
        Refresh();
    }

    public void Refresh()
    {
        LoadComponents();

        ItemDefinition item = equipmentManager != null ? equipmentManager.GetItem(slotType) : null;
        EquipmentInstanceData equipmentInstance = equipmentManager != null
            ? equipmentManager.GetEquipmentInstance(slotType)
            : null;
        bool hasItem = item != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasItem ? item.Icon : null;
            iconImage.enabled = hasItem && item.Icon != null;
            iconImage.preserveAspect = true;
        }

        if (upgradeLevelText != null)
            upgradeLevelText.text = equipmentInstance != null && equipmentInstance.UpgradeLevel > 0
                ? "+" + equipmentInstance.UpgradeLevel
                : string.Empty;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI sourceSlot = InventorySlotUI.DraggingSlot;
        if (sourceSlot == null || !sourceSlot.HasItem) return;

        LoadComponents();

        bool equipped = equipmentManager != null && equipmentManager.Equip(
            slotType,
            sourceSlot.CurrentItem,
            inventoryManager,
            sourceSlot.CurrentInventoryIndex);

        if (!equipped)
            return;

        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null && equipmentManager != null && equipmentManager.GetItem(slotType) != null)
            ItemTooltipUI.Move(eventData.position, canvas.rootCanvas);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            ShowTooltip(eventData);
    }

    private void HandleClick()
    {
        LoadComponents();

        if (equipmentManager == null)
            return;

        equipmentManager.Unequip(slotType, inventoryManager);
        Refresh();
    }

    private void ShowTooltip(PointerEventData eventData)
    {
        LoadComponents();
        ItemDefinition item = equipmentManager != null ? equipmentManager.GetItem(slotType) : null;
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (item != null && canvas != null)
            ItemTooltipUI.Show(item, equipmentManager.GetEquipmentInstance(slotType), canvas.rootCanvas, eventData.position);
    }

    private void LoadManagers()
    {
        if (equipmentManager == null)
            equipmentManager = PlayerEquipmentManager.InstanceOrNull;

        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);

        if (inventoryManager == null)
            inventoryManager = PlayerInventoryManager.InstanceOrNull;

        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
    }

    private void LoadButton()
    {
        if (button != null) return;

        button = GetComponent<Button>();
        if (button == null)
            button = GetComponentInChildren<Button>(true);
    }

    private void LoadIconImage()
    {
        if (iconImage != null) return;

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image == null || image.transform == transform) continue;

            string imageName = image.name.ToLowerInvariant();
            if (imageName.Contains("select") || imageName.Contains("frame") || imageName.Contains("outline")) continue;

            iconImage = image;
            return;
        }
    }

    private void LoadUpgradeLevelText()
    {
        if (upgradeLevelText != null) return;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            string textName = text.name.ToLowerInvariant();
            if (textName.Contains("rank") || textName.Contains("level") || textName.Contains("upgrade"))
            {
                upgradeLevelText = text;
                return;
            }
        }
    }

    private void BindButton()
    {
        if (button == null) return;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
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
