public class ALCardDTO : CardDTO
{
    public string faction = EALFaction.None.ToString();
    public string rarity = EALCardRarity.N.ToString();
    public string type = EALCardType.Ship.ToString();

    // Flagship, Ship
    public int power = 0;
    public ALCardSkillDTO[] skills = [];
    public string factionCountry = EALFactionCountry.None.ToString();

    // Flagship 
    public int durability = 0;

    // Ship
    public int supportValue = 0;
    public string supportScope = EALSupportScope.Both.ToString();
    // Ship, Event
    public int cost = 0;

    // Dynamic readable properties for Modifiers
    public int Power { get => power; }
}


public class ALCardSkillDTO
{
    public ALCardSkillConditionDTO[] condition = [];
    public string duration = EALCardSkillDuration.WhileVisible.ToString();
    public string effectId;
    public string effectLabel;
}

public class ALCardSkillConditionDTO
{
    public string conditionId = EALCardSkillCondition.ManuallyActivated.ToString();
    public string conditionCard;
    public string conditionAmount;
}

public enum EALCardType
{
    Ship,
    Flagship,
    Cube,
    Event
}
public enum EALSupportScope
{
    Hand,
    Battlefield,
    Both
}
public enum EALFaction
{
    None,
    CrimsonAxis,
    AzurLane,
}
public enum EALFactionCountry
{
    None,
    EagleUnion,
    RoyalNavy,
    SakuraEmpire,
    IronBlood
}
public enum EALCardRarity
{
    N,
    R,
    L,
    SR,
    SSR
}
public enum EALCardSkillDuration
{
    WhileVisible, // Tt is always active as soon the card is in hand or board
    MainPhase, // While in the main phase
    AttackPhase,  // While in the attack phase
    CurrentBattle, // While in a battle
}
public enum EALCardSkillCondition
{
    ManuallyActivated,
    WhenPlayed, // Every time this card is played into the board
    OncePerTurn, // Every turn 
    OncePerMatch, // Once for all match
    EnemyTurnStart,
    Retaliation,
    Counter,
    StartsAttack,
    IsAttacked,
    IsSpecificCardOnField
}