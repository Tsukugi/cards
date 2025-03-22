
using Godot;

public partial class Player : Node3D
{
    public delegate void SelectPlayerBoardPositionEvent(Vector2I position, Board.BoardProvidedCallback boardEvent);
    public delegate void SelectPlayerBoardEvent(Board board);
    public event SelectPlayerBoardPositionEvent OnPlayerBoardPositionSelect;
    public event SelectPlayerBoardEvent OnPlayerBoardSelect;

    [Export]
    bool isPlayerActive = false;
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    Board selectedBoard;
    Vector2I selectedBoardPosition = new(0, 1);
    PlayerHand hand;
    PlayerBoard board;

    PlayState playState = PlayState.Select;

    public override void _Ready()
    {
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");

        hand.OnPlayCard -= OnPlayCard;
        hand.OnPlayCard += OnPlayCard;
        board.OnPlaceCard -= OnPlaceCard;
        board.OnPlaceCard += OnPlaceCard;
        board.OnCancelPlaceCard -= OnCancelPlaceCard;
        board.OnCancelPlaceCard += OnCancelPlaceCard;
        board.OnEdgeBoardRequest -= OnEdgeBoardRequestHandler;
        board.OnEdgeBoardRequest += OnEdgeBoardRequestHandler;
        hand.OnEdgeBoardRequest -= OnEdgeBoardRequestHandler;
        hand.OnEdgeBoardRequest += OnEdgeBoardRequestHandler;

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (!isPlayerActive) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    void StartGameForPlayer()
    {
        SelectBoard(hand);
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.SelectCard(Vector2I.Zero);
        board.SelectCard(new Vector2I(1, 1));
        SetPlayState(PlayState.Select);
    }

    void OnAxisChangeHandler(Vector2I axis)
    {

    }

    void OnEdgeBoardRequestHandler(Vector2I axis)
    {
        if (axis == Vector2I.Down) SelectBoard(hand);
        else if (axis == Vector2I.Up) SelectBoard(board);
    }

    void SelectBoard(Board board)
    {
        if (OnPlayerBoardSelect is null) return;
        OnPlayerBoardSelect(board);
        selectedBoard = board;
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
              //  groups.ForEach(group => group.playState = state);
              playState = state;
              GD.Print("[SetPlayState] " + oldState + " -> " + playState);
          });
    }
}
