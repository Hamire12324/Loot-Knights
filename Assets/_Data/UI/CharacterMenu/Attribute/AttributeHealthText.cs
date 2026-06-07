public class AttributeHealthText : AttributeText
{
    protected override string GetValue(AttributeStatSnapshot statSnapshot, string emptyValue)
    {
        if (statSnapshot.MaxHealth <= 0f)
            return emptyValue;

        return FormatNumber(statSnapshot.CurrentHealth, emptyValue) + " / " + FormatNumber(statSnapshot.MaxHealth, emptyValue);
    }
}
