public class ALCardDTO : CardDTO
{
    public string faction = ALFaction.None;
    public string rarity = CardRarity.N;
    public string cardType = ALCardType.Ship;
    public string shipType = ALShipType.None;

    // Flagship, Ship
    public int power = 0;
    public string[] factionCountry = [];

    // Flagship 
    public int durability = 5;

    // Ship
    public int supportValue = 0;
    public string supportScope = ALSupportScope.Both;
    // Ship, Event
    public int cost = 0;

    // Dynamic readable properties for Modifiers
    public int Power { get => power; }
    public int Cost { get => cost; }
}

public static class ALCardType
{
    public static readonly string Ship = "ShipCard";
    public static readonly string Flagship = "FlagshipCard";
    public static readonly string FlagshipAwakened = "FlagshipAwakenedCard";
    public static readonly string Cube = "CubeCard";
    public static readonly string Event = "EventCard";
}
public static class ALShipType
{
    public static readonly string Destroyer = "Destroyer";
    public static readonly string LightCruiser = "LightCruiser";
    public static readonly string HeavyCruiser = "HeavyCruiser";
    public static readonly string ArmoredHeavyCruiser = "ArmoredHeavyCruiser";
    public static readonly string BattleCruiser = "BattleCruiser";
    public static readonly string Battleship = "Battleship";
    public static readonly string AviationBattleship = "AviationBattleship";
    public static readonly string LightCarrier = "LightCarrier";
    public static readonly string AircraftCarrier = "AircraftCarrier";
    public static readonly string Submarine = "Submarine";
    public static readonly string RepairShip = "RepairShip";
    public static readonly string None = "-";
}

public static class ALSupportScope
{
    public static readonly string Hand = "Hand";
    public static readonly string Battlefield = "Battlefield";
    public static readonly string Both = "Both";
}

public static class ALFaction
{
    public static readonly string None = "None";
    public static readonly string CrimsonAxis = "CrimsonAxis";
    public static readonly string AzurLane = "AzurLane";
}

public static class ALFactionCountry
{
    public static readonly string EagleUnion = "EagleUnion";
    public static readonly string RoyalNavy = "RoyalNavy";
    public static readonly string SakuraEmpire = "SakuraEmpire";
    public static readonly string IronBlood = "IronBlood";
    public static readonly string VichiyaDominion = "VichiyaDominion";
    public static readonly string IrisLibre = "IrisLibre";
}

public static class ALCardRarity
{
    public static readonly string L = "L";
}

public static class ALCardEffectDuration
{
    public static readonly string MainPhase = "MainPhase"; // While in the main phase
    public static readonly string AttackPhase = "AttackPhase"; // While in the attack phase
    public static readonly string CurrentBattle = "CurrentBattle"; // While in a battle
}

public static class ALCardEffectCondition
{
    public static readonly string IsSpecificCardOnField = "IsSpecificCardOnField";
}

public static class ALCardEffectTrigger
{
    public static readonly string StartsAttack = "StartsAttack"; // When one card declares an attack
    public static readonly string IsAttacked = "IsAttacked"; // When an enemy declares an attack on your unit
    public static readonly string IsBattleSupported = "IsBattleSupported"; // Another ship supports this card
    public static readonly string Retaliation = "Retaliation"; // (When the flagship card is damaged and this card as the flagship durability is added to the hand, you can use this card without paying the cost)
    public static readonly string Counter = "Counter"; // (Can be activated during your Defense Step) 
    public static readonly string OnCardDestroyed = "OnCardDestroyed"; // When this card is send to the retreat zone
    public static readonly string EndOfTurn = "EndOfTurn"; // When a player is on end phase
    public static readonly string OnDamageReceived = "OnDamageReceived"; // When flagship durability takes damage
}

public static class ALCardStatusEffects
{
    public static readonly string LimitBattleSupport = "LimitBattleSupport";
    public static readonly string RangedAttack = "RangedAttack";
}
public static class ALCardReservedEffectValues
{
    public static readonly string StackableEffect = "StackableEffect";
}