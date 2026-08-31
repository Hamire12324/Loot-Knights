using UnityEngine;
using UnityEngine.UI;

public class CharacterRoleButton : ButtonAbstract
{
    [SerializeField] private CharacterCreationPanel characterCreationPanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private int roleIndex;

    public RectTransform RectTransform => transform as RectTransform;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        characterCreationPanel ??= GetComponentInParent<CharacterCreationPanel>();
        portraitImage ??= FindPortraitImage();
    }

    protected override void OnClick()
    {
        characterCreationPanel?.SelectRole(roleIndex);
    }

    public void Setup(CharacterCreationPanel panel, int index, Sprite portrait)
    {
        characterCreationPanel = panel;
        roleIndex = index;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    private Image FindPortraitImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image.transform == transform) continue;

            string imageName = image.name.ToLowerInvariant();

            if (imageName.Contains("portrait") || imageName.Contains("avatar") || imageName.Contains("icon") || imageName.Contains("role"))
            {
                return image;
            }
        }

        foreach (Image image in images)
        {
            if (image.transform != transform)
            {
                return image;
            }
        }

        return null;
    }
}
