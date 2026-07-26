using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ElementalShardPickup : PoolObj
{
    private const string DefaultIconSetResourcePath = "Element/ElementalIconSet";
    private const string ElementalShardPoolKey = "ElementalShardPickup";
    private static readonly Collider2D[] Hits = new Collider2D[64];
    private static ElementalIconSet cachedDefaultIconSet;
    private static bool missingPoolWarningLogged;

    [SerializeField] private ElementType element = ElementType.Fire;
    [SerializeField, Min(0f)] private float power = 1f;
    [SerializeField, Min(0.05f)] private float absorbSpeed = 7.5f;
    [SerializeField, Min(0.02f)] private float collectDistance = 0.18f;
    [SerializeField, Min(0.1f)] private float lifetime = 18f;
    [SerializeField, Min(0.05f)] private float visualSize = 0.32f;
    [SerializeField] private bool useShardVfx = true;
    [SerializeField] private bool hideSpriteWhenVfxAssigned = true;
    [SerializeField] private ElementalIconSet elementalIconSet;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform target;
    private Action<ElementalShardPickup> collectedCallback;
    private PoolObj activeShardVfx;
    private VFXDefinition activeShardVfxDefinition;
    private float spawnTime;
    private bool absorbing;

    public ElementType Element => element;
    public float Power => power;
    public bool IsAvailable => isActiveAndEnabled && !absorbing && element != ElementType.None;

    protected override void OnEnable()
    {
        base.OnEnable();
        spawnTime = Time.time;
        absorbing = false;
        target = null;
        collectedCallback = null;
        ReturnActiveShardVfx();
        RefreshVisual();
    }

    protected override void Update()
    {
        base.Update();

        if (!absorbing)
        {
            if (Time.time - spawnTime >= lifetime)
            {
                ReturnToPool();
                return;
            }

            return;
        }

        if (target == null)
        {
            ReturnToPool();
            return;
        }

        Vector3 targetPosition = target.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            absorbSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) > collectDistance)
            return;

        Action<ElementalShardPickup> callback = collectedCallback;
        callback?.Invoke(this);
        ReturnToPool();
    }

    public void Configure(ElementType shardElement, float shardPower)
    {
        element = shardElement != ElementType.None ? shardElement : ElementType.Fire;
        power = Mathf.Max(0f, shardPower);
        RefreshVisual();
    }

    public void BeginAbsorb(Transform absorbTarget, Action<ElementalShardPickup> onCollected)
    {
        if (!IsAvailable || absorbTarget == null)
            return;

        target = absorbTarget;
        collectedCallback = onCollected;
        absorbing = true;

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
            pickupCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollectOnTouch(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollectOnTouch(other);
    }

    private void TryCollectOnTouch(Collider2D other)
    {
        if (!IsAvailable || other == null)
            return;

        HeroCtrl hero = other.GetComponentInParent<HeroCtrl>();
        if (hero == null || hero.HeroSkillController == null)
            return;

        if (!hero.HeroSkillController.TryCollectElementShard(this))
            return;

        ReturnToPool();
    }

    public override void OnReturnedToPool()
    {
        target = null;
        collectedCallback = null;
        absorbing = false;
        ReturnActiveShardVfx();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
            pickupCollider.enabled = true;

        base.OnReturnedToPool();
    }

    protected override void ResetValue()
    {
        base.ResetValue();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
            pickupCollider.isTrigger = true;
    }

    public static int AbsorbNearby(
        Vector3 origin,
        float radius,
        Transform target,
        Func<ElementalShardPickup, bool> predicate,
        Action<ElementalShardPickup> onCollected)
    {
        if (target == null || radius <= 0f)
            return 0;

        ContactFilter2D filter = new()
        {
            useTriggers = true
        };
        int hitCount = Physics2D.OverlapCircle(origin, radius, filter, Hits);
        int absorbed = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = Hits[i];
            if (hit == null)
                continue;

            ElementalShardPickup shard = hit.GetComponentInParent<ElementalShardPickup>();
            if (shard == null || !shard.IsAvailable)
                continue;

            if (predicate != null && !predicate(shard))
                continue;

            shard.BeginAbsorb(target, onCollected);
            absorbed++;
        }

        return absorbed;
    }

    public static ElementalShardPickup Spawn(
        ElementType element,
        float power,
        Vector3 position,
        ElementalShardPickup prefab = null)
    {
        if (!PoolManager.HasInstance)
        {
            LogMissingPoolWarning();
            return null;
        }

        PoolObj spawned = prefab != null
            ? PoolManager.InstanceOrNull.Spawn(prefab, position, Quaternion.identity)
            : PoolManager.InstanceOrNull.Spawn(ElementalShardPoolKey, position, Quaternion.identity);

        ElementalShardPickup shard = spawned as ElementalShardPickup;
        if (shard == null)
        {
            if (spawned != null)
                spawned.ReturnToPool();

            LogMissingPoolWarning();
            return null;
        }

        shard.Configure(element, power);
        return shard;
    }

    private static void LogMissingPoolWarning()
    {
        if (missingPoolWarningLogged)
            return;

        missingPoolWarningLogged = true;
        Debug.LogWarning(
            $"{nameof(ElementalShardPickup)} needs a PoolManager config with key '{ElementalShardPoolKey}' or an assigned prefab. Runtime auto-pool creation is disabled.");
    }

    private void RefreshVisual()
    {
        ElementalIconSet iconSet = GetIconSet();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = iconSet != null ? iconSet.GetElementSprite(element) : null;
            spriteRenderer.color = Color.white;
            spriteRenderer.gameObject.SetActive(true);

            FitSpriteToVisualSize(spriteRenderer);
        }

        VFXDefinition shardVfx = useShardVfx && iconSet != null
            ? iconSet.GetElementShardVfx(element)
            : null;

        if (shardVfx != activeShardVfxDefinition)
            ReturnActiveShardVfx();

        if (shardVfx != null)
        {
            if (spriteRenderer != null)
                spriteRenderer.gameObject.SetActive(!hideSpriteWhenVfxAssigned);

            PlayShardVfx(shardVfx);
        }
        else
        {
            StopAmbientParticles();
        }
    }

    private ElementalIconSet GetIconSet()
    {
        if (elementalIconSet != null)
            return elementalIconSet;

        if (cachedDefaultIconSet == null)
            cachedDefaultIconSet = Resources.Load<ElementalIconSet>(DefaultIconSetResourcePath);

        return cachedDefaultIconSet;
    }

    private void FitSpriteToVisualSize(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (largestSide <= 0f)
            return;

        float scale = visualSize / largestSide;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void StopAmbientParticles()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.gameObject.SetActive(false);
        }
    }

    private void PlayShardVfx(VFXDefinition shardVfx)
    {
        if (activeShardVfx != null || shardVfx == null || !VFXManager.HasInstance)
            return;

        activeShardVfxDefinition = shardVfx;
        activeShardVfx = VFXManager.InstanceOrNull.Play(
            shardVfx,
            transform.position,
            Vector2.zero,
            transform);

        if (activeShardVfx == null)
        {
            activeShardVfxDefinition = null;
            if (spriteRenderer != null)
                spriteRenderer.gameObject.SetActive(true);
            return;
        }

        VFXPoolObj vfxPoolObj = activeShardVfx as VFXPoolObj;
        if (vfxPoolObj == null)
            vfxPoolObj = activeShardVfx.GetComponent<VFXPoolObj>();

        vfxPoolObj?.SetAutoReturnToPool(false);
        activeShardVfx.transform.SetParent(transform, true);
    }

    private void ReturnActiveShardVfx()
    {
        if (activeShardVfx != null)
            activeShardVfx.ReturnToPool();

        activeShardVfx = null;
        activeShardVfxDefinition = null;
    }
}
