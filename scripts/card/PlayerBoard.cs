using Godot;

public partial class PlayerBoard : Board
{
    public delegate void PlaceCardEventHandler(Card card);
    public event PlaceCardEventHandler OnPlaceCard;
    public event PlaceCardEventHandler OnCancelPlaceCard;
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
                    switch (playState)
                    {
                        case PlayState.PlaceCard: PlaceCardInBoard(); break;
                    }
                    break;
                }

            case InputAction.Cancel:
                {
                    switch (playState)
                    {
                        case PlayState.PlaceCard: CancelPlaceCard(); break;
                    }
                    break;
                }
        }
    }

    void CancelPlaceCard()
    {
        OnCancelPlaceCard(CardToPlay);
        CardToPlay = null;
    }
    void PlaceCardInBoard()
    {
        if (!SelectedCard.IsPlaceable)
        {
            GD.PushWarning("[PlaceCardInBoard] This card place is not placeable!");
            return;
        }
        SelectedCard.IsEmptyField = false;
        SelectedCard.cardDTO = new CardDTO(); // TODO assign card;
        OnPlaceCard(CardToPlay);
        CardToPlay = null;
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;
        SelectCard(SelectCardPosition + axis);
    }
}
