using Godot;

public partial class Card : CardField
{
    public delegate void OnProvidedCardEvent(Card card);
    protected Node3D cardDisplay, selectedIndicator;
    protected Board board;

    [Export]
    bool isFaceDown = false;
    [Export]
    bool isSideWays = false;

    public CardDTO cardDTO = new();

    public override void _Ready()
    {
        board = this.TryFindParentNodeOfType<Board>();
        board.OnClearSelection -= OnUnselectCardHandler;
        board.OnClearSelection += OnUnselectCardHandler;
        board.OnSelectCardPosition -= OnSelectCardPositionHandler;
        board.OnSelectCardPosition += OnSelectCardPositionHandler;
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
        cardDisplay = GetNode<Node3D>("CardDisplay");
        SetIsFaceDown(isFaceDown);
        SetIsSideWays(isSideWays);
    }

    void OnSelectCardPositionHandler(Vector2I position, OnProvidedCardEvent cardCallback)
    {
        bool isSelectingThisCard = position == PositionInBoard;
        SetIsSelected(isSelectingThisCard);
        if (isSelectingThisCard)
        {
            // GD.Print($"[OnSelectCardPositionHandler] Card {Name} is active");
            cardCallback(this);
        }
    }
    void OnUnselectCardHandler()
    {
        SetIsSelected(false);
    }

    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
        OnFieldStateChangeHandler();
    }


    void OnSelectHandler(bool isSelected)
    {
        selectedIndicator.Visible = isSelected && board.IsBoardActive;
    }

    void OnFieldStateChangeHandler()
    {
        cardDisplay.Visible = !IsEmptyField;
    }

    public void SetIsFaceDown(bool value)
    {
        isFaceDown = value;
        if (isFaceDown) cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithZ(180);
        else cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithZ(0);
    }

    public void SetIsSideWays(bool value)
    {
        isSideWays = value;
        if (isSideWays) RotationDegrees = RotationDegrees.WithY(90);
        else RotationDegrees = RotationDegrees.WithY(0);
    }
}