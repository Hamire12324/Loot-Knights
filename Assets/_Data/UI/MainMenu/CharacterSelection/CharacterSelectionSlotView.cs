using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionSlotView : BaseMonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text selectLabel;
    [SerializeField] private TMP_Text deleteLabel;

    private Action onSelected;
    private Action onDeleted;
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Unbind();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadReferences();
    }
    private void LoadReferences()
    {
        selectButton ??= FindButton("SelectButton");
        deleteButton ??= FindButton("DeleteButton");
        selectLabel ??= selectButton != null ? selectButton.GetComponentInChildren<TMP_Text>(true) : null;
        deleteLabel ??= deleteButton != null ? deleteButton.GetComponentInChildren<TMP_Text>(true) : null;
    }
    public void Bind(CreatedCharacterData character, int level, Action selectAction, Action deleteAction)
    {
        Unbind();

        onSelected = selectAction;
        onDeleted = deleteAction;

        if (selectLabel != null)
            selectLabel.text = $"{character.CharacterName}  •  {character.CharacterClass}  •  Lv. {level}";

        if (deleteLabel != null)
            deleteLabel.text = "XÓA";

        if (selectButton != null)
            selectButton.onClick.AddListener(Select);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(Delete);
    }


    private void Select()
    {
        onSelected?.Invoke();
    }

    private void Delete()
    {
        onDeleted?.Invoke();
    }

    private void Unbind()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(Select);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(Delete);

        onSelected = null;
        onDeleted = null;
    }

    private Button FindButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == objectName)
                return button;
        }

        return null;
    }
}
