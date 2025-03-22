using Godot;

public partial class ALCard : Card
{
    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    public bool isInActiveState = true;
    public new ALCardDTO cardDTO = new();
}