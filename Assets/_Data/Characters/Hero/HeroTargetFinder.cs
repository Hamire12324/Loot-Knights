using UnityEngine;

public class HeroTargetFinder : CharacterTargetFinder
{
    protected override void ResetValue()
    {
        base.ResetValue();

        this.targetLayer = LayerMask.GetMask("Enemy");
    }
}
