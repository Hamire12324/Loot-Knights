public class AttributeCritChanceText : AttributeText
{
    protected override string GetValue(CharacterAttributeData attributeData, string emptyValue)
    {
        return FormatPercent(attributeData.CritChance);
    }
}
