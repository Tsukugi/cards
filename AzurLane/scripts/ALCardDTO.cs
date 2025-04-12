public class ALCardDTO : CardDTO
{
    public string faction = "None"; // None, AzurLane, CrimsonAxis    public string type = "Ship"; // Ship, Flagship, Cube, Event
    public string rarity = "N"; // N, R, SR, SSR, L
    public string type = "Ship"; // Ship, Flagship, Cube, Event

    // Flagship, Ship
    public int power;
    public ALCardSkillDTO[] skills = [];
    public string factionCountry = "None"; // None, EagleUnion, SakuraEmpire, RoyalNavy, IronBlood

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
    public string duration = "OncePerTurn"; // When this effect is active - Always, OncePerTurn, OncePerMatch, AttackPhase, MainPhase
    public string effectId;
    public string effectLabel;
}

public class ALCardSkillConditionDTO
{
    public string conditionId;  // Condtition for this effect to activate - EnemyTurn, Retaliation, Counter , StartsAttack, IsAttacked, IsSpecificCardOnField
    public string conditionCard;
    public string conditionAmount;
}