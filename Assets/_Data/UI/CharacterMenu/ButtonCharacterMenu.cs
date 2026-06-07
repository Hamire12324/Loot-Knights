using UnityEngine;

public class ButtonCharacterMenu : ButtonAbstract
{
    [SerializeField] private CharacterMenuPanel panel;
    [SerializeField] private CharacterMenuSection section = CharacterMenuSection.Attribute;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (panel == null)
            panel = FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);
    }

    protected override void OnClick()
    {
        if (panel == null)
            panel = FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);

        panel?.ShowSection(section);
    }
}
