using System.Collections.Generic;
using Godot;

public partial class Board : CardField
{
    [Export]
    Vector2I boardSize = new(); // <width, height> board in a square grid
    [Export]
    public bool IsDebugging = false;
    protected bool isBoardActive = false; // If false, the board should not use any Input

    protected Card selectedCard = null;
    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    public PlayState playState = PlayState.Select;
    public Card SelectedCard { get => selectedCard; }

    public override void _Ready()
    {
        InitializeBoardFields(GetCards(), boardSize);
    }

    public List<CardField> GetCards()
    {
        List<CardField> cards = this.TryGetAllChildOfType<CardField>();
        return cards;
    }

    public void DeselectAllCards()
    {
        GetCards().ForEach(field =>
        {
            if (field is not Card card) return;
            card.SetIsSelected(false);
        });
    }
    public void SelectCard(Card field)
    {
        DeselectAllCards();
        if (field is null) return;
        field.SetIsSelected(true);
        selectedCard = field;
    }

    public void SelectCard()
    {
        DeselectAllCards();
    }

    public void DebugLog(string message)
    {
        if (IsDebugging) GD.Print(Name + " -> " + message);
    }

    void InitializeBoardFields(List<CardField> fieldsAsChildren, Vector2I boardSize)
    {
        if (boardSize.X == 0 || boardSize.Y == 0) return;

        Vector2I boardIndex = new();

        fieldsAsChildren.ForEach(field =>
        {
            field.positionInBoard = boardIndex;
            GD.Print(field.positionInBoard, field.Name);
            if (boardIndex.X + 1 < boardSize.X) boardIndex.X++;
            else if (boardIndex.Y + 1 < boardSize.Y)
            {
                boardIndex.X = 0;
                boardIndex.Y++;
            }
            else GD.PushWarning("[InitializeBoardFields] Attempted to initialize a field in " + boardIndex + " for " + field.Name + ". but the board is too small");
        });

    }

    public void SetIsGroupActive(bool value)
    {
        isBoardActive = value;
        if (isBoardActive) SelectCard(selectedCard); //TODO select proper place
        else DeselectAllCards();
    }
}