using TMPro;
using UnityEngine;

public class CharacterNameInput : BaseMonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private CharacterCreationPanel characterCreationPanel;

    protected override void Start()
    {
        base.Start();

        if (inputField == null || characterCreationPanel == null) return;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }

        if (characterCreationPanel == null)
        {
            characterCreationPanel = GetComponentInParent<CharacterCreationPanel>();
        }
    }
}
