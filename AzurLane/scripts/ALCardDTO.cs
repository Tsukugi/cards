public class ALCardDTO : CardDTO
{
    public string faction = "None"; // None, AzurLane, CrimsonAxis
    public string factionCountry = "None"; // None, EagleUnion, SakuraEmpire, RoyalNavy, IronBlood
    public string type = "Ship"; // Ship, Flagship, Cube, Event

    // Flagship, Ship
    public int power;
    public ALCardSkillDTO[] skills = [];

    // Flagship 
    public int durability = 0;

    // Ship
    public int supportValue = 0;
    public string supportScope = "Hand"; // Hand, Battlefield
    // Ship, Event
    public int cost = 0;
}

public class ALCardSkillDTO
{
    public ALCardSkillConditionDTO[] condition = [];
    public string duration = "Always"; // When this effect is active - Always, OncePerTurn, OncePerMatch, AttackPhase
    public string effectId;
    public string effectLabel;
}

public class ALCardSkillConditionDTO
{
    public string conditionId;  // Condtition for this effect to activate - EnemyTurn, IsSupportedWhileDefending, StartsAttack, IsAttacked, IsSpecificCardOnField
    public string conditionCard;
    public string conditionAmount;
}