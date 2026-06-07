using UnityEngine;

public class EnemyDamSender : CharacterDamSender
{
    protected override void ResetValue()
    {
        base.ResetValue();
        this.targetLayer = LayerMask.GetMask("Player");
    }
}
