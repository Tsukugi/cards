using System;

public class CardDTO : BaseDTO
{
    public string name;
    public string imageSrc;
    public string backImageSrc;
    public CardEffectDTO[] effects = [];

    public bool HasEffectWithTrigger(string trigger) => effects.Length > 0 && Array.Find(effects, (effect) => Array.Find(effect.triggerEvent, (triggerEvent) => triggerEvent == trigger) is not null) is not null;
}


public class CardEffectDTO
{
    public CardEffectConditionDTO[] condition = [];
    public string[] triggerEvent = [];
    public string duration = CardEffectDuration.WhileVisible;
    public string effectId;
    public string effectLabel; // Card description or flavor text
    public string[] effectValue = []; // Any value passed for the logic
}

public class CardEffectConditionDTO
{
    public string conditionId;
    public string[] conditionArgs = [];
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
    public static readonly string WhileFaceDown = "WhileFaceDown"; // While the card is face down
    public static readonly string UntilEndOfTurn = "UntilEndOfTurn";
}

public static class CardEffectCondition
{
    public static readonly string OncePerTurn = "OncePerTurn"; // Once per turn  
    public static readonly string OncePerMatch = "OncePerMatch"; // Once for all match
}

public static class CardEffectTrigger
{
    public static readonly string WhenPlayedIntoBoard = "WhenPlayedIntoBoard"; // Every time this card is played into the board
    public static readonly string WhenPlayedFromHand = "WhenPlayedFromHand"; // Every time this card is taken FROM the hand 
    public static readonly string EnemyTurnStart = "EnemyTurnStart";
    public static readonly string OnVisible = "OnVisible"; // When the card is visible in board or hand
    public static readonly string AnyCardPlayed = "AnyCardPlayed"; // When ANY card is placed in the board
}
