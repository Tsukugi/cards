
using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Node3D
{
    public delegate void SelectPlayerBoardPositionEvent(Vector2I position, Board.BoardProvidedCallback boardEvent);
    public delegate void SelectPlayerBoardEvent(Board board);

    [Export]
    protected bool isControlledPlayer = false;
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected Board selectedBoard;
    protected PlayerHand hand;
    protected PlayerBoard board;
    protected PlayerHand enemyHand;
    protected PlayerBoard enemyBoard;
    AsyncHandler boardInputAsync;
    [Export]
    Color playerColor = new();

    // Board position state
    List<Board> orderedBoards;
    int selectedBoardIndex = 0;

    // PlayState
    EPlayState playState = EPlayState.Select;
    public bool IsPlayingTurn = false;

    public PlayerHand Hand { get => hand; }
    public PlayerBoard Board { get => board; }
    public PlayerHand EnemyHand { get => enemyHand; }
    public PlayerBoard EnemyBoard { get => enemyBoard; }

    public override void _Ready()
    {
        boardInputAsync = new(this);
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        orderedBoards = [hand, board];
        InitializeEvents();

        if (isControlledPlayer)
        {
            SelectBoard(hand);
            orderedBoards[0].SetCanReceivePlayerInput(true); // For the playing user we need an active board at start
        }
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

    protected void OnPlaceCardCancelHandler(Card cardPlaced)
    {
        cardPlaced.IsEmptyField = false;
        SetPlayState(EPlayState.Select);
        SelectBoardAndAllowInput(hand);
    }

    protected void OnPlaceCardStartHandler(Card cardPlaced)
    {
        board.PlaceCardInBoardFromHand(cardPlaced);
    }

    protected void OnPlaceCardEndHandler(Card cardPlaced)
    {
        hand.RemoveCardFromHand(cardPlaced);
        SetPlayState(EPlayState.Select);
        SelectBoardAndAllowInput(hand);
    }

    protected void OnPlayCardStartHandler(Card cardToPlay)
    {
        GD.Print($"[OnPlayCardStartHandler] Card to play {cardToPlay} {cardToPlay.GetAttributes<CardDTO>().name}");
        board.CardToPlace = cardToPlay;
        cardToPlay.IsEmptyField = true;
        SetPlayState(EPlayState.PlaceCard);
        SelectBoardAndAllowInput(board);
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

        exitingBoard.SetCanReceivePlayerInput(false);

        Board newBoard = orderedBoards[newIndex];
        SelectBoardAndAllowInput(newBoard);
        selectedBoardIndex = newIndex;

        GD.Print($"[OnBoardEdgeHandler] {newBoard.Name} - {selectedBoardIndex} ");
    }

    protected void OnSelectFixedCardEdgeHandler(Board triggeringBoard, Card card)
    {
        if (!triggeringBoard.GetCanReceivePlayerInput()) return;

        triggeringBoard.SetCanReceivePlayerInput(false);

        Board newBoard = card.GetBoard();
        selectedBoardIndex = orderedBoards.FindIndex((board) => board == newBoard);

        SelectBoardAndAllowInput(newBoard);
        newBoard.SelectCardField(this, card.PositionInBoard); // Use the card's board to select itself, a referenced card can be from another board than the triggering one

        GD.Print($"[OnSelectFixedCardEdgeHandler] {newBoard.GetType()} - {selectedBoardIndex} ");
    }

    protected void SelectBoard(Board board)
    {
        if (selectedBoard is not null) UnassignBoardEvents(selectedBoard);
        selectedBoard = board;
        if (selectedBoard is not null) AssignBoardEvents(selectedBoard);
    }

    protected void SelectBoardAndAllowInput(Board board)
    {
        selectedBoard?.SetCanReceivePlayerInput(false);
        SelectBoard(board);
        boardInputAsync.AwaitBefore(() => board?.SetCanReceivePlayerInput(true), 0.05f); // Delay execution so the newBoard doesn't retrigger this event
    }

    protected void SetPlayState(EPlayState state)
    {
        EPlayState oldState = playState;
        boardInputAsync.AwaitBefore(() => // This delay allows to avoid trigering different EPlayState events on the same frame
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

    public EPlayState GetPlayState() => playState;
    public bool GetIsControllerPlayer() => isControlledPlayer;
    public Color GetPlayerColor() => playerColor;
}
