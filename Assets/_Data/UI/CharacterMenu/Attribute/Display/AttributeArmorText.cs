public class AttributeArmorText : AttributeText
{
    protected override string GetValue(CharacterAttributeData attributeData, string emptyValue)
    {
        return FormatNumber(attributeData.Armor);
    }
}
