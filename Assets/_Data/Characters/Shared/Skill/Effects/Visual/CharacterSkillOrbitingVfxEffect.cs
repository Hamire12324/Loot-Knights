using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillOrbitingVfxEffect", menuName = "Loot Knights/Character/Skill Effects/Visual/Orbiting VFX")]
public sealed class CharacterSkillOrbitingVfxEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private VFXDefinition vfx;
    [SerializeField, Min(0.05f)] private float duration = 2f;
    [SerializeField, Min(0.02f)] private float interval = 0.12f;
    [SerializeField, Min(0f)] private float radius = 0.5f;
    [SerializeField, Min(1)] private int count = 2;
    [SerializeField] private float degreesPerSecond = 540f;
    [SerializeField] private float rotationOffset = -90f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null || vfx == null)
            return;

        context.Controller.StartCoroutine(PlayRoutine(context.Caster));
    }

    private IEnumerator PlayRoutine(CharacterCtrl caster)
    {
        float elapsed = 0f;
        WaitForSeconds wait = new(interval);

        while (caster != null && elapsed < duration)
        {
            float baseAngle = elapsed * degreesPerSecond;
            int vfxCount = Mathf.Max(1, count);

            for (int i = 0; i < vfxCount; i++)
            {
                float angle = baseAngle + 360f * i / vfxCount;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 radialDirection = new(Mathf.Cos(radians), Mathf.Sin(radians));
                Vector3 position = caster.transform.position + (Vector3)(radialDirection * radius);
                PoolObj spawned = CharacterSkillVfxUtility.Play(vfx, position, radialDirection);

                if (spawned != null)
                    spawned.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f + rotationOffset);
            }

            yield return wait;
            elapsed += interval;
        }
    }
}
