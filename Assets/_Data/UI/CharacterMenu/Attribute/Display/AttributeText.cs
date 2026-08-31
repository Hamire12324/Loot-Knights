using TMPro;

public abstract class AttributeText : TextAbstract
{
    protected override void LoadText()
    {
        if (text != null) return;

        foreach (TMP_Text foundText in GetComponentsInChildren<TMP_Text>(true))
        {
            if (foundText == null) continue;
            if (!foundText.name.ToLowerInvariant().Contains("value")) continue;

            text = foundText;
            return;
        }

        base.LoadText();
    }

    public void Refresh(CharacterAttributeData attributeData, string emptyValue)
    {
        SetValue(!attributeData.IsValid ? emptyValue : GetValue(attributeData, emptyValue));
    }

    protected abstract string GetValue(CharacterAttributeData attributeData, string emptyValue);

    protected string FormatNumber(float value) => value.ToString("0.#");
    protected string FormatPercent(float value) => (value * 100f).ToString("0.#") + "%";
    protected string FormatMultiplier(float value) => "x" + value.ToString("0.##");

    private void SetValue(string value)
    {
        if (text != null)
            text.text = value;
    }
}
