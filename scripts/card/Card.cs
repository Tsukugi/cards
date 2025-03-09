using Godot;

public partial class Card : Node3D
{
    public static float cardSize = 4;
    public CardDTO cardDTO = null;

    Node3D cardDisplay, selectedIndicator;

    [Export]
    // If true, the card's DTO cannot be modified. Gameplay: Another card cant replace this one
    public bool IsPlaceable = true;
    [Export]
    // If true, the card's display won't be visible. Gameplay: An empty board's field has this as true.
    public bool IsEmptyField = false;
    [Export]
    // Gameplay: A board/hand can know which card is selected via this flag.
    public bool IsSelected = false;


    public override void _Ready()
    {
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
        cardDisplay = GetNode<Node3D>("CardDisplay");
    }

    public override void _Process(double delta)
    {
        OnSelectHandler(IsSelected);
        OnFieldStateChangeHandler();
    }

    void OnSelectHandler(bool isSelected)
    {
        selectedIndicator.Visible = isSelected;
    }

    void OnFieldStateChangeHandler()
    {
        cardDisplay.Visible = !IsEmptyField;
    }
}
