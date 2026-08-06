using UnityEngine;

public class EnemySkillController : CharacterSkillController
{
    public EnemyCtrl Enemy => characterCtrl as EnemyCtrl;

    // Compatibility for combat-pattern controllers. The block state remains
    // owned by the optional capability component, not this skill controller.
    protected bool IsBlocking => GetComponent<EnemyBlockBehaviour>()?.IsBlocking == true;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<EnemyCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no EnemyCtrl in {gameObject.name}", gameObject);
    }

}
