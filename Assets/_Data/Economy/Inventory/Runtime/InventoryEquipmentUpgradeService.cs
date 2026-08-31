public static class InventoryEquipmentUpgradeService
{
    public static InventoryOperationResult TryUpgradeAtSlot(InventoryContainer container, int index)
    {
        InventorySlotData slot = container != null ? container.GetSlot(index) : null;
        if (slot == null)
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidSlot, InventoryChangeType.Updated);

        if (slot.IsEmpty)
            return InventoryOperationResult.Failed(InventoryOperationStatus.EmptySlot, InventoryChangeType.Updated);

        if (slot.Item == null || slot.Item.Category != ItemCategory.Equipment ||
            slot.EquipmentInstance == null || !slot.EquipmentInstance.IsValid)
        {
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidItem, InventoryChangeType.Updated);
        }

        if (!EquipmentUpgradeService.TryUpgrade(slot.Item, slot.EquipmentInstance, 1))
            return InventoryOperationResult.Failed(InventoryOperationStatus.InvalidAmount, InventoryChangeType.Updated);

        return InventoryOperationResult.Succeeded(InventoryChangeType.Updated, new[] { index });
    }
}
