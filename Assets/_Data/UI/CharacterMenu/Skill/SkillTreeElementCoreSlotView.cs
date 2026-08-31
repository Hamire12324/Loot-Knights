using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns the Element Core skill icon and label.</summary>
public sealed class SkillTreeElementCoreSlotView : BaseMonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private SkillTreeTextView label;

    private void OnValidate() => LoadComponents();
    protected override void OnEnable() => LoadComponents();

    public void Render(HeroSkillDefinition skill)
    {
        LoadComponents();
        if (icon != null)
        {
            icon.sprite = skill != null ? skill.Icon : null;
            icon.enabled = skill != null && skill.Icon != null;
        }

        if (label != null)
        {
            label.Value = "ELEMENT";
            label.SetColor(skill != null
                ? new Color(.45f, 1f, .95f)
                : new Color(.55f, .62f, .75f, .85f));
        }
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        icon ??= transform.Find("IconMask/Icon")?.GetComponent<Image>();
        label ??= SkillTreeTextView.GetOrAdd(transform.Find("LabelText"));
    }
}
