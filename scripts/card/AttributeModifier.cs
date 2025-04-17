public class AttributeModifier
{
    public string AttributeName;
    public string Duration;
    public int Amount;

    public AttributeModifier() { }
    public AttributeModifier(string attributeName, int amount, string duration)
    {
        AttributeName = attributeName;
        Amount = amount;
        Duration = duration;
    }
}