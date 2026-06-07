using UnityEngine;

public class DungeonAbstract : BaseMonoBehaviour
{
    [SerializeField] protected DungeonCtrl dungeonCtrl;
    public DungeonCtrl DungeonCtrl => dungeonCtrl;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadDungeonCtrl();
    }

    protected virtual void LoadDungeonCtrl()
    {
        if (dungeonCtrl != null) return;

        dungeonCtrl = GetComponentInParent<DungeonCtrl>();
        if (dungeonCtrl != null) return;

        Debug.LogError($"There is no DungeonCtrl in {gameObject.name}", gameObject);
    }
}
