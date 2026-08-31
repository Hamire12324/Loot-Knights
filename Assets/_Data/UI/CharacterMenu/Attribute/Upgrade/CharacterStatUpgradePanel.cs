using UnityEngine;

public class CharacterStatUpgradePanel : BaseMonoBehaviour
{
    [SerializeField] private CharacterStatUpgradeGrid statUpgradeGrid;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadStatUpgradeGrid();
    }

    private void LoadStatUpgradeGrid()
    {
        statUpgradeGrid ??= GetComponentInChildren<CharacterStatUpgradeGrid>(true);
    }

    public void Refresh()
    {
        statUpgradeGrid?.Refresh();
    }
}
