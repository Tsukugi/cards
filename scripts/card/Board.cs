using System.Collections.Generic;
using Godot;

public partial class Board : Node3D
{
    public delegate void PlaceCardEvent(Card card);
    public delegate void CardTriggerEvent(Card card);
    public delegate void BoardProvidedCallback(Board board);
    public delegate void BoardEdgeEvent(Vector2I axis);
    public delegate void BoardEvent();
    public delegate void CardPositionEvent(Vector2I position, Card.OnProvidedCardEvent callback);
    public event CardPositionEvent OnSelectCardPosition;
    public event BoardEvent OnClearSelection;

    protected Player player;
    protected bool isBoardActive = false; // If false, the board should not use any Input

    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    public Card SelectedCard = null;
    public Vector2I SelectCardPosition = Vector2I.Zero;
    [Export]
    public Vector2I BoardPositionInGrid = new();
    public bool IsBoardActive { get => isBoardActive; }

    public override void _Ready()
    {
        player = this.TryFindParentNodeOfType<Player>();
        player.OnPlayerBoardPositionSelect -= OnPlayerBoardPositionSelectHandler;
        player.OnPlayerBoardPositionSelect += OnPlayerBoardPositionSelectHandler;
        player.OnPlayerBoardSelect -= OnPlayerBoardSelectHandler;
        player.OnPlayerBoardSelect += OnPlayerBoardSelectHandler;
    }

    void OnPlayerBoardPositionSelectHandler(Vector2I position, BoardProvidedCallback boardProvidedCallback)
    {
        bool isActive = position == BoardPositionInGrid;
        SetIsBoardActive(isActive);
    }
    void OnPlayerBoardSelectHandler(Board board)
    {
        bool isActive = board == this;
        SetIsBoardActive(isActive);
    }

    public void DeselectAllCards()
    {
        if (OnClearSelection is null) return;
        OnClearSelection();
    }

    List<Card> GetCardsInTree() => this.TryGetAllChildOfType<Card>(true);
    static Card? FindCard(List<Card> cards, Vector2I position) => cards.Find(card => card.PositionInBoard == position);
    public Card? FindCardInTree(Vector2I position) => FindCard(GetCardsInTree(), position);
    public Card SearchForCardInBoard(Vector2I startingPosition, Vector2I axis, int searchMaxRange = 3, int sideOffsetRange = 3)
    {
        List<Card> cards = GetCardsInTree(); // I manually get the cards here bc i dont wanna use FindCardInTree every search

        if (FindCard(cards, startingPosition + axis) is Card directlyFoundCard) return directlyFoundCard;
        int currentRange = 1;
        while (currentRange <= searchMaxRange)
        {
            for (int sideOffset = -(currentRange * sideOffsetRange); sideOffset <= (currentRange * sideOffsetRange); sideOffset++)
            {
                var newOffset = FindOffsetBasedOnAxis(axis, currentRange, sideOffset);
                var newPosition = startingPosition + newOffset;
                Card? card = FindCard(cards, newPosition);
                GD.Print($"[SearchForCardInBoard] {startingPosition} + {newOffset} = {newPosition}");
                if (card is Card foundCard) return foundCard;
            }
            currentRange++;
        }
        return null;
    }

    static Vector2I FindOffsetBasedOnAxis(Vector2I axis, int range, int sideOffset)
    {
        Vector2I newAddedPosition = Vector2I.Zero;
        if (axis == Vector2I.Right) newAddedPosition = new Vector2I(range, sideOffset);
        if (axis == Vector2I.Left) newAddedPosition = new Vector2I(-range, sideOffset);
        if (axis == Vector2I.Down) newAddedPosition = new Vector2I(sideOffset, range);
        if (axis == Vector2I.Up) newAddedPosition = new Vector2I(sideOffset, -range);
        GD.Print($"[FindPosition] {range} - {sideOffset} -> {newAddedPosition}");
        return newAddedPosition;
    }

    public void SelectCardField(Vector2I position)
    {
        if (OnSelectCardPosition is null) return;
        SelectCardPosition = position;
        OnSelectCardPosition(position, (card) =>
            {
                SelectedCard = card;
            });
    }

    public void SetIsBoardActive(bool value)
    {
        isBoardActive = value;
        if (isBoardActive)
        {
            SelectCardField(SelectCardPosition);
            GD.Print($"[SetIsBoardActive] Active Board: {Name}");
        }
    }

    public Card GetSelectedCard() => SelectedCard;
}