
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : Node3D
{
    public delegate Task EnemyInteractionRequestEvent(Player playerStartingInteraction, Player targetPlayerToInteract);
    public delegate Task InteractionEvent(Player playerStartingInteraction);
    public delegate Task ProvideCardInteractionEvent(Player playerStartingInteraction, Card card);

    [Export]
    protected bool isControlledPlayer = false;
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

    // PlayState
    EPlayState playState = EPlayState.Wait; // Play state refers to the Player actions and what they can do via input --- Pressing Cancel, OK or Waiting for something
    string interactionState = ALInteractionState.None; // Interaction state refers to what specifically each player action should be attached to --- Pressing OK is to play a card in the main phase or to select a target for an effect

    public override void _Ready()
    {
        boardInputAsync = new(this);
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        orderedBoards = [hand, board];
        SelectBoard(GetSelectedBoard());
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
        boardInputAsync.Debounce(() => TriggerAction(action, this), 0.2f);
    }

    protected async Task OnPlaceCardCancelHandler(Card cardPlaced)
    {
        cardPlaced.SetIsEmptyField(false);
        await SetPlayState(EPlayState.SelectCardToPlay);
        SelectBoard(hand);
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
        SelectBoard(hand);
    }

    public async Task OnPlayCardStartHandler(Card cardToPlay)
    {
        GD.Print($"[OnPlayCardStartHandler] Card to play {cardToPlay} {cardToPlay.GetAttributes<CardDTO>().name}");
        board.CardToPlace = cardToPlay;
        await cardToPlay.TryToTriggerCardEffect(CardEffectTrigger.WhenPlayedFromHand);
        cardToPlay.SetIsEmptyField(true);
        await SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectBoardFieldToPlaceCard);
        SelectBoard(board);
    }

    protected void OnBoardEdgeHandler(Board exitingBoard, Vector2I axis)
    {
        // Only vertical for now
        if (axis.Y == 0) return;
        // We invert the input if belongs to player as Up is <0,-1> and the boards are (0, 1, 2, 3) Hand, Board, enemyBoard, enemyHand
        // So by having Up as 0,1 and Down as 0,-1 we can correctly switch between this order
        Vector2I invertedAxis = axis * -1;
        int invertedToken = !exitingBoard.GetIsEnemyBoard() ? 1 : -1;
        int newIndex = selectedBoardIndex + (invertedAxis.Y * invertedToken);
        if (!orderedBoards.Count.IsInsideBounds(newIndex)) { return; }


        Board newBoard = orderedBoards[newIndex];
        SelectBoard(newBoard);
        GD.Print($"[OnBoardEdgeHandler] {newBoard.Name} - {selectedBoardIndex} ");
    }

    protected void OnSelectFixedCardEdgeHandler(Board triggeringBoard, Card card)
    {
        Board newBoard = card.GetBoard();
        SelectBoard(newBoard);
        newBoard.SelectCardField(this, card.PositionInBoard); // Use the card's board to select itself, a referenced card can be from another board than the triggering one
    }

    public void SelectBoard(Board board)
    {
        if (selectedBoard is not null)
        {
            UnassignBoardEvents(selectedBoard);
            if (selectedBoard.GetSelectedCard<Card>(this) is ALCard card) selectedBoard.ClearSelectionForPlayer(this); // Clear selection for old board
        }
        selectedBoard = board;
        if (selectedBoard.GetSelectedCard<ALCard>(this) is null) selectedBoard.SelectCardField(this, Vector2I.Zero); // Select default field if none
        selectedBoardIndex = orderedBoards.FindIndex((board) => board == selectedBoard);
        axisInputHandler.SetInverted(selectedBoard.GetIsEnemyBoard()); // An enemy board should have its axis inverted as it is inverted in the editor
        if (selectedBoard is not null) AssignBoardEvents(selectedBoard);
    }

    public async Task SetPlayState(EPlayState state, string providedInteractionState = null)
    {
        EPlayState oldState = playState;
        string oldInteractionState = interactionState;
        await boardInputAsync.AwaitBefore(() => { }, 0.1f); // This delay allows to avoid trigering different EPlayState events on the same frame
        playState = state;
        string newInteraction = providedInteractionState is null ? ALInteractionState.None : providedInteractionState;
        interactionState = newInteraction;
        GD.Print($"[SetPlayState] {oldInteractionState} -> {newInteraction}");
        GD.Print($"[SetPlayState] {oldState} -> {playState}");
        await TryToExpireCardsModifierDuration(CardEffectDuration.CurrentInteraction);
    }

    protected static T DrawCard<T>(List<T> deck)
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
        enemyBoard = _board;
        enemyHand = _hand;

        orderedBoards = [hand, board, enemyBoard, enemyHand]; // assign order
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
        SelectBoard(foundBoard);
        foundBoard.SelectCardField(this, card.PositionInBoard);
        TriggerAction(InputAction.Ok, this);
    }
    public void TriggerAction(InputAction action, Player player)
    {
        GD.Print($"[Action Triggered by player {Name}] {GetSelectedBoard().Name}.{action}");
        player.GetSelectedBoard().OnActionHandler(this, action);
    }

    public EPlayState GetPlayState() => playState;
    public string GetInteractionState() => interactionState;
    public bool GetIsControllerPlayer() => isControlledPlayer;
    public Color GetPlayerColor() => playerColor;
    public Board GetSelectedBoard() => orderedBoards[selectedBoardIndex];
    public virtual T GetPlayerHand<T>() where T : PlayerHand => hand as T;
    public virtual T GetPlayerBoard<T>() where T : PlayerBoard => board as T;
    public virtual T GetEnemyPlayerHand<T>() where T : PlayerHand => enemyHand as T;
    public virtual T GetEnemyPlayerBoard<T>() where T : PlayerBoard => enemyBoard as T;
    public AsyncHandler GetAsyncHandler() => boardInputAsync;
}
