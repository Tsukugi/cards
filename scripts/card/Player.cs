
using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Node3D
{
    public delegate void SelectPlayerBoardPositionEvent(Vector2I position, Board.BoardProvidedCallback boardEvent);
    public delegate void SelectPlayerBoardEvent(Board board);
    public event SelectPlayerBoardPositionEvent OnPlayerBoardPositionSelect;
    public event SelectPlayerBoardEvent OnPlayerBoardSelect;

    [Export]
    protected bool isControlledPlayer = false;
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected Board selectedBoard;
    protected PlayerHand hand;
    protected PlayerBoard board;

    EPlayState playState = EPlayState.Select;

    public override void _Ready()
    {
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        InitializeEvents();
    }

    public override void _Process(double delta)
    {
        if (!isControlledPlayer) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    protected void InitializeEvents()
    {
        hand.OnPlayCardStart -= OnPlayCardStartHandler;
        hand.OnPlayCardStart += OnPlayCardStartHandler;
        board.OnPlaceCardStart -= OnPlaceCardStartHandler;
        board.OnPlaceCardStart += OnPlaceCardStartHandler;
        board.OnPlaceCardEnd -= OnPlaceCardEndHandler;
        board.OnPlaceCardEnd += OnPlaceCardEndHandler;
        board.OnPlaceCardCancel -= OnPlaceCardCancelHandler;
        board.OnPlaceCardCancel += OnPlaceCardCancelHandler;
        board.OnEdgeBoardRequest -= OnEdgeBoardRequestHandler;
        board.OnEdgeBoardRequest += OnEdgeBoardRequestHandler;
        hand.OnEdgeBoardRequest -= OnEdgeBoardRequestHandler;
        hand.OnEdgeBoardRequest += OnEdgeBoardRequestHandler;
        GD.Print("[InitializeEvents] Default Player events initialized");
    }


    void OnAxisChangeHandler(Vector2I axis)
    {

    }
    protected void OnPlaceCardCancelHandler(Card cardPlaced)
    {
        cardPlaced.IsEmptyField = false;
        SetPlayState(EPlayState.Select);
        SelectBoard(hand);
    }

    protected void OnPlaceCardStartHandler(Card cardPlaced)
    {
        board.PlaceCardInBoardFromHand(cardPlaced);
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
        board.CardToPlay = cardToPlay;
        cardToPlay.IsEmptyField = true;
        SetPlayState(EPlayState.PlaceCard);
        SelectBoard(board);
    }

    protected void OnEdgeBoardRequestHandler(Vector2I axis)
    {
        if (axis == Vector2I.Down) SelectBoard(hand);
        else if (axis == Vector2I.Up) SelectBoard(board);
    }

    protected void SelectBoard(Board board)
    {
        if (OnPlayerBoardSelect is null) return;
        OnPlayerBoardSelect(board);
        selectedBoard = board;
    }

    protected void SetPlayState(EPlayState state)
    {
        EPlayState oldState = playState;
        var task = this.Wait(0.1f, () => // This delay allows to avoid trigering different EPlayState events on the same frame
          {
              //  groups.ForEach(group => group.playState = state);
              playState = state;
              GD.Print("[SetPlayState] " + oldState + " -> " + playState);
          });
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

    public EPlayState GetPlayState() => playState;
    public bool GetIsControllerPlayer() => isControlledPlayer;
}
