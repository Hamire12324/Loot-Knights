using UnityEngine;
using UnityEngine.UI;

public class UIStatBar : SliderAbstract
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadFillImage();
    }

    private void LoadFillImage()
    {
        if (fillImage != null) return;

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image.name.ToLowerInvariant().Contains("fill"))
            {
                fillImage = image;
                return;
            }
        }
    }

    public override void SetValue(float value)
    {
        value = Mathf.Clamp01(value);

        if (slider != null)
        {
            base.SetValue(value);
            return;
        }

        if (fillImage == null) return;

        fillImage.rectTransform.localScale = Vector3.one;

        RectTransform fillRect = fillImage.rectTransform;

        fillRect.anchorMin = new Vector2(0f, fillRect.anchorMin.y);
        fillRect.anchorMax = new Vector2(value, fillRect.anchorMax.y);
        fillRect.offsetMin = new Vector2(0f, fillRect.offsetMin.y);
        fillRect.offsetMax = new Vector2(0f, fillRect.offsetMax.y);
    }
}
