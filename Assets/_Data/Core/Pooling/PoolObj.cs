using UnityEngine;

[DisallowMultipleComponent]
public class PoolObj : BaseMonoBehaviour
{
    [SerializeField] private PoolManager ownerPool;
    [SerializeField] private string poolKey;

    public string PoolKey => poolKey;
    public bool IsInPool { get; private set; }

    public void InitPool(PoolManager pool, string key)
    {
        ownerPool = pool;
        poolKey = key;
    }

    public virtual void OnSpawnedFromPool()
    {
        IsInPool = false;
    }

    public virtual void OnReturnedToPool()
    {
        IsInPool = true;
    }

    public void ReturnToPool()
    {
        if (ownerPool == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ownerPool.Despawn(this);
    }
}
