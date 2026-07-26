using UnityEngine;

public class HeroDamSender : CharacterDamSender
{
    protected override void ResetValue()
    {
        base.ResetValue();

        this.targetLayer = LayerMask.GetMask("Enemy");
    }
}
