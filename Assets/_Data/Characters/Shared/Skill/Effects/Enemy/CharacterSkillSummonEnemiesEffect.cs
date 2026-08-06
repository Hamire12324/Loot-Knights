using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillSummonEnemiesEffect", menuName = "Loot Knights/Enemy/Skill Effects/Summon Enemies")]
public sealed class CharacterSkillSummonEnemiesEffect : CharacterSkillEffectDefinition
{
    [System.Serializable]
    private struct SummonEntry
    {
        public PoolObj Prefab;
        [Min(1)] public int Weight;
    }

    [Header("Summons")]
    [SerializeField] private SummonEntry[] summonEntries;
    [SerializeField, Min(1)] private int summonCount = 2;
    [SerializeField, Min(0f)] private float spawnRadius = 1.15f;
    [SerializeField, Min(0f)] private float delayBetweenSummons = 0.12f;

    [Header("Spawn Animation")]
    [SerializeField] private string summonTrigger = "Summoned";
    [SerializeField, Min(0f)] private float summonAnimationDuration = 0.8f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null || summonEntries == null || summonEntries.Length == 0)
            return;

        context.Controller.StartCoroutine(SummonRoutine(context));
    }

    private IEnumerator SummonRoutine(CharacterSkillExecutionContext context)
    {
        for (int i = 0; i < summonCount; i++)
        {
            SpawnOne(context.Caster, i);

            if (i < summonCount - 1 && delayBetweenSummons > 0f)
                yield return new WaitForSeconds(delayBetweenSummons);
        }
    }

    private void SpawnOne(CharacterCtrl caster, int index)
    {
        PoolManager poolManager = PoolManager.InstanceOrNull;
        PoolObj prefab = PickPrefab();
        if (poolManager == null || prefab == null)
            return;

        float angle = index * Mathf.PI * 2f / Mathf.Max(1, summonCount) + Random.Range(-0.35f, 0.35f);
        float radius = Random.Range(spawnRadius * 0.55f, spawnRadius);
        Vector3 position = caster.transform.position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        PoolObj summoned = poolManager.Spawn(prefab, position, Quaternion.identity);
        if (summoned == null)
            return;

        Animator animator = summoned.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrWhiteSpace(summonTrigger))
            animator.SetTrigger(summonTrigger);

        EnemyCtrl enemy = summoned.GetComponentInChildren<EnemyCtrl>();
        if (enemy == null)
            return;

        enemy.CharacterDamReceiver?.SetInvincible(true);
        if (enemy.EnemyAIController != null)
            enemy.EnemyAIController.enabled = false;

        CharacterSkillController controller = caster.CharacterSkillController;
        if (controller != null)
            controller.StartCoroutine(EnableSummonedEnemyAfterAnimation(enemy));
    }

    private IEnumerator EnableSummonedEnemyAfterAnimation(EnemyCtrl enemy)
    {
        if (summonAnimationDuration > 0f)
            yield return new WaitForSeconds(summonAnimationDuration);

        if (enemy == null || !enemy.gameObject.activeInHierarchy)
            yield break;

        enemy.CharacterDamReceiver?.SetInvincible(false);
        if (enemy.EnemyAIController != null)
            enemy.EnemyAIController.enabled = true;
    }

    private PoolObj PickPrefab()
    {
        int totalWeight = 0;
        foreach (SummonEntry entry in summonEntries)
        {
            if (entry.Prefab != null)
                totalWeight += Mathf.Max(1, entry.Weight);
        }

        if (totalWeight == 0)
            return null;

        int selected = Random.Range(0, totalWeight);
        foreach (SummonEntry entry in summonEntries)
        {
            if (entry.Prefab == null)
                continue;

            selected -= Mathf.Max(1, entry.Weight);
            if (selected < 0)
                return entry.Prefab;
        }

        return null;
    }
}
