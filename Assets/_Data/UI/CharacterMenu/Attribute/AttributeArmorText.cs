public class AttributeArmorText : AttributeText
{
    protected override string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        return FormatNumber(statSnapshot.Armor, emptyValue);
    }
}
