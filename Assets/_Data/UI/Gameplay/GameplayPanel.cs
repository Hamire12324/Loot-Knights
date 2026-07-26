using UnityEngine;

public class GameplayPanel : BaseMonoBehaviour
{
    [SerializeField] private CharacterMenuPanel characterMenuPanel;
    [SerializeField] private GameplayMobileSkillHud mobileSkillHud;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCharacterMenuPanel();
        LoadMobileSkillHud();
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

    private void LoadMobileSkillHud()
    {
        if (mobileSkillHud != null) return;

        mobileSkillHud = GetComponent<GameplayMobileSkillHud>();
        if (mobileSkillHud != null) return;

        mobileSkillHud = gameObject.AddComponent<GameplayMobileSkillHud>();
    }
}
