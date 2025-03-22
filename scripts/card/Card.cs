using Godot;

public partial class Card : CardField
{
    public delegate void OnProvidedCardEvent(Card card);
    protected Node3D cardDisplay, selectedIndicator;
    protected Board board;

    [Export]
    bool isFaceDown = false;

    public CardDTO cardDTO = null;


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
    }

    void OnSelectCardPositionHandler(Vector2I position, OnProvidedCardEvent cardCallback)
    {
        bool isSelectingThisCard = position == PositionInBoard;
        SetIsSelected(isSelectingThisCard);
        if (isSelectingThisCard)
        {
            GD.Print($"[OnSelectCardPositionHandler] Card {Name} is active");
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
        if (isFaceDown) cardDisplay.RotationDegrees = new Vector3(0, 0, 180);
        else cardDisplay.RotationDegrees = new Vector3(0, 0, 0);
    }
}