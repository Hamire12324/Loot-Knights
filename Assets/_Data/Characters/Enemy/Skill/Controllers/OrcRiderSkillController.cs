using UnityEngine;

/// <summary>
/// Orc Rider uses its equipped skill slots as a readable combat loop:
/// guard nearby pressure, charge to close distance, then punish groups with a full spin.
/// Slot 0 = Ironhide Guard, slot 1 = Beastbreaker Charge, slot 2 = Maelstrom Mace.
/// </summary>
public sealed class OrcRiderSkillController : EnemySkillController
{
    [Header("Combat Pattern")]
    [SerializeField, Min(1)] private int maceStrikesBeforeMaelstrom = 3;

    [Header("Beastbreaker Charge")]
    [SerializeField, Min(0f)] private float chargeMinDistance = 1.25f;
    [SerializeField, Min(0f)] private float chargeMaxDistance = 4.25f;

    private int maceStrikesSinceSpecial;

    protected override void OnEnable()
    {
        base.OnEnable();
        maceStrikesSinceSpecial = 0;
    }

    protected override void Update()
    {
        base.Update();
        TryUseChargeToCloseDistance();
    }

    public override bool TryCastBasicAttack()
    {
        if (IsBlocking || IsCasting)
            return false;

        // The spin is deterministic, so players can learn that three mace swings means "back away".
        if (maceStrikesSinceSpecial >= maceStrikesBeforeMaelstrom && TryCastEquippedSkill(2))
        {
            maceStrikesSinceSpecial = 0;
            return true;
        }

        if (!base.TryCastBasicAttack())
            return false;

        maceStrikesSinceSpecial++;
        return true;
    }

    private bool TryCastEquippedSkill(int index)
    {
        CharacterSkillRuntime skill = GetSkill(index);
        return skill != null && skill.CanCast(this) && TryCast(index);
    }

    private void TryUseChargeToCloseDistance()
    {
        if (IsBlocking || IsCasting || characterCtrl == null)
            return;

        Transform target = characterCtrl.CharacterTargetFinder != null
            ? characterCtrl.CharacterTargetFinder.CurrentTarget
            : CharacterSkillTargeting.FindTarget(characterCtrl);
        if (target == null)
            return;

        float distance = Vector2.Distance(characterCtrl.transform.position, target.position);
        if (distance < chargeMinDistance || distance > chargeMaxDistance)
            return;

        TryCastEquippedSkill(1);
    }
}
