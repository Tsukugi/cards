
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : Node3D
{
    public delegate Task EnemyInteractionRequestEvent(Player playerStartingInteraction, Player targetPlayerToInteract);
    public delegate Task InteractionEvent(Player playerStartingInteraction);
    public delegate Task ProvideCardInteractionEvent(Player playerStartingInteraction, Card card);

    int multiplayerId;

    [Export]
    protected bool isControlledPlayer = false;
    [Export]
    protected string boardPath = "Board";
    [Export]
    protected string handPath = "Hand";
    [Export]
    protected string enemyHandPath = "";
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected Board selectedBoard;
    PlayerHand hand, enemyHand;
    PlayerBoard board, enemyBoard;
    AsyncHandler boardInputAsync;
    [Export]
    Color playerColor = new();

    // Board position state
    List<Board> orderedBoards;
    int selectedBoardIndex = 0;
    readonly Dictionary<(ulong fromId, ulong toId), Func<Vector2I, Vector2I>> exitPositionMappings = new();

    // PlayState
    float playStateChangeDelay = 0.2f;
    readonly PlayStateManager playStateManager = new();

    public int MultiplayerId { get => multiplayerId; set => multiplayerId = value; }

    public override void _Ready()
    {
        boardInputAsync = new(this);
        board = GetRequiredNode<PlayerBoard>(boardPath, "board");
        hand = GetRequiredNode<PlayerHand>(handPath, "hand");
        if (!string.IsNullOrWhiteSpace(enemyHandPath))
        {
            enemyHand = GetRequiredNode<PlayerHand>(enemyHandPath, "enemy hand");
            enemyHand.SetIsEnemyBoard(true);
        }
        RebuildOrderedBoards();
        SelectBoard(this, GetSelectedBoard());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        HandleInput();
        HandleAction();
    }

    protected virtual void UnassignBoardEvents(Board board)
    {
        board.OnBoardEdge -= OnBoardEdgeHandler;
        board.OnSelectFixedCardEdge -= OnSelectFixedCardEdgeHandler;
    }
    protected virtual void AssignBoardEvents(Board board)
    {
        UnassignBoardEvents(board);
        board.OnBoardEdge += OnBoardEdgeHandler;
        board.OnSelectFixedCardEdge += OnSelectFixedCardEdgeHandler;
    }
    protected void HandleInput()
    {
        Vector2I axis = axisInputHandler.GetAxis();
        Board selectedBoard = GetSelectedBoard();
        if (isControlledPlayer) selectedBoard.OnInputAxisChange(this, axis);
        hand.SetShowHand(selectedBoard == hand);
    }
    protected void HandleAction()
    {
        if (!isControlledPlayer) return;
        InputAction action = actionInputHandler.GetAction();
        if (action == InputAction.None) return;
        boardInputAsync.Debounce(() => TriggerAction(this, action), 0.2f);
    }

    protected async Task OnPlaceCardCancelHandler(Card cardPlaced)
    {
        if (cardPlaced is null)
        {
            throw new InvalidOperationException("[OnPlaceCardCancelHandler] Card to restore is required.");
        }
        board.CardToPlace = null;
        cardPlaced.SetIsEmptyField(false);
        await SetPlayState(EPlayState.SelectCardToPlay);
        hand.SelectCardField(this, cardPlaced.PositionInBoard, false);
        SelectBoard(this, hand);
    }

    protected async Task OnPlaceCardStartHandler(Card cardPlaced)
    {
        await board.PlaceCardInBoardFromHand(this, cardPlaced);
    }

    protected async Task OnPlaceCardEndHandler(Card cardPlaced)
    {
        GD.Print($"[OnPlaceCardEndHandler]");
        hand.RemoveCardFromHand(this, cardPlaced);
        await SetPlayState(EPlayState.SelectCardToPlay);
    }

    public async Task OnPlayCardStartHandler(Card cardToPlay)
    {
        GD.Print($"[OnPlayCardStartHandler] Card to play {cardToPlay} {cardToPlay.GetAttributes<CardDTO>().name}");
        board.CardToPlace = cardToPlay;
        await cardToPlay.TryToTriggerCardEffect(CardEffectTrigger.WhenPlayedFromHand);
        cardToPlay.SetIsEmptyField(true);
        await SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectBoardFieldToPlaceCard);
        if (board.GetSelectedCard<Card>(this) is null)
        {
            throw new InvalidOperationException($"[OnPlayCardStartHandler] No selected card on board {board.Name}.");
        }
        SelectBoard(this, board);
    }

    protected void OnBoardEdgeHandler(Board exitingBoard, Vector2I axis)
    {
        // Only vertical for now
        if (axis.Y == 0) return;
        // Up moves forward in the ordered boards list, Down moves back.
        int step = -axis.Y;
        int exitingIndex = orderedBoards.FindIndex(board => board == exitingBoard);
        if (exitingIndex < 0)
        {
            throw new InvalidOperationException($"[OnBoardEdgeHandler] Board {exitingBoard.Name} not found in orderedBoards.");
        }
        int newIndex = exitingIndex + step;
        if (!orderedBoards.Count.IsInsideBounds(newIndex)) { return; }

        Board newBoard = orderedBoards[newIndex];
        GD.Print($"[OnBoardEdgeHandler] player={Name} exiting={exitingBoard.Name} axis={axis} step={step} newIndex={newIndex} newBoard={newBoard.Name} newEnemy={newBoard.GetIsEnemyBoard()}");
        SelectBoardPositionByExit(exitingBoard, newBoard);
        SelectBoard(this, newBoard);
        GD.Print($"[OnBoardEdgeHandler] {newBoard.Name} - {selectedBoardIndex} ");
    }

    protected void OnSelectFixedCardEdgeHandler(Board triggeringBoard, Card card)
    {
        Board newBoard = card.GetBoard();
        newBoard.SelectCardField(this, card.PositionInBoard); // Use the card's board to select itself, a referenced card can be from another board than the triggering one
        SelectBoard(this, newBoard);
    }

    void SelectBoardPositionByExit(Board exitingBoard, Board newBoard)
    {
        Vector2I exitingPosition = exitingBoard.GetSelectedCardPosition(this);
        if (TryGetExitMapping(exitingBoard, newBoard, out Func<Vector2I, Vector2I> positionMapper))
        {
            Vector2I mappedPosition = positionMapper(exitingPosition);
            if (newBoard.FindCardInTree(mappedPosition) is null)
            {
                throw new InvalidOperationException($"[OnBoardEdgeHandler] Exit mapping from {exitingBoard.Name} to {newBoard.Name} returned {mappedPosition}, but no card exists.");
            }
            newBoard.SelectCardField(this, mappedPosition);
            return;
        }
        Vector2I targetPosition = exitingPosition;
        if (newBoard is PlayerHand)
        {
            // Hands are 1D (Y=0), keep X but clamp Y to 0 for cross-board moves.
            int maxX = newBoard.GetCardsInTree().Count - 1;
            if (maxX < 0)
            {
                throw new InvalidOperationException($"[OnBoardEdgeHandler] Hand {newBoard.Name} has no cards to select.");
            }
            int clampedX = Math.Clamp(exitingPosition.X, 0, maxX);
            targetPosition = new Vector2I(clampedX, 0);
        }
        if (newBoard is not PlayerHand && newBoard.FindCardInTree(targetPosition) is null)
        {
            List<Card> candidates = newBoard.GetCardsInTree().FindAll(card => card.IsInputSelectable);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"[OnBoardEdgeHandler] No selectable cards on board {newBoard.Name}.");
            }
            Card bestCard = null;
            int bestDistance = int.MaxValue;
            foreach (Card card in candidates)
            {
                int distance = Math.Abs(card.PositionInBoard.X - exitingPosition.X)
                    + Math.Abs(card.PositionInBoard.Y - exitingPosition.Y);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestCard = card;
            }
            if (bestCard is null)
            {
                throw new InvalidOperationException($"[OnBoardEdgeHandler] No selectable cards for mapping on board {newBoard.Name}.");
            }
            targetPosition = bestCard.PositionInBoard;
        }
        if (newBoard.FindCardInTree(targetPosition) is null)
        {
            throw new InvalidOperationException($"[OnBoardEdgeHandler] No card at {targetPosition} on board {newBoard.Name}.");
        }
        newBoard.SelectCardField(this, targetPosition);
    }

    bool TryGetExitMapping(Board fromBoard, Board toBoard, out Func<Vector2I, Vector2I> mapper)
    {
        if (fromBoard is null || toBoard is null)
        {
            throw new InvalidOperationException("[TryGetExitMapping] Boards are required.");
        }
        var key = (fromBoard.GetInstanceId(), toBoard.GetInstanceId());
        return exitPositionMappings.TryGetValue(key, out mapper);
    }

    public void SelectBoard(Player player, Board board)
    {
        if (selectedBoard is not null && selectedBoard != board)
        {
            UnassignBoardEvents(selectedBoard);
            if (selectedBoard.GetSelectedCard<Card>(player) is Card card) selectedBoard.ClearSelectionForPlayer(player); // Clear selection for old board
        }
        selectedBoard = board;
        if (selectedBoard.GetSelectedCard<Card>(player) is null)
        {
            if (selectedBoard.GetCardsInTree().Count == 0)
            {
                selectedBoardIndex = orderedBoards.FindIndex((board) => board == selectedBoard);
                if (selectedBoard is not null) AssignBoardEvents(selectedBoard);
                return;
            }
            throw new InvalidOperationException($"[SelectBoard] No selected card on board {selectedBoard.Name} for player {player.Name}.");
        }
        selectedBoardIndex = orderedBoards.FindIndex((board) => board == selectedBoard);
        if (selectedBoard is not null) AssignBoardEvents(selectedBoard);
    }

    public async Task SetPlayState(EPlayState state, string providedInteractionState = null, bool syncToNet = true)
    {
        PlayState oldState = playStateManager.GetPlayState();

        await boardInputAsync.AwaitBefore(() => { }, playStateChangeDelay); // This delay allows to avoid trigering different EPlayState events on the same frame
        var newInteractionState = providedInteractionState is null ? ALInteractionState.None : providedInteractionState;
        if (oldState.state == state && oldState.interactionState == newInteractionState) return;
        playStateManager.SetPlayState(new()
        {
            state = state,
            interactionState = newInteractionState
        });
        PlayState newState = playStateManager.GetPlayState();
        if (syncToNet) Network.Instance.SendPlayState(multiplayerId, (int)newState.state, newState.interactionState);
        //GD.Print($"[SetPlayState] PlayState: {oldState.state} -> {newState.state} --- InteractionState: {oldState.interactionState} -> {newState.interactionState}");
        await TryToExpireCardsModifierDuration(CardEffectDuration.CurrentInteraction);
    }

    protected static T DrawCard<T>(List<T> deck) where T : CardDTO
    {
        if (deck.Count <= 0)
        {
            throw new Exception($"[DrawCard] No cards available in deck {deck}");
        }
        T cardToDraw = deck[0];
        deck.RemoveAt(0);
        return cardToDraw;
    }

    // Public API

    public async Task TryToExpireCardsModifierDuration(string duration)
    {
        var boardCards = GetPlayerBoard<PlayerBoard>().GetCardsInTree();
        var handCards = GetPlayerHand<PlayerHand>().GetCardsInTree();

        foreach (var card in boardCards)
        {
            card.TryToExpireEffectOrModifier(duration);
        }
        foreach (var card in handCards)
        {
            card.TryToExpireEffectOrModifier(duration);
        }
        await Task.CompletedTask;
    }

    public async Task TryToTriggerOnAllCards(string triggerEvent)
    {
        var boardCards = GetPlayerBoard<PlayerBoard>().GetCardsInTree();
        var handCards = GetPlayerHand<PlayerHand>().GetCardsInTree();

        foreach (var card in boardCards)
        {
            await card.TryToTriggerCardEffect(triggerEvent);
        }
        foreach (var card in handCards)
        {
            await card.TryToTriggerCardEffect(triggerEvent);
        }
    }
    public void AssignEnemyBoards(PlayerHand _hand, PlayerBoard _board)
    {
        if (_hand is null)
        {
            throw new InvalidOperationException("[AssignEnemyBoards] Enemy hand is required.");
        }
        if (_board is null)
        {
            throw new InvalidOperationException("[AssignEnemyBoards] Enemy board is required.");
        }
        enemyBoard = _board;
        enemyHand = _hand;

        RebuildOrderedBoards();
        GD.Print($"[AssignEnemyBoards] Boards assigned for {Name}");
    }
    public virtual void OnCardTriggerHandler(Card card)
    {
        GD.Print($"[OnCardTriggerHandler] {card.Name}");
    }
    public void SelectAndTriggerCard(Card card)
    {
        var foundBoard = orderedBoards.Find(orderedBoard => orderedBoard == card.GetBoard());
        if (foundBoard is null) GD.PrintErr($"[SelectAndTriggerCard] Board {card.GetBoard()} cannot be found ");
        foundBoard.SelectCardField(this, card.PositionInBoard);
        SelectBoard(this, foundBoard);
        TriggerAction(this, InputAction.Ok);
    }
    public void TriggerAction(Player player, InputAction action, bool syncToNet = true)
    {
        GD.Print($"[Action Triggered by player {Name}] {GetSelectedBoard().Name}.{action}");
        player.GetSelectedBoard().OnActionHandler(this, action);
        if (syncToNet) Network.Instance.SendInputAction(action);
    }

    public void SetBoardExitMapping(Board fromBoard, Board toBoard, Func<Vector2I, Vector2I> positionMapper)
    {
        if (fromBoard is null || toBoard is null)
        {
            throw new InvalidOperationException("[SetBoardExitMapping] Boards are required.");
        }
        if (positionMapper is null)
        {
            throw new InvalidOperationException("[SetBoardExitMapping] Position mapper is required.");
        }
        var key = (fromBoard.GetInstanceId(), toBoard.GetInstanceId());
        exitPositionMappings[key] = positionMapper;
    }

    public Vector2I SimulateAxisInput(Vector2I axis)
    {
        Vector2I appliedAxis = axisInputHandler.ApplyInversion(axis);
        GetSelectedBoard().OnInputAxisChange(this, appliedAxis);
        return appliedAxis;
    }

    public async Task GoBackInHistoryState() => await boardInputAsync.AwaitBefore(playStateManager.GoBackInHistory, playStateChangeDelay);
    public PlayState GetPlayState() => playStateManager.GetPlayState();
    public EPlayState GetInputPlayState() => playStateManager.GetPlayState().state;
    public string GetInteractionState() => playStateManager.GetPlayState().interactionState;
    public bool GetIsControllerPlayer() => isControlledPlayer;
    public Color GetPlayerColor() => playerColor;
    public void SetPlayerColor(Color color) => playerColor = color;
    public Board GetSelectedBoard() => orderedBoards[selectedBoardIndex];
    public virtual T GetPlayerHand<T>() where T : PlayerHand => hand as T;
    public virtual T GetPlayerBoard<T>() where T : PlayerBoard => board as T;
    public virtual T GetEnemyPlayerHand<T>() where T : PlayerHand => enemyHand as T;
    public virtual T GetEnemyPlayerBoard<T>() where T : PlayerBoard => enemyBoard as T;
    public AsyncHandler GetAsyncHandler() => boardInputAsync;

    void RebuildOrderedBoards()
    {
        if (hand is null)
        {
            throw new InvalidOperationException("[RebuildOrderedBoards] Player hand is required.");
        }
        if (board is null)
        {
            throw new InvalidOperationException("[RebuildOrderedBoards] Player board is required.");
        }
        List<Board> boards = [hand, board];
        if (enemyBoard is not null)
        {
            enemyBoard.SetIsEnemyBoard(true);
            boards.Add(enemyBoard);
        }
        if (enemyHand is not null)
        {
            enemyHand.SetIsEnemyBoard(true);
            boards.Add(enemyHand);
        }
        orderedBoards = boards;
    }

    T GetRequiredNode<T>(string path, string label) where T : Node
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"[Player._Ready] {label} path is required.");
        }
        T node = GetNodeOrNull<T>(path) ?? throw new InvalidOperationException($"[Player._Ready] {label} not found at '{path}'.");
        return node;
    }
}
