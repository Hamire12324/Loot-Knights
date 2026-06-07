using UnityEngine;

public class ButtonConfirmCharacterCreation : ButtonAbstract
{
    [SerializeField] private CharacterCreationPanel characterCreationPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (characterCreationPanel != null) return;

        characterCreationPanel = GetComponentInParent<CharacterCreationPanel>();
    }

    protected override void OnClick()
    {
        if (characterCreationPanel == null)
        {
            Debug.LogError(transform.name + ": Missing CharacterCreationPanel reference.", gameObject);
            return;
        }

        characterCreationPanel.CreateCharacter();
    }
}
