using UnityEngine;

public class EnemyTargetFinder : CharacterTargetFinder
{
    protected override void ResetValue()
    {
        base.ResetValue();

        this.targetLayer = LayerMask.GetMask("Player");
    }
}
