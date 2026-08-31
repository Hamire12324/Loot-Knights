public class ForgeEnhanceInventoryView : InventoryView
{
    // Selection-only inventory for the forge. The shared base deliberately
    // leaves filtering, selling, arranging and slot reordering disabled.
    protected override bool ShowEmptySlots => false;

    protected override bool IsItemVisible(ItemDefinition item)
    {
        return item != null && item.Category == ItemCategory.Equipment;
    }
}
