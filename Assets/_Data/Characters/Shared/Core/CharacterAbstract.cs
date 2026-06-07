using UnityEngine;

public class CharacterAbstract : BaseMonoBehaviour
{
    [SerializeField] protected CharacterCtrl characterCtrl;
    public CharacterCtrl CharacterCtrl => characterCtrl;
    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCharacterCtrl();
    }
    protected virtual void LoadCharacterCtrl()
    {
        if (this.characterCtrl != null) return;
        this.characterCtrl = GetComponentInParent<CharacterCtrl>();
        if (this.characterCtrl != null) return;
        Debug.LogError($"There is no CharacterCtrl in {gameObject.name}", gameObject);
    }
}
