public class AttributeCritDamageText : AttributeText
{
    protected override string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        return FormatMultiplier(statSnapshot.CritDamage, emptyValue);
    }
}
