using UnityEngine;

public class GameplayPanel : BaseMonoBehaviour
{
    [SerializeField] private CharacterMenuPanel characterMenuPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCharacterMenuPanel();
    }

    public void OpenCharacterMenu()
    {
        LoadCharacterMenuPanel();
        characterMenuPanel?.Show();
    }

    public void OpenInventory()
    {
        LoadCharacterMenuPanel();
        characterMenuPanel?.ShowInventory();
    }

    private void LoadCharacterMenuPanel()
    {
        if (characterMenuPanel != null) return;

        characterMenuPanel = GetComponentInChildren<CharacterMenuPanel>(true);
        if (characterMenuPanel != null) return;

        characterMenuPanel = FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);
    }
}
