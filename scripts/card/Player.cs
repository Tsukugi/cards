
using Godot;
using System.Collections.Generic;

public partial class Player : Node3D
{
    [Export]
    bool isPlayerActive = false;
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    CardGroup selectedGroup;
    PlayerHand hand;
    PlayerBoard board;
    List<CardGroup> groups = new();

    PlayState playState = PlayState.Select;

    public override void _Ready()
    {
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        groups = new() { hand, board };
        int orderedIndex = 0;
        groups.ForEach(group => { group.GroupIndex = orderedIndex; orderedIndex++; }); // Try to assign automatically GroupIndexes in order
        SelectGroup(hand);
        hand.SelectCard(0);
        SetPlayState(PlayState.Select);

        hand.OnPlayCard -= OnPlayCard;
        hand.OnPlayCard += OnPlayCard;
        board.OnPlaceCard -= OnPlaceCard;
        board.OnPlaceCard += OnPlaceCard;
        board.OnCancelPlaceCard -= OnCancelPlaceCard;
        board.OnCancelPlaceCard += OnCancelPlaceCard;
    }

    public override void _Process(double delta)
    {
        if (!isPlayerActive) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    void OnAxisChangeHandler(Vector2 axis)
    {
        if (axis.Y != 0)
        {
            switch (playState)
            {
                case PlayState.Select:
                    {
                        int newGroupIndex = groups.Count.ApplyCircularBounds((int)(selectedGroup.GroupIndex + axis.Y));
                        SelectGroup(groups[newGroupIndex]);
                        break;
                    }
            }
        }
    }

    void SelectGroup(CardGroup newSelectedCardGroup)
    {
        selectedGroup = newSelectedCardGroup;
        groups.ForEach(group => group.SetIsGroupActive(false));
        selectedGroup.SetIsGroupActive(true);
        GD.Print("Selected Group: " + selectedGroup.Name);
    }

    void OnCancelPlaceCard(Card cardPlaced)
    {
        hand.SelectCard(cardPlaced);
        cardPlaced.IsEmptyField = false;
        SetPlayState(PlayState.Select);
        SelectGroup(hand);
    }

    void OnPlaceCard(Card cardPlaced)
    {
        hand.RemoveCardFromHand(cardPlaced);
        hand.SelectCard(0);
        SetPlayState(PlayState.Select);
        SelectGroup(hand);
    }
    void OnPlayCard(Card cardToPlay)
    {
        board.CardToPlay = cardToPlay;
        cardToPlay.IsEmptyField = true;
        SetPlayState(PlayState.PlaceCard);
        SelectGroup(board);
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
