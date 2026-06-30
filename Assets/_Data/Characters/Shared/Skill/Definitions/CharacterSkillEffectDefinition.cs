using UnityEngine;

public abstract class CharacterSkillEffectDefinition : ScriptableObject
{
    public abstract void Execute(CharacterSkillExecutionContext context);
}
