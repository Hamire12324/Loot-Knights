public class AttributeCritDamageText : AttributeText
{
    protected override string GetValue(CharacterAttributeData attributeData, string emptyValue)
    {
        return FormatMultiplier(attributeData.CritDamage);
    }
}
