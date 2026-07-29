using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillVfxEffect", menuName = "Loot Knights/Character/Skill Effects/VFX")]
public sealed class CharacterSkillVfxEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private VFXDefinition vfx;
    [SerializeField] private bool attachToCaster;
    [SerializeField, Min(0f)] private float forwardOffset;
    [SerializeField] private Vector2 localOffset;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        CharacterCtrl caster = context.Caster;
        if (caster == null)
            return;

        Vector2 direction = context.AimDirection == Vector2.zero ? Vector2.down : context.AimDirection.normalized;
        Vector2 right = new(-direction.y, direction.x);
        Vector3 position = caster.transform.position +
            (Vector3)(direction * forwardOffset + right * localOffset.x + direction * localOffset.y);

        CharacterSkillVfxUtility.Play(vfx, position, direction, attachToCaster ? caster.transform : null);
    }
}
