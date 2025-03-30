using System.Collections.Generic;
using Godot;

public partial class Board : Node3D
{
    public delegate void PlaceCardEvent(Card card);
    public delegate void CardTriggerEvent(Card card);
    public delegate void BoardProvidedCallback(Board board);
    public delegate void BoardEdgeEvent(Board board, Vector2I axis);
    public delegate void BoardCardEvent(Board board, Card card);
    public delegate void BoardEvent();
    public delegate void CardPositionEvent(Vector2I position, Card.OnProvidedCardEvent callback);
    public event CardPositionEvent OnSelectCardPosition;
    public event BoardEvent OnClearSelection;
    public virtual event BoardEdgeEvent OnBoardEdge;
    public virtual event BoardCardEvent OnSelectFixedCardEdge;

    protected Player player;

    // --- State ---
    bool canReceivePlayerInput = false;
    bool isEnemyBoard = false;
    Card selectedCard = null;

    // --- Public ---

    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected Vector2I selectedCardPosition = Vector2I.Zero;
    [Export]
    public Vector2I BoardPositionInGrid = new();

    public override void _Ready()
    {
        player = this.TryFindParentNodeOfType<Player>();
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
                // GD.Print($"[SearchForCardInBoard] {startingPosition} + {newOffset} = {newPosition}");
                if (card is Card foundCard) return foundCard;
            }
            currentRange++;
        }
        return null;
    }


    // Takes an axis and return an offset with Range added to the direction and offset to the opposite axis
    static Vector2I FindOffsetBasedOnAxis(Vector2I axis, int range, int sideOffset)
    {
        Vector2I newAddedPosition = Vector2I.Zero;
        if (axis == Vector2I.Right) newAddedPosition = new Vector2I(range, sideOffset);
        if (axis == Vector2I.Left) newAddedPosition = new Vector2I(-range, sideOffset);
        if (axis == Vector2I.Down) newAddedPosition = new Vector2I(sideOffset, range);
        if (axis == Vector2I.Up) newAddedPosition = new Vector2I(sideOffset, -range);
        // GD.Print($"[FindPosition] {range} - {sideOffset} -> {newAddedPosition}");
        return newAddedPosition;
    }

    // --- Public API---
    public Vector2I GetSelectedCardPosition() => selectedCardPosition;
    public void SelectCardField(Vector2I position)
    {
        if (OnSelectCardPosition is null) return;
        selectedCardPosition = position;
        OnSelectCardPosition(position, (card) =>
            {
                selectedCard = card;
            });
    }

    public void SetCanReceivePlayerInput(bool value)
    {
        canReceivePlayerInput = value;
        GD.Print($"[SetCanReceivePlayerInput] {player.Name}.{Name} {canReceivePlayerInput}");
        if (canReceivePlayerInput)
        {
            SelectCardField(selectedCardPosition);
        }
    }
    public bool GetCanReceivePlayerInput() => canReceivePlayerInput;

    public Player GetPlayer() => player;
    public T GetSelectedCard<T>() where T : Card => selectedCard as T;
    public bool DoesBelongToPlayer(Player player) => GetPlayer() == player;
    public void SetIsEnemyBoard(bool value)
    {
        isEnemyBoard = value;
        axisInputHandler.SetInverted(value); // An enemy board should have its axis inverted as it is inverted in the editor
    }
}