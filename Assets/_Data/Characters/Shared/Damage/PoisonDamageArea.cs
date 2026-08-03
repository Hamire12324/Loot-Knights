using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PoisonDamageArea : BaseMonoBehaviour
{
    private const int MaxOverlapResults = 32;

    [Header("Area")]
    [SerializeField] private Collider2D areaCollider;
    [SerializeField, Min(0.05f)] private float fallbackRadius = 1.25f;

    [Header("Damage Over Time")]
    [SerializeField, Min(0.05f)] private float duration = 3f;
    [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
    [SerializeField, Min(0f)] private float damageMultiplier = 0.25f;
    [SerializeField, Min(0f)] private float elementalPower = 1.5f;
    [SerializeField, Min(0f)] private float elementalStatusDuration = 4f;

    private readonly Collider2D[] overlapResults = new Collider2D[MaxOverlapResults];
    private CharacterCtrl owner;
    private DamageData poisonDamageData;
    private LayerMask targetLayer;
    private Coroutine damageRoutine;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadAreaCollider();
    }

    protected override void OnDisable()
    {
        StopDamageRoutine();
        base.OnDisable();
    }

    public void Configure(CharacterCtrl owner, LayerMask targetLayer)
    {
        this.owner = owner;
        this.targetLayer = targetLayer;
        poisonDamageData = new DamageData(1f, false)
            .CloneWithElement(ElementType.Poison, elementalPower, elementalStatusDuration);

        LoadAreaCollider();
        EnsureFallbackCollider();

        VFXPoolObj vfxPoolObj = GetComponent<VFXPoolObj>();
        vfxPoolObj?.SetAutoReturnToPool(false);

        StopDamageRoutine();
        damageRoutine = StartCoroutine(DamageRoutine(vfxPoolObj));
    }

    private IEnumerator DamageRoutine(VFXPoolObj vfxPoolObj)
    {
        float elapsed = 0f;
        WaitForSeconds wait = new(tickInterval);

        while (elapsed < duration)
        {
            DealTickDamage();
            yield return wait;
            elapsed += tickInterval;
        }

        damageRoutine = null;

        if (vfxPoolObj != null)
            vfxPoolObj.ReturnToPool();
        else
            gameObject.SetActive(false);
    }

    private void DealTickDamage()
    {
        if (owner == null || areaCollider == null)
            return;

        ContactFilter2D filter = CharacterSkillTargetUtility.CreateTargetFilter(owner, targetLayer);
        int count = areaCollider.Overlap(filter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            overlapResults[i] = null;

            CharacterCtrl target = hit != null ? hit.GetComponentInParent<CharacterCtrl>() : null;
            if (!CharacterSkillTargetUtility.IsValidTarget(owner, hit, target))
                continue;

            float damage = CalculateTickDamage();
            target.CharacterDamReceiver.ReceiveDamage(damage, owner.transform, poisonDamageData);
        }
    }

    private float CalculateTickDamage()
    {
        if (owner?.CharacterStat == null)
            return 0f;

        return owner.CharacterStat.Attack.FinalValue * damageMultiplier;
    }

    private void StopDamageRoutine()
    {
        if (damageRoutine == null)
            return;

        StopCoroutine(damageRoutine);
        damageRoutine = null;
    }

    private void LoadAreaCollider()
    {
        if (areaCollider != null)
            return;

        areaCollider = GetComponent<Collider2D>();
    }

    private void EnsureFallbackCollider()
    {
        if (areaCollider == null)
        {
            CircleCollider2D fallbackCollider = gameObject.AddComponent<CircleCollider2D>();
            fallbackCollider.radius = GetLocalRadiusForWorldRadius(fallbackRadius);
            areaCollider = fallbackCollider;
        }

        areaCollider.isTrigger = true;

        if (areaCollider is CircleCollider2D circleCollider && circleCollider.radius <= 0.01f)
            circleCollider.radius = GetLocalRadiusForWorldRadius(fallbackRadius);
    }

    private float GetLocalRadiusForWorldRadius(float worldRadius)
    {
        Vector3 scale = transform.lossyScale;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 0.0001f);
        return Mathf.Max(0.05f, worldRadius) / largestScale;
    }

    private void OnValidate()
    {
        fallbackRadius = Mathf.Max(0.05f, fallbackRadius);
        duration = Mathf.Max(0.05f, duration);
        tickInterval = Mathf.Max(0.05f, tickInterval);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        elementalPower = Mathf.Max(0f, elementalPower);
        elementalStatusDuration = Mathf.Max(0f, elementalStatusDuration);
    }
}
