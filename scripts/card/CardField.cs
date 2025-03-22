using Godot;

public partial class CardField : Node3D
{
    [Export]
    // Gameplay: A board/hand can know which card is selected via this flag.
    protected bool isSelected = false;

    [Export]
    public Vector2I PositionInBoard = new(); // Position used to track and identify it in board
    public float CardWidth = 4; // Card width
    [Export]
    public int CardStack = 1; // Amount of cards that should be stacked in this field


    [Export]
    // If true, the card's DTO cannot be modified. Gameplay: Another card cant replace this one
    public bool IsPlaceable = true;
    [Export]
    // If true, a player cannot place in this field. 
    // Gameplay: Decks, Graveyard or fields that are automatically filled and shouldn't be replaced by player
    public bool IsPlayerPlaceable = true;
    [Export]
    // If true, the card's display won't be visible. 
    // Gameplay: An empty board's field has this as true. / A card is being placed from the hand, and I want to show an empty hand space.
    public bool IsEmptyField = false;

    public void SetIsSelected(bool value)
    {
        if (value) GD.Print($"[SetIsSelected] {PositionInBoard}");
        isSelected = value;
    }

    public bool CanPlayerPlaceInThisField()
    {
        return IsPlaceable && IsPlayerPlaceable;
    }
}
