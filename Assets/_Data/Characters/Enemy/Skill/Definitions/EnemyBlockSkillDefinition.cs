using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBlockSkillDefinition", menuName = "Loot Knights/Enemy/Block Skill Definition")]
public sealed class EnemyBlockSkillDefinition : EnemySkillDefinition
{
    [SerializeField, Range(0f, 1f)] private float damageMultiplier = 0.4f;
    public float DamageMultiplier => damageMultiplier;
}
