using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillResourceEffect", menuName = "Loot Knights/Character/Skill Effects/Resource")]
public sealed class CharacterSkillResourceEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private string resourceId;
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField, Min(1)] private int maximumValue = 5;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterSkillResource.Add(context.Caster, resourceId, amount, maximumValue);
    }
}
