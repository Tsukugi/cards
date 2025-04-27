public class ALCardDTO : CardDTO
{
    public string faction = ALFaction.None;
    public string rarity = CardRarity.N;
    public string type = ALCardType.Ship;

    // Flagship, Ship
    public int power = 0;
    public string factionCountry = ALFactionCountry.None;

    // Flagship 
    public int durability = 0;

    // Ship
    public int supportValue = 0;
    public string supportScope = ALSupportScope.Both;
    // Ship, Event
    public int cost = 0;

    // Dynamic readable properties for Modifiers
    public int Power { get => power; }
}

public static class ALCardType
{
    public static readonly string Ship = "Ship";
    public static readonly string Flagship = "Flagship";
    public static readonly string Cube = "Cube";
    public static readonly string Event = "Event";
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
    public static readonly string None = "None";
    public static readonly string EagleUnion = "EagleUnion";
    public static readonly string RoyalNavy = "RoyalNavy";
    public static readonly string SakuraEmpire = "SakuraEmpire";
    public static readonly string IronBlood = "IronBlood";
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
    public static readonly string UntilDestroyed = "UntilDestroyed"; // While in a battle
}

public static class ALCardEffectCondition
{
    public static readonly string IsSpecificCardOnField = "IsSpecificCardOnField";
}

public static class ALCardEffectTrigger
{
    public static readonly string StartsAttack = "StartsAttack";
    public static readonly string IsAttacked = "IsAttacked";
    public static readonly string IsBattleSupported = "IsBattleSupported"; // Another ship supports this card
    public static readonly string Retaliation = "Retaliation"; // (When the flagship card is damaged and this card as the flagship durability is added to the hand, you can use this card without paying the cost)
    public static readonly string Counter = "Counter"; // (Can be activated during your Defense Step) 
    public static readonly string OnceDestroyed = "OnceDestroyed"; // When this card is send to the retreat zone
}

public static class ALCardEffectIds
{
    public static readonly string GetPower = "GetPower";
    public static readonly string AddEffect = "AddEffect";
    public static readonly string AddStatusEffect = "AddStatusEffect";

}
public static class ALCardStatusEffects {
    
    public static readonly string LimitBattleSupport = "LimitBattleSupport";
    public static readonly string RangedAttack = "RangedAttack";
}