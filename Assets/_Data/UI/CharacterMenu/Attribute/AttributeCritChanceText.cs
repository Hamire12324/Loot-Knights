public class AttributeCritChanceText : AttributeText
{
    protected override string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        return FormatPercent(statSnapshot.CritChance, emptyValue);
    }
}
