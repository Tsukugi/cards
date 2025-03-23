public class ALCardDTO : CardDTO
{
    public string type = "Ship"; // Ship, Flagship, Cube, Event
    

    // Flagship, Ship
    public string power;
    public string[] skills;

    // Flagship 
    public int durability = 0;

    // Ship
    public int supportValue = 0;
    public string supportScope = "Hand"; // Hand, Battlefield
    // Ship, Event
    public int cost;
}