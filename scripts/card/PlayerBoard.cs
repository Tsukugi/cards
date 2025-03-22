using Godot;

public partial class PlayerBoard : Board
{
    public delegate void PlaceCardEventHandler(Card card);
    public event PlaceCardEventHandler OnPlaceCard;
    public event PlaceCardEventHandler OnCancelPlaceCard;
    public event BoardEdgeEvent OnEdgeBoardRequest;
    public Card CardToPlay = null;

    public override void _Process(double delta)
    {
        if (!isBoardActive) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
        OnActionHandler(actionInputHandler.GetAction());
    }

    void OnActionHandler(InputAction action)
    {
        switch (action)
        {
            case InputAction.Ok:
                {
                    switch (player.GetPlayState())
                    {
                        case EPlayState.PlaceCard: PlaceCardInBoard(); break;
                    }
                    break;
                }

            case InputAction.Cancel:
                {
                    switch (player.GetPlayState())
                    {
                        case EPlayState.PlaceCard: CancelPlaceCard(); break;
                    }
                    break;
                }
        }
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;

        Card? card = SearchForCardInBoard(SelectCardPosition, axis, 1, 10);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnEdgeBoardRequest is not null) OnEdgeBoardRequest(axis);
            return;
        }

        SelectCardPosition = card.PositionInBoard;
        SelectCard(SelectCardPosition);
        GD.Print($"[PlayerBoard.OnAxisChangeHandler] SelectCard in board for position {SelectCardPosition}");
    }

    void CancelPlaceCard()
    {
        OnCancelPlaceCard(CardToPlay);
        CardToPlay = null;
    }

    void PlaceCardInBoard()
    {
        if (!SelectedCard.CanPlayerPlaceInThisField())
        {
            GD.PushWarning("[PlaceCardInBoard] This card place is not placeable!");
            return;
        }
        GD.PushWarning($"[PlaceCardInBoard] Placing {CardToPlay}!");
        SelectedCard.IsEmptyField = false;
        SelectedCard.cardDTO = CardToPlay.cardDTO; // TODO assign card;
        OnPlaceCard(CardToPlay);
        CardToPlay = null;
    }
}
