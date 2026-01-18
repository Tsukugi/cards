using System.Collections.Generic;
using System.Text;
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
    readonly Dictionary<int, Card> selectedCard = []; // <PlayerId selecting the card, Card>
    readonly Dictionary<int, Vector2I> selectedCardPositionByPlayer = [];
    readonly Dictionary<Card, HashSet<int>> cardSelectors = [];
    readonly Dictionary<int, Player> playerByPeerId = [];
    protected Player ownerPlayer;

    [Export]
    protected Vector2I selectedCardPosition = Vector2I.Zero; // Owner player's selection for layout
    [Export]
    public Vector2I BoardPositionInGrid = new();

    public override void _Ready()
    {
        ownerPlayer = this.TryFindParentNodeOfType<Player>();
    }

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
    protected Vector2I GetOwnerSelectedCardPosition() => selectedCardPosition;
    public Vector2I GetSelectedCardPosition(Player player)
    {
        int playerId = player.MultiplayerId;
        if (!selectedCardPositionByPlayer.TryGetValue(playerId, out Vector2I position))
        {
            throw new System.InvalidOperationException($"[GetSelectedCardPosition] No selected position stored for player {playerId} on board {Name}.");
        }
        return position;
    }
    public virtual void SelectCardField(Player player, Vector2I position, bool syncToNet = true)
    {
        int playerId = player.MultiplayerId;
        playerByPeerId[playerId] = player;
        selectedCardPositionByPlayer[playerId] = position;
        if (player == ownerPlayer) selectedCardPosition = position;

        ClearSelectionForPlayer(player);

        List<Card> allCards = GetCardsInTree();
        if (allCards.Count == 0)
        {
            GD.Print($"[Board.SelectCardField] Board '{Name}' has no cards for player {player.Name}({playerId}) pos={position}");
            return;
        }

        Card foundCard = allCards.Find(card => card.PositionInBoard == position);
        if (foundCard is null)
        {
            throw new System.InvalidOperationException($"[SelectCardField] Position {position} cannot be found on board {Name}.");
        }
        selectedCard[playerId] = foundCard;
        GD.Print($"[SelectCardField] player={player.Name}({playerId}) board={Name} enemy={isEnemyBoard} pos={position} card={foundCard.Name} cardPos={foundCard.PositionInBoard} sync={syncToNet}");
        if (!cardSelectors.TryGetValue(foundCard, out HashSet<int> selectors))
        {
            selectors = [];
            cardSelectors[foundCard] = selectors;
        }
        selectors.Add(playerId);
        ApplyIndicatorForCard(foundCard);
        if (syncToNet)
        {
            if (ownerPlayer is null)
            {
                throw new System.InvalidOperationException($"[SelectCardField] Board owner is missing for {Name}.");
            }
            Network.Instance.SendSelectCardField(player.MultiplayerId, ownerPlayer.MultiplayerId, this, position);
        }
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
        int playerId = player.MultiplayerId;
        var res = selectedCard.TryGetValue(playerId, out Card value) ? value as T : null;
        if (value is not T && value is not null) GD.PushError($"[GetSelectedCard] Value exists but is not of type {typeof(T)} => {value.GetType()}");
        return res;
    }
    public void ClearSelectionForPlayer(Player player)
    {
        int playerId = player.MultiplayerId;
        if (selectedCard.TryGetValue(playerId, out Card value))
        {
            // GD.Print($"[ClearSelectionForPlayer] {playerName} {value.Name} ");
            selectedCard.Remove(playerId);
            if (cardSelectors.TryGetValue(value, out HashSet<int> selectors))
            {
                selectors.Remove(playerId);
                if (selectors.Count == 0) cardSelectors.Remove(value);
            }
            ApplyIndicatorForCard(value);
        }
    }
    public void SetIsEnemyBoard(bool value) => isEnemyBoard = value;
    public bool GetIsEnemyBoard() => isEnemyBoard;
    public Player GetOwnerPlayer() => ownerPlayer;

    public Vector2I MirrorPosition(Vector2I position)
    {
        List<Card> cards = GetCardsInTree();
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;
        foreach (Card card in cards)
        {
            if (!card.IsInputSelectable) continue;
            Vector2I cardPosition = card.PositionInBoard;
            if (cardPosition.X < minX) minX = cardPosition.X;
            if (cardPosition.X > maxX) maxX = cardPosition.X;
            if (cardPosition.Y < minY) minY = cardPosition.Y;
            if (cardPosition.Y > maxY) maxY = cardPosition.Y;
        }
        if (minX == int.MaxValue)
        {
            throw new System.InvalidOperationException($"[MirrorPosition] No selectable cards found on board {Name}.");
        }
        return new Vector2I(minX + maxX - position.X, minY + maxY - position.Y);
    }

    void ApplyIndicatorForCard(Card card)
    {
        if (!cardSelectors.TryGetValue(card, out HashSet<int> selectors) || selectors.Count == 0)
        {
            card.UpdateSelectionIndicators(false, default, false, default);
            card.SetIsSelected(false);
            return;
        }

        Player localSelector = null;
        Player enemySelector = null;
        foreach (int selectorId in selectors)
        {
            if (!playerByPeerId.TryGetValue(selectorId, out Player selectorPlayer))
            {
                throw new System.InvalidOperationException($"[ApplyIndicatorForCard] Selector {selectorId} not registered for board {Name}.");
            }
            if (selectorPlayer.GetIsControllerPlayer())
            {
                if (localSelector is not null && localSelector.MultiplayerId != selectorPlayer.MultiplayerId)
                {
                    throw new System.InvalidOperationException($"[ApplyIndicatorForCard] Multiple local selectors on board {Name}.");
                }
                localSelector = selectorPlayer;
                continue;
            }
            if (enemySelector is not null && enemySelector.MultiplayerId != selectorPlayer.MultiplayerId)
            {
                throw new System.InvalidOperationException($"[ApplyIndicatorForCard] Multiple enemy selectors on board {Name}.");
            }
            enemySelector = selectorPlayer;
        }
        var selectorInfo = new StringBuilder();
        foreach (int selectorId in selectors)
        {
            if (selectorInfo.Length > 0) selectorInfo.Append(", ");
            if (playerByPeerId.TryGetValue(selectorId, out Player selectorPlayer))
            {
                selectorInfo.Append($"{selectorPlayer.Name}({selectorId}) controller={selectorPlayer.GetIsControllerPlayer()}");
            }
            else
            {
                selectorInfo.Append($"Unknown({selectorId})");
            }
        }
        bool localSelected = localSelector is not null;
        bool enemySelected = enemySelector is not null;
        Color localColor = localSelected ? localSelector.GetPlayerColor() : default;
        Color enemyColor = enemySelected ? enemySelector.GetPlayerColor() : default;
        string localLabel = localSelected ? $"{localSelector.Name}({localSelector.MultiplayerId})" : "None";
        string enemyLabel = enemySelected ? $"{enemySelector.Name}({enemySelector.MultiplayerId})" : "None";
        GD.Print($"[SelectIndicator] board={Name} card={card.Name} selectors=[{selectorInfo}] local={localLabel} enemy={enemyLabel}");
        card.UpdateSelectionIndicators(localSelected, localColor, enemySelected, enemyColor);
        card.SetIsSelected(localSelected || enemySelected);
        GD.Print($"[SelectIndicatorState] board={Name} enemyBoard={isEnemyBoard} card={card.Name} pos={card.PositionInBoard} localSelected={localSelected} enemySelected={enemySelected} localColor={localColor} enemyColor={enemyColor}");
    }
}
