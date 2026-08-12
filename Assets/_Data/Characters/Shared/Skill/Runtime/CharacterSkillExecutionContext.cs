using UnityEngine;

public readonly struct CharacterSkillExecutionContext
{
    public readonly CharacterSkillController Controller;
    public readonly CharacterCtrl Caster;
    public readonly CharacterSkillRuntime Runtime;
    public readonly CharacterSkillDefinition Definition;
    public readonly Vector2 AimDirection;
    public readonly Transform Target;
    public readonly bool HasManualTargetPosition;
    public readonly Vector2 ManualTargetPosition;

    public CharacterSkillExecutionContext(
        CharacterSkillController controller,
        CharacterSkillRuntime runtime,
        Vector2 aimDirection,
        Transform target)
        : this(controller, runtime, aimDirection, target, default, false)
    {
    }

    public CharacterSkillExecutionContext(
        CharacterSkillController controller,
        CharacterSkillRuntime runtime,
        Vector2 aimDirection,
        Transform target,
        Vector2 manualTargetPosition,
        bool hasManualTargetPosition)
    {
        Controller = controller;
        Caster = controller != null ? controller.CharacterCtrl : null;
        Runtime = runtime;
        Definition = runtime != null ? runtime.Definition : null;
        AimDirection = aimDirection == Vector2.zero ? Vector2.down : aimDirection.normalized;
        Target = target;
        HasManualTargetPosition = hasManualTargetPosition;
        ManualTargetPosition = manualTargetPosition;
    }
}
