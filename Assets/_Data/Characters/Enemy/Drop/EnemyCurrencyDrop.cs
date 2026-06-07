using UnityEngine;

public class EnemyCurrencyDrop : CharacterDrop
{
    [SerializeField] private CurrencyPickup coinPickupPrefab;
    [SerializeField] private int minCoinDrops = 1;
    [SerializeField] private int maxCoinDrops = 3;

    protected override void Drop(CharacterDamReceiver receiver)
    {
        if (coinPickupPrefab == null) return;

        int dropCount = Random.Range(
            Mathf.Max(0, minCoinDrops),
            Mathf.Max(minCoinDrops, maxCoinDrops) + 1);

        for (int i = 0; i < dropCount; i++)
        {
            SpawnPickup(receiver);
        }
    }

    private void SpawnPickup(CharacterDamReceiver receiver)
    {
        Vector3 position = GetDropPosition(receiver);

        if (PoolManager.HasInstance)
        {
            PoolManager.InstanceOrNull.Spawn(coinPickupPrefab, position, Quaternion.identity);
            return;
        }

        Instantiate(coinPickupPrefab, position, Quaternion.identity);
    }
}
