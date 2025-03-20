using Godot;

public partial class Card : CardField
{

    Node3D cardDisplay, selectedIndicator;
    [Export]
    bool isFaceDown = false;
    // Gameplay: A board/hand can know which card is selected via this flag.
    bool isSelected = false;
    [Export]
    // If true, the card's DTO cannot be modified. Gameplay: Another card cant replace this one
    public bool IsPlaceable = true;
    [Export]
    // If true, the card's display won't be visible. 
    // Gameplay: An empty board's field has this as true. / A card is being placed from the hand, and I want to show an empty hand space.
    public bool IsEmptyField = false;
    [Export]

    public CardDTO cardDTO = null;


    public override void _Ready()
    {
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
        cardDisplay = GetNode<Node3D>("CardDisplay");
        SetIsFaceDown(isFaceDown);
    }
    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
        OnFieldStateChangeHandler();
    }

    void OnSelectHandler(bool value)
    {
        selectedIndicator.Visible = value;
    }

    void OnFieldStateChangeHandler()
    {
        cardDisplay.Visible = !IsEmptyField;
    }

    public void SetIsFaceDown(bool value)
    {
        isFaceDown = value;
        if (isFaceDown) cardDisplay.RotationDegrees = new Vector3(0, 0, 180);
        else cardDisplay.RotationDegrees = new Vector3(0, 0, 0);
    }

    public void SetIsSelected(bool value)
    {
        isSelected = value;
    }

}