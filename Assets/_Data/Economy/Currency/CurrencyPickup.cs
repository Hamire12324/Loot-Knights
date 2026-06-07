using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CurrencyPickup : CurrencyReward
{
    [SerializeField] private bool returnToPoolOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<HeroCtrl>() == null) return;

        Grant();

        if (returnToPoolOnPickup)
            ReturnToPool();
    }
}
