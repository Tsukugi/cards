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
    public string duration = EALCardSkillDuration.OncePerTurn.ToString();
    public string effectId;
    public string effectLabel;
}

public class ALCardSkillConditionDTO
{
    public string conditionId;  // Condtition for this effect to activate - EnemyTurn, Retaliation, Counter , StartsAttack, IsAttacked, IsSpecificCardOnField
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
    Always,
    OncePerTurn,
    OncePerMatch,
    AttackPhase,
    MainPhase,
    CurrentBattle,
}