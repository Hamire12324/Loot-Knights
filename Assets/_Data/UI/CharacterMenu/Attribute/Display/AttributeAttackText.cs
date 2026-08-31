public class AttributeAttackText : AttributeText
{
    protected override string GetValue(CharacterAttributeData attributeData, string emptyValue)
    {
        return FormatNumber(attributeData.Attack);
    }
}
