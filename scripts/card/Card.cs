using Godot;

public partial class Card : Node3D
{
    public static float cardSize = 4;

    bool isSelected = false;
    public CardDTO cardDTO = null;

    Node3D selectedIndicator;

    [Export]
    public bool IsEmptyField = false;
    [Export]
    public bool IsSelected
    {
        get => isSelected; set
        {
            isSelected = value;
        }
    }


    Node3D cardDisplay;

    public override void _Ready()
    {
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
        cardDisplay = GetNode<Node3D>("CardDisplay");
    }

    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
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
