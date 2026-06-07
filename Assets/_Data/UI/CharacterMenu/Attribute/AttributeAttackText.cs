public class AttributeAttackText : AttributeText
{
    protected override string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        return FormatNumber(statSnapshot.Attack, emptyValue);
    }
}
