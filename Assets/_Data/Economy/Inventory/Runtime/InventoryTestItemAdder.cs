using UnityEngine;

public class InventoryTestItemAdder : BaseMonoBehaviour
{
    [SerializeField] private PlayerInventoryManager inventoryManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private ItemDefinition item;
    [SerializeField] private string itemId = "health_potion";
    [SerializeField] private int amount = 1;
    [SerializeField] private bool logResult = true;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadInventoryManager();
        LoadItemDatabase();
    }

    [ContextMenu("Add Test Item")]
    public void AddConfiguredItem()
    {
        InventoryOperationResult result = AddItem(item, itemId, amount);
        LogResult(result);
    }

    public InventoryOperationResult AddItem(ItemDefinition targetItem, int targetAmount = 1)
    {
        InventoryOperationResult result = AddItem(targetItem, null, targetAmount);
        LogResult(result);
        return result;
    }

    public InventoryOperationResult AddItemById(string targetItemId, int targetAmount = 1)
    {
        InventoryOperationResult result = AddItem(null, targetItemId, targetAmount);
        LogResult(result);
        return result;
    }

    private InventoryOperationResult AddItem(ItemDefinition targetItem, string targetItemId, int targetAmount)
    {
        if (inventoryManager == null)
            LoadInventoryManager();

        if (inventoryManager == null)
        {
            Debug.LogError(transform.name + ": Missing PlayerInventoryManager.", gameObject);
            return InventoryOperationResult.Failed(
                InventoryOperationStatus.DatabaseMissing,
                requestedAmount: targetAmount);
        }

        int safeAmount = Mathf.Max(1, targetAmount);
        ItemDefinition resolvedItem = ResolveItem(targetItem, targetItemId);

        if (resolvedItem != null)
            return inventoryManager.AddItem(resolvedItem, safeAmount);

        string resolvedItemId = !string.IsNullOrWhiteSpace(targetItemId) ? targetItemId : itemId;
        return inventoryManager.AddItem(resolvedItemId, safeAmount);
    }

    private ItemDefinition ResolveItem(ItemDefinition targetItem, string targetItemId)
    {
        if (targetItem != null)
            return targetItem;

        if (itemDatabase == null)
            LoadItemDatabase();

        if (itemDatabase == null)
            return null;

        if (!string.IsNullOrWhiteSpace(targetItemId) &&
            itemDatabase.TryGetItem(targetItemId, out ItemDefinition foundByTargetId))
        {
            return foundByTargetId;
        }

        if (!string.IsNullOrWhiteSpace(itemId) &&
            itemDatabase.TryGetItem(itemId, out ItemDefinition foundByConfiguredId))
        {
            return foundByConfiguredId;
        }

        return itemDatabase.Items.Count > 0 ? itemDatabase.Items[0] : null;
    }

    private void LoadInventoryManager()
    {
        if (inventoryManager != null) return;

        if (PlayerInventoryManager.InstanceOrNull != null)
        {
            inventoryManager = PlayerInventoryManager.InstanceOrNull;
            return;
        }

        inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
    }

    private void LoadItemDatabase()
    {
        if (itemDatabase != null) return;

        if (inventoryManager != null && inventoryManager.Database != null)
        {
            itemDatabase = inventoryManager.Database;
            return;
        }

        itemDatabase = ItemDatabase.LoadDefault();
    }

    private void LogResult(InventoryOperationResult result)
    {
        if (!logResult || result == null) return;

        string status = result.Success ? "added" : "failed";
        Debug.Log(
            transform.name + ": Test item " + status
            + ". Status=" + result.Status
            + ", requested=" + result.RequestedAmount
            + ", accepted=" + result.AcceptedAmount
            + ", remaining=" + result.RemainingAmount,
            gameObject);
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }
}
