using System;
using UnityEngine;

/// <summary>
/// Runtime marker applied by a boss wave. It keeps boss identity separate from the
/// prefab, so a prefab can appear as a normal enemy in one stage and as a boss later.
/// </summary>
[DisallowMultipleComponent]
public class BossEnemy : MonoBehaviour
{
    private EnemyCtrl enemy;
    private CharacterDamReceiver damageReceiver;
    public bool IsBoss { get; private set; }

    public static event Action<BossEnemy> OnBossSpawned;
    public static event Action<BossEnemy> OnBossDefeated;

    private void Awake()
    {
        enemy = GetComponent<EnemyCtrl>();
        damageReceiver = enemy != null ? enemy.CharacterDamReceiver : null;
    }

    private void OnDisable()
    {
        UnsubscribeDeath();
    }

    public void Configure(bool isBoss)
    {
        enemy ??= GetComponent<EnemyCtrl>();
        damageReceiver ??= enemy != null ? enemy.CharacterDamReceiver : null;
        UnsubscribeDeath();

        IsBoss = isBoss;
        enemy?.SetFaction(isBoss ? Faction.Boss : Faction.Enemy);

        if (!isBoss)
            return;

        if (damageReceiver != null)
            damageReceiver.OnDeath += HandleDeath;

        OnBossSpawned?.Invoke(this);
    }

    private void HandleDeath(CharacterDamReceiver _)
    {
        if (!IsBoss)
            return;

        OnBossDefeated?.Invoke(this);
        UnsubscribeDeath();
    }

    private void UnsubscribeDeath()
    {
        if (damageReceiver != null)
            damageReceiver.OnDeath -= HandleDeath;
    }
}
