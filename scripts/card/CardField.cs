using Godot;

public partial class CardField : Node3D
{
    public delegate void OnProvidedCardFieldEvent(CardField card);
    public event OnProvidedCardFieldEvent OnFieldIsEmptyUpdate;
    [Export]
    // Gameplay: A board/hand can know which card is selected via this flag.
    protected bool isSelected = false;
    [Export]
    // Gameplay: A deck can be identified with this flag
    protected bool isDeck = false;

    [Export]
    public Vector2I PositionInBoard = new(); // Position used to track and identify it in board


    public float CardWidth = 4; // Card width
    [Export]
    public int CardStack = 1; // Amount of cards that should be stacked in this field


    [Export]
    // If true, the card's DTO cannot be modified. Gameplay: Another card cant replace this one
    public bool IsPlaceable = true;
    [Export]
    // If true, the card's can be selected by input, 
    // If false, input selection should ignore this field
    // Programatic Selection should still be able to select this field regardless of this value
    public bool IsInputSelectable = true;
    [Export]
    // If true, a player cannot place in this field. 
    // Gameplay: Decks, Graveyard or fields that are automatically filled and shouldn't be replaced by player
    public bool IsPlayerPlaceable = true;
    [Export]
    private bool isEmptyField = false;

    public bool GetIsEmptyField() => isEmptyField;
    public void SetIsEmptyField(bool value)
    {
        isEmptyField = value;
        if (OnFieldIsEmptyUpdate is not null) OnFieldIsEmptyUpdate(this);
    }

    public void SetIsSelected(bool value)
    {
        // if (value) GD.Print($"[SetIsSelected] {PositionInBoard}");
        isSelected = value;
    }

    public bool CanPlayerPlaceInThisField()
    {
        return IsPlaceable && IsPlayerPlaceable;
    }
}
