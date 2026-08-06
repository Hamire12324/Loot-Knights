using System.Collections;
using UnityEngine;

/// <summary>
/// Optional defensive capability for an enemy prefab. It casts the equipped
/// block skill when a target is close, then exposes the active block state to
/// the damage receiver.
/// </summary>
[RequireComponent(typeof(EnemySkillController))]
public sealed class EnemyBlockBehaviour : MonoBehaviour
{
    [SerializeField, Min(0f)] private float maxTargetDistance = 2.4f;
    [SerializeField, Min(0.01f)] private float decisionInterval = 0.15f;
    [SerializeField, Min(0f)] private float initialDecisionDelay = 2f;

    private EnemySkillController skillController;
    private float nextDecisionTime;
    private Coroutine blockRoutine;

    public bool IsBlocking { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        skillController = GetComponent<EnemySkillController>();
    }

    private void OnEnable()
    {
        IsBlocking = false;
        DamageMultiplier = 1f;
        nextDecisionTime = Time.time + initialDecisionDelay;
    }

    private void OnDisable()
    {
        if (blockRoutine != null)
            StopCoroutine(blockRoutine);

        blockRoutine = null;
        IsBlocking = false;
        DamageMultiplier = 1f;
    }

    private void Update()
    {
        if (IsBlocking || skillController == null || skillController.IsCasting || Time.time < nextDecisionTime)
            return;

        EnemyCtrl enemy = skillController.Enemy;
        if (enemy == null || enemy.CharacterDamReceiver == null || enemy.CharacterDamReceiver.IsDead ||
            enemy.CharacterDamReceiver.IsHitStunned)
            return;

        nextDecisionTime = Time.time + decisionInterval;
        int skillIndex = FindBlockSkillIndex(out EnemyBlockSkillDefinition definition);
        if (skillIndex < 0 || !skillController.GetSkill(skillIndex).CanCast(skillController))
            return;

        Transform target = enemy.CharacterTargetFinder != null ? enemy.CharacterTargetFinder.CurrentTarget : null;
        if (target == null || Vector2.Distance(enemy.transform.position, target.position) > maxTargetDistance)
            return;

        if (skillController.TryCast(skillIndex))
            blockRoutine = StartCoroutine(TrackBlock(definition));
    }

    private int FindBlockSkillIndex(out EnemyBlockSkillDefinition definition)
    {
        for (int i = 0; i < 4; i++)
        {
            CharacterSkillRuntime skill = skillController.GetSkill(i);
            if (skill?.Definition is EnemyBlockSkillDefinition blockDefinition)
            {
                definition = blockDefinition;
                return i;
            }
        }

        definition = null;
        return -1;
    }

    private IEnumerator TrackBlock(EnemyBlockSkillDefinition definition)
    {
        IsBlocking = true;
        DamageMultiplier = definition.DamageMultiplier;
        yield return new WaitForSeconds(definition.CastTime + definition.Duration);
        blockRoutine = null;
        IsBlocking = false;
        DamageMultiplier = 1f;
    }
}
