using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Board : Node3D
{
    public delegate Task InteractionEvent(Player player, Board board, InputAction action);
    public delegate void PlaceCardEvent(Card card);
    public delegate Task CardEvent(Card card);
    public delegate void BoardEdgeEvent(Board board, Vector2I axis);
    public delegate void BoardCardEvent(Board board, Card card);
    public virtual event BoardEdgeEvent OnBoardEdge;
    public virtual event BoardCardEvent OnSelectFixedCardEdge;
    public event CardEvent OnRetireCardEvent;

    public event CardEvent OnCardEffectTargetSelected;
    public event InteractionEvent OnInputAction;

    // --- Refs ---

    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    protected readonly ActionInputHandler actionInputHandler = new();

    // --- State ---
    bool isEnemyBoard = false;
    readonly Dictionary<string, Card> selectedCard = []; // <PlayerName selecting the card, Card>

    [Export]
    protected Vector2I selectedCardPosition = Vector2I.Zero;
    [Export]
    public Vector2I BoardPositionInGrid = new();

    public virtual async Task TriggerCardEffectOnTargetSelected(Card card)
    {
        if (OnCardEffectTargetSelected is null)
        {
            GD.PrintErr($"[{GetType()}.TriggerCardEffectOnTargetSelected] No events attached"); return;
        }
        if (card is null)
        {
            GD.PrintErr($"[{GetType()}.TriggerCardEffectOnTargetSelected] No card selected, select a card to trigger"); return;
        }

        GD.Print($"[{GetType()}.TriggerCardEffectOnTargetSelected] Triggering card {card.Name}");
        await OnCardEffectTargetSelected(card);
    }

    static Card? FindCard(List<Card> cards, Vector2I position) => cards.Find(card => card.PositionInBoard == position);
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

    // ----- API -----

    public List<Card> GetCardsInTree() => this.TryGetAllChildOfType<Card>(true);
    public Card? FindCardInTree(Vector2I position) => FindCard(GetCardsInTree(), position);

    // Search for an available card in a specified range and also oposed axis to find diagonal matches
    public Card SearchForCardInBoard(Vector2I startingPosition, Vector2I axis, int searchMaxRange = 3, int sideOffsetRange = 3)
    {
        List<Card> cards = GetCardsInTree(); // I manually get the cards here bc i dont wanna use FindCardInTree every search

        if (FindCard(cards, startingPosition + axis) is Card directlyFoundCard && directlyFoundCard.IsInputSelectable) return directlyFoundCard;
        int currentRange = 1;
        while (currentRange <= searchMaxRange)
        {
            for (int sideOffset = -(currentRange * sideOffsetRange); sideOffset <= (currentRange * sideOffsetRange); sideOffset++)
            {
                var newOffset = FindOffsetBasedOnAxis(axis, currentRange, sideOffset);
                var newPosition = startingPosition + newOffset;
                Card? card = FindCard(cards, newPosition);
                // GD.Print($"[SearchForCardInBoard] {startingPosition} + {newOffset} = {newPosition}");
                if (card is Card foundCard && foundCard.IsInputSelectable) return foundCard;
            }
            currentRange++;
        }
        return null;
    }

    // --- Public API---
    public Vector2I GetSelectedCardPosition() => selectedCardPosition;
    public virtual void SelectCardField(Player player, Vector2I position, bool syncToNet = true)
    {
        string playerName = player.Name.ToString();
        selectedCardPosition = position;

        ClearSelectionForPlayer(player);

        List<Card> allCards = GetCardsInTree();
        if (allCards.Count == 0)
        {
            GD.PrintErr($"[SelectCardField] Cannot select card, no cards assigned");
            return;
        }

        Card foundCard = allCards.Find(card => card.PositionInBoard == position);
        if (foundCard is null)
        {
            GD.PrintErr($"[SelectCardField] {position} cannot be found");
            SelectCardField(player, Vector2I.Zero);
            return;
        }
        selectedCard[playerName] = foundCard;
        GD.Print($"[SelectCardField] {player.Name}.{Name} - {foundCard.Name}");
        foundCard.UpdatePlayerSelectedColor(player);
        foundCard.SetIsSelected(true);
        if (syncToNet) Network.Instance.SendSelectCardField(player.MultiplayerId, this, position);
    }

    public virtual void OnInputAxisChange(Player player, Vector2I axis) => GD.Print($"[OnInputAxisChange] {player.Name}.{axis}");
    public virtual void OnActionHandler(Player player, InputAction action)
    {
        EPlayState playState = player.GetInputPlayState();
        string interactionState = player.GetInteractionState();
        GD.Print($"[OnActionHandler] {player.Name} - Action: {action} - PlayState {playState} - InteractionState {interactionState}");
        if (OnInputAction is not null) OnInputAction(player, this, action);
    }
    public virtual void RetireCard(Card card)
    {
        GD.Print($"[RetireCard] Retire {card.Name}");
        if (OnRetireCardEvent is not null) OnRetireCardEvent(card);
    }
    public T? GetSelectedCard<T>(Player player) where T : Card
    {
        string playerName = player.Name.ToString();
        var res = selectedCard.TryGetValue(playerName, out Card value) ? value as T : null;
        if (value is not T && value is not null) GD.PushError($"[GetSelectedCard] Value exists but is not of type {typeof(T)} => {value.GetType()}");
        return res;
    }
    public void ClearSelectionForPlayer(Player player)
    {
        string playerName = player.Name.ToString();
        if (selectedCard.TryGetValue(playerName, out Card value))
        {
            // GD.Print($"[ClearSelectionForPlayer] {playerName} {value.Name} ");
            value.SetIsSelected(false);
            selectedCard.Remove(playerName);
        }
    }
    public void SetIsEnemyBoard(bool value) => isEnemyBoard = value;
    public bool GetIsEnemyBoard() => isEnemyBoard;
}