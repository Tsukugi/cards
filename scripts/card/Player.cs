
using Godot;
using System.Collections.Generic;

public partial class Player : Node3D
{
    [Export]
    bool isPlayerActive = false;
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    Board selectedBoard;
    Vector2I selectedBoardPosition = new(0, 1);
    PlayerHand hand;
    PlayerBoard board;
    List<Board> groups = new();

    PlayState playState = PlayState.Select;

    public override void _Ready()
    {
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        groups = [hand, board];

        hand.OnPlayCard -= OnPlayCard;
        hand.OnPlayCard += OnPlayCard;
        board.OnPlaceCard -= OnPlaceCard;
        board.OnPlaceCard += OnPlaceCard;
        board.OnCancelPlaceCard -= OnCancelPlaceCard;
        board.OnCancelPlaceCard += OnCancelPlaceCard;

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (!isPlayerActive) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    void StartGameForPlayer (){
        SelectBoard(hand);
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.SelectCard(Vector2I.Zero);
        SetPlayState(PlayState.Select);
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis.Y != 0)
        {
            switch (playState)
            {
                case PlayState.Select:
                    {
                        selectedBoardPosition.Y = groups.Count.ApplyCircularBounds(selectedBoardPosition.Y + axis.Y);
                        SelectBoard(groups[selectedBoardPosition.Y]);
                        break;
                    }
            }
        }
    }

    void SelectBoard(Board newSelectedBoard)
    {
        selectedBoard = newSelectedBoard;
        groups.ForEach(group => group.SetIsGroupActive(false));
        selectedBoard.SetIsGroupActive(true);
        GD.Print("Selected Group: " + selectedBoard.Name);
    }

    void OnCancelPlaceCard(Card cardPlaced)
    {
        cardPlaced.IsEmptyField = false;
        SetPlayState(PlayState.Select);
        SelectBoard(hand);
    }

    void OnPlaceCard(Card cardPlaced)
    {
        hand.RemoveCardFromHand(cardPlaced);
        hand.DeselectAllCards();
        SetPlayState(PlayState.Select);
        SelectBoard(hand);
    }
    void OnPlayCard(Card cardToPlay)
    {
        board.CardToPlay = cardToPlay;
        cardToPlay.IsEmptyField = true;
        SetPlayState(PlayState.PlaceCard);
        SelectBoard(board);
    }


    void SetPlayState(PlayState state)
    {
        PlayState oldState = playState;

        var task = this.Wait(0.1f, () => // This delay allows to avoid trigering different PlayState events on the same frame
          {
              groups.ForEach(group => group.playState = state);
              playState = state;
              GD.Print("Play State: " + oldState + " -> " + playState);
          });
    }
}
