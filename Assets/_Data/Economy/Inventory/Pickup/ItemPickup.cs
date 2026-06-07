using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : PoolObj
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool returnToPoolOnPickup = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInventoryManager inventoryManager;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSpriteRenderer();
    }

    public void Configure(ItemDefinition itemDefinition, int pickupAmount)
    {
        item = itemDefinition;
        amount = Mathf.Max(1, pickupAmount);
        RefreshVisual();
    }

    public override void OnReturnedToPool()
    {
        base.OnReturnedToPool();

        item = null;
        amount = 1;

        if (spriteRenderer == null)
            LoadSpriteRenderer();

        if (spriteRenderer != null)
            spriteRenderer.sprite = null;
    }

    protected override void ResetValue()
    {
        base.ResetValue();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
            pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HeroCtrl hero = other.GetComponentInParent<HeroCtrl>();
        if (hero == null) return;

        PlayerInventoryManager manager = ResolveInventoryManager(hero);
        if (manager == null) return;

        InventoryOperationResult result = manager.AddItem(item, Mathf.Max(1, amount));
        if (result == null || !result.Success) return;

        if (returnToPoolOnPickup)
            ReturnToPool();
    }

    private void LoadSpriteRenderer()
    {
        if (spriteRenderer != null) return;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            LoadSpriteRenderer();

        if (spriteRenderer == null || item == null || item.Icon == null) return;

        spriteRenderer.sprite = item.Icon;
    }

    private PlayerInventoryManager ResolveInventoryManager(HeroCtrl hero)
    {
        if (inventoryManager != null)
            return inventoryManager;

        inventoryManager = hero.GetComponentInChildren<PlayerInventoryManager>(true);
        if (inventoryManager != null)
            return inventoryManager;

        if (PlayerInventoryManager.InstanceOrNull != null)
        {
            inventoryManager = PlayerInventoryManager.InstanceOrNull;
            return inventoryManager;
        }

        inventoryManager = FindAnyObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        return inventoryManager;
    }
}
