using System.Collections.Generic;
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
                        case EPlayState.PlaceCard: PlaceCardInBoardFromHand(CardToPlay); break;
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
        SelectCardField(SelectCardPosition);
        GD.Print($"[PlayerBoard.OnAxisChangeHandler] SelectCardField in board for position {SelectCardPosition}");
    }

    void CancelPlaceCard()
    {
        OnCancelPlaceCard(CardToPlay);
        CardToPlay = null;
    }

    public void UpdateSelectedCardDTO(CardDTO cardDTO)
    {
        SelectedCard.IsEmptyField = false;
        SelectedCard.UpdateDTO(cardDTO);
    }

    public void PlaceCardInBoardFromHand(Card CardToPlace)
    {
        if (!SelectedCard.CanPlayerPlaceInThisField())
        {
            GD.PrintErr("[PlaceCardInBoardFromHand] This card place is not placeable!");
            return;
        }
        CardDTO cardDTO = CardToPlace.GetAttributes();
        GD.Print($"[PlaceCardInBoardFromHand] Placing {cardDTO.name}!");
        UpdateSelectedCardDTO(cardDTO);
        OnPlaceCard(CardToPlace);
        CardToPlay = null;
    }

    public void SetAllCardsAsActive()
    {
        List<Card> cards = this.TryGetAllChildOfType<Card>(true);

        foreach (Card card in cards)
        {
            card.SetIsSideWays(false);
        }
    }

    public static Card FindLastEmptyFieldInRow(List<Card> row) => row.Find(card => card.IsEmptyField == true);
}
