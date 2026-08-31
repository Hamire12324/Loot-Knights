using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns tree-tabs and reports the selected definition.</summary>
public sealed class SkillTreeTreeSwitcherView : BaseMonoBehaviour
{
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button secondaryButton;
    private SkillTreeDefinition primaryTree;
    private SkillTreeDefinition secondaryTree;
    public event Action<SkillTreeDefinition> TreeSelected;

    protected override void Awake() { base.Awake(); BindButtons(); }
    private void OnValidate() => LoadComponents();
    protected override void OnEnable() { LoadComponents(); BindButtons(); }
    protected override void OnDisable() { if (primaryButton != null) primaryButton.onClick.RemoveListener(SelectPrimary); if (secondaryButton != null) secondaryButton.onClick.RemoveListener(SelectSecondary); }

    public void Configure(SkillTreeDefinition primary, SkillTreeDefinition secondary, SkillTreeDefinition active)
    {
        LoadComponents(); primaryTree = primary; secondaryTree = secondary;
        gameObject.SetActive(primary != null && secondary != null && primary != secondary);
        SetSelected(primaryButton, active == primary); SetSelected(secondaryButton, active == secondary);
    }
    protected override void LoadComponents() { base.LoadComponents(); primaryButton ??= transform.Find("ClassTreeButton")?.GetComponent<Button>(); secondaryButton ??= transform.Find("ElementTreeButton")?.GetComponent<Button>(); }
    private void BindButtons() { if (primaryButton != null) { primaryButton.onClick.RemoveListener(SelectPrimary); primaryButton.onClick.AddListener(SelectPrimary); } if (secondaryButton != null) { secondaryButton.onClick.RemoveListener(SelectSecondary); secondaryButton.onClick.AddListener(SelectSecondary); } }
    private void SelectPrimary() { if (primaryTree != null) TreeSelected?.Invoke(primaryTree); }
    private void SelectSecondary() { if (secondaryTree != null) TreeSelected?.Invoke(secondaryTree); }
    private static void SetSelected(Button button, bool selected) { Image image = button != null ? button.targetGraphic as Image : null; if (image != null) image.color = selected ? new Color(.05f, .72f, .95f, .96f) : new Color(.14f, .22f, .54f, .92f); }
}
