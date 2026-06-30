using System.Collections.Generic;
using UnityEngine;

public class PoolManager : BaseSingleton<PoolManager>
{
    [SerializeField] private Transform defaultParent;
    public Transform DefaultParent => defaultParent != null ? defaultParent : transform;

    [SerializeField] private List<PoolConfig> poolConfigs = new();

    private readonly Dictionary<string, RuntimePool> poolsByKey = new();
    private readonly Dictionary<PoolObj, RuntimePool> poolsByPrefab = new();

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        BuildPools();
    }

    private void BuildPools()
    {
        poolsByKey.Clear();
        poolsByPrefab.Clear();

        foreach (PoolConfig config in poolConfigs)
        {
            if (config == null || config.Prefab == null) continue;

            RuntimePool pool = CreateRuntimePool(config);
            RegisterPool(pool);
            Preload(pool);
        }
    }

    private RuntimePool CreateRuntimePool(PoolConfig config)
    {
        Transform parent = config.Parent != null
            ? config.Parent
            : CreatePoolParent(config);

        return new RuntimePool
        {
            Config = config,
            Parent = parent
        };
    }

    private void RegisterPool(RuntimePool pool)
    {
        poolsByPrefab[pool.Config.Prefab] = pool;

        if (!string.IsNullOrWhiteSpace(pool.Config.Key))
            poolsByKey[pool.Config.Key] = pool;
    }

    private void Preload(RuntimePool pool)
    {
        int count = Mathf.Min(pool.Config.PreloadAmount, pool.Config.MaxSize);

        for (int i = 0; i < count; i++)
        {
            PoolObj obj = CreateObject(pool);
            ReturnToInactive(pool, obj);
        }
    }

    public PoolObj Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolsByKey.TryGetValue(key, out RuntimePool pool))
        {
            Debug.LogError($"Pool key not found: {key}", gameObject);
            return null;
        }

        return Spawn(pool, position, rotation, parent);
    }

    public PoolObj Spawn(PoolObj prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        if (!poolsByPrefab.TryGetValue(prefab, out RuntimePool pool))
        {
            pool = CreateRuntimePool(new PoolConfig
            {
                Key = prefab.name,
                Prefab = prefab,
                PreloadAmount = 0,
                MaxSize = 30,
                CanExpand = true,
                Parent = null
            });

            RegisterPool(pool);
        }

        return Spawn(pool, position, rotation, parent);
    }

    private PoolObj Spawn(RuntimePool pool, Vector3 position, Quaternion rotation, Transform parent)
    {
        PoolObj obj = GetObject(pool);
        if (obj == null) return null;

        Transform objTransform = obj.transform;
        Transform spawnParent = parent != null ? parent : pool.Parent;
        objTransform.SetParent(spawnParent, true);
        objTransform.SetPositionAndRotation(position, rotation);

        obj.gameObject.SetActive(true);
        pool.ActiveObjects.Add(obj);
        obj.OnSpawnedFromPool();

        if (!obj.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                $"{obj.name} was spawned but is not active in hierarchy. Check if its parent is inactive.",
                obj.gameObject
            );
        }

        return obj;
    }

    private PoolObj GetObject(RuntimePool pool)
    {
        while (pool.InactiveObjects.Count > 0)
        {
            PoolObj obj = pool.InactiveObjects.Dequeue();
            if (obj != null) return obj;
        }

        if (!pool.Config.CanExpand && pool.TotalCount >= pool.Config.MaxSize)
            return null;

        return CreateObject(pool);
    }

    private PoolObj CreateObject(RuntimePool pool)
    {
        PoolObj obj = Instantiate(pool.Config.Prefab, pool.Parent);
        obj.InitPool(this, pool.Config.Key);
        obj.gameObject.SetActive(false);
        return obj;
    }

    public void Despawn(PoolObj obj)
    {
        if (obj == null) return;
        if (obj.IsInPool) return;

        RuntimePool pool = FindPool(obj);
        if (pool == null)
        {
            obj.gameObject.SetActive(false);
            return;
        }

        if (!pool.ActiveObjects.Remove(obj))
            return;

        ReturnToInactive(pool, obj);
    }

    private RuntimePool FindPool(PoolObj obj)
    {
        if (!string.IsNullOrWhiteSpace(obj.PoolKey) &&
            poolsByKey.TryGetValue(obj.PoolKey, out RuntimePool pool))
        {
            return pool;
        }

        foreach (RuntimePool runtimePool in poolsByPrefab.Values)
        {
            if (runtimePool.ActiveObjects.Contains(obj))
                return runtimePool;
        }

        return null;
    }

    private void ReturnToInactive(RuntimePool pool, PoolObj obj)
    {
        obj.OnReturnedToPool();
        obj.transform.SetParent(pool.Parent, false);
        obj.gameObject.SetActive(false);
        pool.InactiveObjects.Enqueue(obj);
    }

    private Transform CreatePoolParent(PoolConfig config)
    {
        string parentName = !string.IsNullOrWhiteSpace(config.Key)
            ? $"{config.Key}_Pool"
            : $"{config.Prefab.name}_Pool";

        GameObject parentObj = new(parentName);
        parentObj.transform.SetParent(transform);
        return parentObj.transform;
    }
}
