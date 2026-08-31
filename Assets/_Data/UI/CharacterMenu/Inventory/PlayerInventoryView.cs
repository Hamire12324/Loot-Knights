using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryView : InventoryView
{
    [Header("Inventory actions")]
    [SerializeField] private Button arrangeButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private InventorySellDropTarget sellDropTarget;

    [Header("Inventory filters")]
    [SerializeField] private Button allFilterButton;
    [SerializeField] private Button equipmentFilterButton;
    [SerializeField] private Button itemsFilterButton;
    [SerializeField] private InventoryFilter currentFilter;

    protected override bool SupportsArrange => true;
    protected override bool SupportsSelling => true;
    protected override bool SupportsFiltering => true;
    protected override bool SupportsSlotReordering => true;

    protected override Button ArrangeButton { get => arrangeButton; set => arrangeButton = value; }
    protected override Button SellButton { get => sellButton; set => sellButton = value; }
    protected override InventorySellDropTarget SellDropTarget { get => sellDropTarget; set => sellDropTarget = value; }
    protected override Button AllFilterButton { get => allFilterButton; set => allFilterButton = value; }
    protected override Button EquipmentFilterButton { get => equipmentFilterButton; set => equipmentFilterButton = value; }
    protected override Button ItemsFilterButton { get => itemsFilterButton; set => itemsFilterButton = value; }
    protected override InventoryFilter CurrentFilter { get => currentFilter; set => currentFilter = value; }
}
