public class AttributeHealthText : AttributeText
{
    protected override string GetValue(CharacterAttributeData attributeData, string emptyValue)
    {
        if (attributeData.MaxHealth <= 0f)
            return emptyValue;

        return FormatNumber(attributeData.CurrentHealth) + " / " + FormatNumber(attributeData.MaxHealth);
    }
}
