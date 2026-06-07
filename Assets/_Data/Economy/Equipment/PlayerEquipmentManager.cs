using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipmentManager : BaseSingleton<PlayerEquipmentManager>
{
    public event Action OnEquipmentChanged;

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private EquipmentInventory equipmentInventory = new();

    private bool loaded;

    public IReadOnlyList<EquipmentSlotData> EquippedSlots
    {
        get
        {
            EnsureLoaded();
            return equipmentInventory.Slots;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (InstanceOrNull != this) return;

        if (loadOnAwake)
            EnsureLoaded();
    }

    protected override void OnDisable()
    {
        if (autoSave && loaded)
            Save();

        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (itemDatabase == null)
            itemDatabase = ItemDatabase.LoadDefault();

        EnsureEquipmentInventory();
    }

    public ItemDefinition GetItem(EquipmentSlotType slotType)
    {
        EnsureLoaded();

        return equipmentInventory.GetItem(slotType);
    }

    public int GetUpgradeLevel(ItemDefinition item)
    {
        return EquipmentUpgradeStorage.GetLevel(item);
    }

    public int GetUpgradeLevel(EquipmentSlotType slotType)
    {
        EquipmentInstanceData instance = GetEquipmentInstance(slotType);
        return instance != null ? instance.UpgradeLevel : GetUpgradeLevel(GetItem(slotType));
    }

    public EquipmentInstanceData GetEquipmentInstance(EquipmentSlotType slotType)
    {
        EnsureLoaded();

        return equipmentInventory.GetInstance(slotType);
    }

    public bool CanEquip(ItemDefinition item, EquipmentSlotType slotType)
    {
        return item != null
               && item.IsValid
               && item.Category == ItemCategory.Equipment
               && item.EquipmentSlotType == slotType;
    }
    public bool Equip(
        EquipmentSlotType slotType,
        ItemDefinition item,
        PlayerInventoryManager inventoryManager = null,
        int inventoryIndex = -1)
    {
        EnsureLoaded();

        if (!CanEquip(item, slotType))
            return false;

        EquipmentInstanceData equipmentInstance = ResolveEquipmentInstance(item, inventoryManager, inventoryIndex);
        ItemDefinition previousItem = equipmentInventory.GetItem(slotType);
        EquipmentInstanceData previousInstance = equipmentInventory.GetInstance(slotType);

        if (previousInstance != null && equipmentInstance != null &&
            previousInstance.InstanceId == equipmentInstance.InstanceId)
            return false;

        if (!CanReturnPreviousItem(previousItem, inventoryManager, inventoryIndex))
            return false;

        if (!RemoveFromInventory(item, inventoryManager, inventoryIndex))
            return false;

        equipmentInventory.SetItem(slotType, item, equipmentInstance);

        if (previousItem != null)
            ReturnPreviousItemToInventory(previousItem, previousInstance, inventoryManager);

        Commit();
        return true;
    }

    public bool UpgradeEquippedItem(EquipmentSlotType slotType, int levels = 1)
    {
        EnsureLoaded();

        ItemDefinition item = equipmentInventory.GetItem(slotType);
        if (item == null || item.MaxUpgradeLevel <= 0)
            return false;

        EquipmentInstanceData instance = equipmentInventory.GetInstance(slotType);
        if (instance == null)
        {
            instance = CreateLegacyInstance(item);
            equipmentInventory.SetItem(slotType, item, instance);
        }

        int previousLevel = instance.UpgradeLevel;
        instance.AddUpgradeLevels(levels, item.MaxUpgradeLevel);
        int nextLevel = instance.UpgradeLevel;

        if (nextLevel == previousLevel)
            return false;

        Commit();
        return true;
    }

    public bool UpgradeItem(ItemDefinition item, int levels = 1)
    {
        return UpgradeEquippedInstance(item, levels);
    }

    public bool UpgradeItemById(string itemId, int levels = 1)
    {
        EnsureLoaded();

        if (itemDatabase == null || !itemDatabase.TryGetItem(itemId, out ItemDefinition item))
            return false;

        return UpgradeEquippedInstance(item, levels);
    }

    public bool Unequip(EquipmentSlotType slotType, PlayerInventoryManager inventoryManager = null)
    {
        EnsureLoaded();

        ItemDefinition item = equipmentInventory.GetItem(slotType);
        EquipmentInstanceData instance = equipmentInventory.GetInstance(slotType);
        if (item == null)
            return false;

        if (inventoryManager != null && !inventoryManager.Inventory.CanAddItem(item, 1))
            return false;

        equipmentInventory.ClearSlot(slotType);
        ReturnPreviousItemToInventory(item, instance, inventoryManager);

        Commit();
        return true;
    }

    public void Reload()
    {
        loaded = false;
        EnsureLoaded();
        OnEquipmentChanged?.Invoke();
    }

    public void Clear()
    {
        EnsureLoaded();

        equipmentInventory.ClearAll();

        EquipmentSaveService.Clear();
        OnEquipmentChanged?.Invoke();
    }

    private void EnsureLoaded()
    {
        if (loaded) return;

        if (itemDatabase == null)
            itemDatabase = ItemDatabase.LoadDefault();

        EnsureEquipmentInventory();
        equipmentInventory.EnsureDefaultSlots();
        equipmentInventory.ClearAll();

        IReadOnlyList<EquipmentItemSaveData> savedItems = EquipmentSaveService.LoadItems();
        foreach (EquipmentItemSaveData savedItem in savedItems)
        {
            if (savedItem == null || string.IsNullOrWhiteSpace(savedItem.ItemId)) continue;
            if (itemDatabase == null || !itemDatabase.TryGetItem(savedItem.ItemId, out ItemDefinition item)) continue;
            if (!CanEquip(item, savedItem.SlotType)) continue;

            EquipmentInstanceData instance = savedItem.HasEquipmentInstance
                ? savedItem.EquipmentInstance
                : CreateLegacyInstance(item);

            equipmentInventory.SetItem(savedItem.SlotType, item, instance);
        }

        loaded = true;
        ApplyToLocalHero();
    }

    private void EnsureEquipmentInventory()
    {
        equipmentInventory ??= new EquipmentInventory();
    }

    private bool RemoveFromInventory(ItemDefinition item, PlayerInventoryManager inventoryManager, int inventoryIndex)
    {
        if (inventoryManager == null)
            return true;

        if (inventoryIndex >= 0)
        {
            InventorySlotData slot = inventoryManager.Inventory.GetSlot(inventoryIndex);
            if (slot == null || slot.IsEmpty || slot.Item != item)
                return false;

            InventoryOperationResult result = inventoryManager.RemoveItemAtSlot(inventoryIndex, 1);
            return result != null && result.Success;
        }

        InventoryOperationResult removeResult = inventoryManager.TryRemoveItem(item, 1);
        return removeResult != null && removeResult.Success;
    }

    private EquipmentInstanceData ResolveEquipmentInstance(
        ItemDefinition item,
        PlayerInventoryManager inventoryManager,
        int inventoryIndex)
    {
        if (inventoryManager != null && inventoryIndex >= 0)
        {
            EquipmentInstanceData instance = inventoryManager.GetEquipmentInstanceAtSlot(inventoryIndex);
            if (instance != null && instance.IsValid)
                return instance.Clone();
        }

        return item != null ? item.CreateEquipmentInstance() : null;
    }

    private EquipmentInstanceData CreateLegacyInstance(ItemDefinition item)
    {
        if (item == null) return null;

        EquipmentInstanceData instance = item.CreateEquipmentInstance();
        instance?.SetUpgradeLevel(EquipmentUpgradeStorage.GetLevel(item), item.MaxUpgradeLevel);
        return instance;
    }

    private bool UpgradeEquippedInstance(ItemDefinition item, int levels)
    {
        EnsureLoaded();

        if (item == null || item.MaxUpgradeLevel <= 0)
            return false;

        foreach (EquipmentSlotData slot in equipmentInventory.Slots)
        {
            if (slot == null || slot.Item != item) continue;

            EquipmentInstanceData instance = slot.EquipmentInstance;
            if (instance == null)
            {
                instance = CreateLegacyInstance(item);
                slot.Set(item, instance);
            }

            int previousLevel = instance.UpgradeLevel;
            instance.AddUpgradeLevels(levels, item.MaxUpgradeLevel);

            if (instance.UpgradeLevel == previousLevel)
                return false;

            Commit();
            return true;
        }

        return false;
    }

    private void ReturnPreviousItemToInventory(
        ItemDefinition item,
        EquipmentInstanceData instance,
        PlayerInventoryManager inventoryManager)
    {
        if (item == null || inventoryManager == null)
            return;

        if (instance != null && instance.IsValid)
            inventoryManager.AddEquipmentInstance(item, instance);
        else
            inventoryManager.AddItem(item, 1);
    }

    private bool CanReturnPreviousItem(
        ItemDefinition previousItem,
        PlayerInventoryManager inventoryManager,
        int inventoryIndex)
    {
        if (previousItem == null || inventoryManager == null)
            return true;

        if (inventoryManager.Inventory.CanAddItem(previousItem, 1))
            return true;

        if (inventoryIndex < 0)
            return false;

        InventorySlotData sourceSlot = inventoryManager.Inventory.GetSlot(inventoryIndex);
        return sourceSlot != null && !sourceSlot.IsEmpty && sourceSlot.Amount <= 1;
    }

    private void Commit()
    {
        if (autoSave)
            Save();

        ApplyToLocalHero();
        OnEquipmentChanged?.Invoke();
    }

    public void ApplyToLocalHero()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero != null)
            ApplyToHero(hero);
    }

    public void ApplyToHero(HeroCtrl hero)
    {
        if (hero == null || hero.CharacterStat == null) return;

        EnsureLoaded();

        List<StatModifier> equipmentModifiers = new();

        foreach (EquipmentSlotData slot in equipmentInventory.Slots)
        {
            ItemDefinition item = slot?.Item;
            if (item == null) continue;

            EquipmentInstanceData instance = slot.EquipmentInstance;

            if (instance != null && instance.IsValid)
                equipmentModifiers.AddRange(instance.BuildModifiers(item));
            else
                equipmentModifiers.AddRange(item.BuildEquipmentModifiers(EquipmentUpgradeStorage.GetLevel(item)));
        }

        hero.CharacterStat.RecalculateEquipment(equipmentModifiers);
    }

    [ContextMenu("Apply Equipment To Local Hero")]
    private void ApplyEquipmentToLocalHeroContext()
    {
        ApplyToLocalHero();
    }

    [ContextMenu("Upgrade Equipped Weapon")]
    private void UpgradeEquippedWeaponContext()
    {
        UpgradeEquippedItem(EquipmentSlotType.Weapon);
    }

    [ContextMenu("Upgrade Equipped Armor")]
    private void UpgradeEquippedArmorContext()
    {
        UpgradeEquippedItem(EquipmentSlotType.Armor);
    }

    [ContextMenu("Upgrade Equipped OffHand")]
    private void UpgradeEquippedOffHandContext()
    {
        UpgradeEquippedItem(EquipmentSlotType.OffHand);
    }

    [ContextMenu("Save Equipment Now")]
    public void Save()
    {
        EnsureEquipmentInventory();
        equipmentInventory.EnsureDefaultSlots();
        EquipmentSaveService.SaveItems(equipmentInventory.Slots);
    }
}
