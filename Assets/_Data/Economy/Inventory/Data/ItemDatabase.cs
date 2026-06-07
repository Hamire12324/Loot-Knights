using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public const string DefaultResourcesPath = "Inventory/ItemDatabase";

    [SerializeField] private List<ItemDefinition> items = new();

    private Dictionary<string, ItemDefinition> itemsById;

    public IReadOnlyList<ItemDefinition> Items => items;

    public static ItemDatabase LoadDefault()
    {
        return Resources.Load<ItemDatabase>(DefaultResourcesPath);
    }

    public bool TryGetItem(string itemId, out ItemDefinition item)
    {
        EnsureLookup();

        if (string.IsNullOrWhiteSpace(itemId))
        {
            item = null;
            return false;
        }

        return itemsById.TryGetValue(itemId, out item);
    }

    public ItemDefinition GetItem(string itemId)
    {
        return TryGetItem(itemId, out ItemDefinition item) ? item : null;
    }

    private void EnsureLookup()
    {
        if (itemsById != null) return;

        itemsById = new Dictionary<string, ItemDefinition>();

        foreach (ItemDefinition item in items)
        {
            if (item == null || !item.IsValid) continue;

            if (itemsById.ContainsKey(item.ItemId))
            {
                Debug.LogWarning(name + ": Duplicate item id '" + item.ItemId + "'.", this);
                continue;
            }

            itemsById.Add(item.ItemId, item);
        }
    }

    private void OnValidate()
    {
        items.RemoveAll(item => item == null);
        itemsById = null;
    }
}
