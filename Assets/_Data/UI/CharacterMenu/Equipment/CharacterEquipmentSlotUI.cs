using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterEquipmentSlotUI : BaseMonoBehaviour, IDropHandler
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

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
        bool hasItem = item != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasItem ? item.Icon : null;
            iconImage.enabled = hasItem && item.Icon != null;
            iconImage.preserveAspect = true;
        }
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

    private void HandleClick()
    {
        LoadComponents();

        if (equipmentManager == null)
            return;

        equipmentManager.Unequip(slotType, inventoryManager);
        Refresh();
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
