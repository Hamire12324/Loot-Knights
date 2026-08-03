using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillAfterimageEffect", menuName = "Loot Knights/Character/Skill Effects/Afterimage")]
public sealed class CharacterSkillAfterimageEffect : CharacterSkillEffectDefinition
{
    [SerializeField] private string requiredSkillTreeNodeId = "archer.arrowstep.hunter_afterimage";
    [SerializeField, Min(0f)] private float spawnDelay = 0.24f;
    [SerializeField, Min(0.1f)] private float duration = 2f;

    public override void Execute(CharacterSkillExecutionContext context)
    {
        if (context.Caster == null || context.Controller == null)
            return;

        if (SkillTreeRankResolver.GetRank(context.Caster, requiredSkillTreeNodeId) <= 0)
            return;

        Vector3 dashOrigin = context.Caster.transform.position;
        context.Controller.StartCoroutine(SpawnAfterDash(context.Caster, dashOrigin));
    }

    private IEnumerator SpawnAfterDash(CharacterCtrl caster, Vector3 dashOrigin)
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (caster != null)
            CharacterSkillAfterimageDecoy.Create(caster, dashOrigin, duration);
    }
}
