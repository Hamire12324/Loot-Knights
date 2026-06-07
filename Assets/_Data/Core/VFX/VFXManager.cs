using UnityEngine;

public class VFXManager : BaseSingleton<VFXManager>
{
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private Transform defaultParent;
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadPoolManager();
        LoadDefaultParent();
    }

    private void LoadPoolManager()
    {
        if (poolManager != null)
            return;

        poolManager = PoolManager.HasInstance
            ? PoolManager.InstanceOrNull
            : FindAnyObjectByType<PoolManager>(FindObjectsInactive.Exclude);
    }

    private void LoadDefaultParent()
    {
        if (defaultParent != null || poolManager == null)
            return;

        defaultParent = poolManager.DefaultParent;
    }

    public PoolObj Play(
        VFXDefinition definition,
        Vector3 position,
        Vector3 direction,
        Transform anchor = null)
    {
        if (definition == null || definition.Prefab == null)
            return null;

        if (poolManager == null)
            return null;

        Vector3 finalOffset = definition.Offset;
        bool mirrorHorizontally = definition.MirrorHorizontallyByDirection && direction.x < -0.01f;

        if (mirrorHorizontally)
            finalOffset.x = -finalOffset.x;

        Vector3 finalPosition = position + finalOffset;
        Transform parent = definition.ParentToAnchor ? anchor : defaultParent;

        PoolObj spawned = poolManager.Spawn(definition.Prefab, finalPosition, Quaternion.identity, parent);
        ApplyRendererFlip(spawned, definition.FlipX ^ mirrorHorizontally, definition.FlipY);
        return spawned;
    }

    private static void ApplyRendererFlip(PoolObj spawned, bool flipX, bool flipY)
    {
        if (spawned == null)
            return;

        VFXPoolObj vfxPoolObj = spawned as VFXPoolObj;
        if (vfxPoolObj == null)
            vfxPoolObj = spawned.GetComponent<VFXPoolObj>();

        vfxPoolObj?.SetRendererFlip(flipX, flipY);
    }
}
