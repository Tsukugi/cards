public class CardDTO : BaseDTO
{
    public string name;
    public string imageSrc;
    public string backImageSrc;
    public CardEffectDTO[] effects = [];
}


public class CardEffectDTO
{
    public CardEffectConditionDTO[] condition = [];
    public string triggerEvent = CardEffectTrigger.ManuallyActivated;
    public string duration = CardEffectDuration.WhileVisible;
    public string effectId;
    public string effectLabel;
}

public class CardEffectConditionDTO
{
    public string conditionId;
    public string conditionCard;
    public string conditionAmount;
}
public static class CardRarity
{
    public static readonly string N = "N";
    public static readonly string R = "R";
    public static readonly string SR = "SR";
    public static readonly string SSR = "SSR";
}

public static class CardEffectDuration
{
    public static readonly string WhileVisible = "WhileVisible"; // It is always active as soon the card is in hand or board
}

public static class CardEffectCondition
{
}

public static class CardEffectTrigger
{
    public static readonly string ManuallyActivated = "ManuallyActivated";
    public static readonly string WhenPlayed = "WhenPlayed"; // Every time this card is played into the board
    public static readonly string OncePerTurn = "OncePerTurn"; // Once per turn 
    public static readonly string OncePerMatch = "OncePerMatch"; // Once for all match
    public static readonly string EnemyTurnStart = "EnemyTurnStart";
    public static readonly string OnVisible = "OnVisible"; // When the card is visible in board or hand
}
