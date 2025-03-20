
using System.Collections.Generic;
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
        if (!selectedCard.IsPlaceable)
        {
            GD.PushWarning("[PlaceCardInBoard] This card place is not placeable!");
            return;
        }
        selectedCard.IsEmptyField = false;
        selectedCard.cardDTO = new CardDTO(); // TODO assign card;
        OnPlaceCard(CardToPlay);
        CardToPlay = null;
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        List<CardField> cards = GetCards();
        if (cards.Count == 0) return;
        if (axis.X != 0)
        {
            // SelectCard(cards.GetSafely(selectedCard.positionInBoard.X + axis.X));
        }
    }
}
