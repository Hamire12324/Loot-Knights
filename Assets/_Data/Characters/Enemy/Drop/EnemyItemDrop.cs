using System.Collections.Generic;
using UnityEngine;

public class EnemyItemDrop : CharacterDrop
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private ItemPickup itemPickupPrefab;
    [SerializeField] private int minItemDrops = 0;
    [SerializeField] private int maxItemDrops = 1;
    [Header("Set Loot Balance")]
    [Tooltip("Applied after choosing an item that belongs to an equipment set. This keeps full sets as a longer-term goal without reducing ordinary loot.")]
    [SerializeField, Range(0f, 1f)] private float equipmentSetDropChance = 0.35f;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadStageManager();
    }

    private void LoadStageManager()
    {
        if (stageManager != null) return;

        stageManager = FindAnyObjectByType<StageManager>(FindObjectsInactive.Include);
    }

    protected override void Drop(CharacterDamReceiver receiver)
    {
        if (itemPickupPrefab == null) return;

        IReadOnlyList<ItemDropEntry> drops = GetCurrentStageDrops();
        if (drops == null || drops.Count == 0) return;

        int dropCount = Random.Range(
            Mathf.Max(0, minItemDrops),
            Mathf.Max(minItemDrops, maxItemDrops) + 1);

        for (int i = 0; i < dropCount; i++)
        {
            ItemDropEntry drop = PickDrop(drops);
            if (drop == null) continue;
            if (drop.Item.EquipmentSet != null && Random.value > equipmentSetDropChance)
                continue;

            SpawnDrop(drop, receiver);
        }
    }

    private IReadOnlyList<ItemDropEntry> GetCurrentStageDrops()
    {
        if (stageManager == null)
            LoadStageManager();

        return stageManager != null && stageManager.CurrentStage != null
            ? stageManager.CurrentStage.ItemDrops
            : null;
    }

    private ItemDropEntry PickDrop(IReadOnlyList<ItemDropEntry> drops)
    {
        float totalWeight = 0f;

        foreach (ItemDropEntry drop in drops)
        {
            if (drop == null || !drop.IsValid) continue;

            totalWeight += drop.Weight;
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);

        foreach (ItemDropEntry drop in drops)
        {
            if (drop == null || !drop.IsValid) continue;

            roll -= drop.Weight;
            if (roll <= 0f)
                return drop;
        }

        return null;
    }

    private void SpawnDrop(ItemDropEntry drop, CharacterDamReceiver receiver)
    {
        ItemPickup pickup = SpawnPickup(receiver);
        if (pickup == null) return;

        pickup.Configure(drop.Item, drop.RollAmount());
    }

    private ItemPickup SpawnPickup(CharacterDamReceiver receiver)
    {
        Vector3 position = GetDropPosition(receiver);

        if (PoolManager.HasInstance)
            return PoolManager.InstanceOrNull.Spawn(itemPickupPrefab, position, Quaternion.identity) as ItemPickup;

        return Instantiate(itemPickupPrefab, position, Quaternion.identity);
    }
}
