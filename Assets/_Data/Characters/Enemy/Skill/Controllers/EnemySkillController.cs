using UnityEngine;

public class EnemySkillController : CharacterSkillController
{
    public EnemyCtrl Enemy => characterCtrl as EnemyCtrl;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<EnemyCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no EnemyCtrl in {gameObject.name}", gameObject);
    }
}
