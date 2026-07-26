using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterMenuTabButton : ButtonAbstract
{
    [SerializeField] private CharacterMenuPanel panel;
    [SerializeField] private CharacterMenuSection section;
    [SerializeField] private Graphic selectedGraphic;
    [SerializeField] private Image selectedImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    public void SetPanel(CharacterMenuPanel targetPanel)
    {
        panel = targetPanel;
    }

    public void SetSection(CharacterMenuSection targetSection)
    {
        section = targetSection;
    }

    public void SetLabel(string value)
    {
        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }

    public void SetSelected(bool selected)
    {
        LoadSelectedGraphic();

        if (selectedImage == null) return;

        Sprite targetSprite = selected ? selectedSprite : normalSprite;
        if (targetSprite != null)
            selectedImage.sprite = targetSprite;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (panel == null)
            panel = GetComponentInParent<CharacterMenuPanel>(true);

        LoadSelectedGraphic();
    }

    private void LoadSelectedGraphic()
    {
        if (selectedGraphic == null)
            selectedGraphic = button != null ? button.targetGraphic : GetComponent<Graphic>();

        if (selectedImage == null)
            selectedImage = selectedGraphic as Image;

        if (selectedImage == null)
            selectedImage = GetComponent<Image>();

        if (selectedImage != null && normalSprite == null)
            normalSprite = selectedImage.sprite;

        if (button == null || selectedSprite != null) return;

        SpriteState spriteState = button.spriteState;
        if (spriteState.selectedSprite != null)
            selectedSprite = spriteState.selectedSprite;
        else if (spriteState.pressedSprite != null)
            selectedSprite = spriteState.pressedSprite;
    }

    protected override void OnClick()
    {
        if (panel == null)
            panel = FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);

        panel?.ShowSection(section);
    }
}
