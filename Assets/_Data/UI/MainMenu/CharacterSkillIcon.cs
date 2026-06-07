using UnityEngine;
using UnityEngine.UI;

public class CharacterSkillIcon : BaseMonoBehaviour
{
    [SerializeField] private Image iconImage;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }
}
