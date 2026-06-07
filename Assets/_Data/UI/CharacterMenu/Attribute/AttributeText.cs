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

    public void Refresh(CharacterStat characterStat, string emptyValue)
    {
        Refresh(AttributeStatSnapshot.FromCharacterStat(characterStat), emptyValue);
    }

    public void Refresh(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        string value = !statSnapshot.IsValid ? emptyValue : GetValue(statSnapshot, emptyValue);
        SetValue(value);
    }

    protected abstract string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue);

    protected string FormatNumber(float? value, string emptyValue)
    {
        return value.HasValue ? value.Value.ToString("0.#") : emptyValue;
    }

    protected string FormatPercent(float? value, string emptyValue)
    {
        return value.HasValue ? (value.Value * 100f).ToString("0.#") + "%" : emptyValue;
    }

    protected string FormatMultiplier(float? value, string emptyValue)
    {
        return value.HasValue ? "x" + value.Value.ToString("0.##") : emptyValue;
    }

    private void SetValue(string value)
    {
        if (text != null)
            text.text = value;
    }
}
