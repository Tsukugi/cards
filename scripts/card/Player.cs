
using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Node3D
{
    public delegate void EnemyInteractionRequestEvent(Player playerStartingInteraction, Player targetPlayerToInteract);
    public delegate void InteractionEvent(Player playerStartingInteraction);
    public delegate void ProvideCardInteractionEvent(Player playerStartingInteraction, Card card);

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
    EPlayState playState = EPlayState.Wait;
    public override void _Ready()
    {
        boardInputAsync = new(this);
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        orderedBoards = [hand, board];
        SelectBoard(GetSelectedBoard());
        Callable.From(InitializeEvents).CallDeferred();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        HandleInput();
        HandleAction();
    }

    protected virtual void InitializeEvents()
    {
        hand.OnPlayCardStart -= OnPlayCardStartHandler;
        hand.OnPlayCardStart += OnPlayCardStartHandler;
        board.OnPlaceCardStart -= OnPlaceCardStartHandler;
        board.OnPlaceCardStart += OnPlaceCardStartHandler;
        board.OnPlaceCardEnd -= OnPlaceCardEndHandler;
        board.OnPlaceCardEnd += OnPlaceCardEndHandler;
        board.OnPlaceCardCancel -= OnPlaceCardCancelHandler;
        board.OnPlaceCardCancel += OnPlaceCardCancelHandler;
        GD.Print("[InitializeEvents] Default Player events initialized");
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
        boardInputAsync.Debounce(() => TriggerAction(action), 0.2f);
    }

    protected void OnPlaceCardCancelHandler(Card cardPlaced)
    {
        cardPlaced.IsEmptyField = false;
        SetPlayState(EPlayState.Select);
        SelectBoard(hand);
    }

    protected void OnPlaceCardStartHandler(Card cardPlaced)
    {
        board.PlaceCardInBoardFromHand(this, cardPlaced);
    }

    protected void OnPlaceCardEndHandler(Card cardPlaced)
    {
        hand.RemoveCardFromHand(cardPlaced);
        SetPlayState(EPlayState.Select);
        SelectBoard(hand);
    }

    protected void OnPlayCardStartHandler(Card cardToPlay)
    {
        GD.Print($"[OnPlayCardStartHandler] Card to play {cardToPlay} {cardToPlay.GetAttributes<CardDTO>().name}");
        board.CardToPlace = cardToPlay;
        cardToPlay.IsEmptyField = true;
        SetPlayState(EPlayState.PlaceCard);
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

    protected void SelectBoard(Board board)
    {
        if (selectedBoard is not null)
        {
            UnassignBoardEvents(selectedBoard);
            if (selectedBoard.GetSelectedCard<ALCard>(this) is ALCard card) selectedBoard.ClearSelectionForPlayer(this); // Clear selection for old board
        }
        selectedBoard = board;
        selectedBoardIndex = orderedBoards.FindIndex((board) => board == selectedBoard);
        axisInputHandler.SetInverted(selectedBoard.GetIsEnemyBoard()); // An enemy board should have its axis inverted as it is inverted in the editor
        if (selectedBoard is not null) AssignBoardEvents(selectedBoard);
    }

    public void SetPlayState(EPlayState state)
    {
        EPlayState oldState = playState;
        _ = boardInputAsync.AwaitBefore(() => // This delay allows to avoid trigering different EPlayState events on the same frame
            {
                playState = state;
                GD.Print("[SetPlayState] " + oldState + " -> " + playState);
            }, 0.1f);
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
    public void AssignEnemyBoards(PlayerHand _hand, PlayerBoard _board)
    {
        enemyBoard = _board;
        enemyHand = _hand;

        orderedBoards = [hand, board, enemyBoard, enemyHand]; // assign order
        GD.Print($"[AssignEnemyBoards] Boards assigned for {Name}");
    }
    protected virtual void OnCardTriggerHandler(Card card)
    {
        GD.Print($"[OnCardTriggerHandler] {card.Name}");
    }
    public void SelectAndTriggerCard(Card card)
    {
        var foundBoard = orderedBoards.Find(orderedBoard => orderedBoard == card.GetBoard());
        if (foundBoard is null) GD.PrintErr($"[SelectAndTriggerCard] Board {card.GetBoard()} cannot be found ");
        SelectBoard(foundBoard);
        foundBoard.SelectCardField(this, card.PositionInBoard);
        TriggerAction(InputAction.Ok);
    }
    public void TriggerAction(InputAction action)
    {
        GD.Print($"[Action Triggered by player {Name}] {GetSelectedBoard().Name}.{action}");
        GetSelectedBoard().OnActionHandler(this, action);
    }

    public EPlayState GetPlayState() => playState;
    public bool GetIsControllerPlayer() => isControlledPlayer;
    public Color GetPlayerColor() => playerColor;
    public bool AllowsInputFromPlayer(Board board) => GetSelectedBoard() == board;
    public Board GetSelectedBoard() => orderedBoards[selectedBoardIndex];
    public virtual T GetPlayerHand<T>() where T : PlayerHand => hand as T;
    public virtual T GetPlayerBoard<T>() where T : PlayerBoard => board as T;
    public virtual T GetEnemyPlayerHand<T>() where T : PlayerHand => enemyHand as T;
    public virtual T GetEnemyPlayerBoard<T>() where T : PlayerBoard => enemyBoard as T;
}
