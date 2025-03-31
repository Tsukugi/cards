using System.Collections.Generic;
using Godot;

public partial class PlayerBoard : Board
{
    public event PlaceCardEvent OnPlaceCardStart;
    public event PlaceCardEvent OnPlaceCardEnd;
    public event PlaceCardEvent OnPlaceCardCancel;
    public event CardTriggerEvent OnCardTrigger;
    public override event BoardEdgeEvent OnBoardEdge;
    public override event BoardCardEvent OnSelectFixedCardEdge;
    public Card CardToPlace = null;
    public override void _Process(double delta)
    {
        if (!GetCanReceivePlayerInput()) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
        InputAction action = actionInputHandler.GetAction();
        if (action != InputAction.None) GD.Print($"[Action Triggered by player {GetPlayerPlayingTurn().Name}] {action}");
        OnActionHandler(action);
    }

    void OnActionHandler(InputAction action)
    {
        Player playingPlayer = GetPlayerPlayingTurn();
        switch (action)
        {
            case InputAction.Ok:
                {
                    switch (playingPlayer.GetPlayState())
                    {
                        case EPlayState.PlaceCard: StartPlaceCard(CardToPlace); break;
                        case EPlayState.Select: TriggerCard(); break;
                        case EPlayState.SelectTarget: TriggerCard(); break;
                    }
                    break;
                }

            case InputAction.Cancel:
                {
                    switch (playingPlayer.GetPlayState())
                    {
                        case EPlayState.PlaceCard: CancelPlaceCard(); break;
                    }
                    break;
                }
        }
    }

    void TriggerCard()
    {
        Card card = GetSelectedCard<Card>(GetPlayerPlayingTurn());
        GD.Print($"[TriggerCard] Triggering card {card}");
        if (OnCardTrigger is not null && card is not null) OnCardTrigger(card);
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;

        Player playingPlayer = GetPlayerPlayingTurn();

        if (OnSelectFixedCardEdge is not null && GetSelectedCard<Card>(playingPlayer) is Card selectedCard)
        {
            // Override search with predefined edges
            if (axis == Vector2I.Up && selectedCard.EdgeUp is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeUp); return; }
            if (axis == Vector2I.Down && selectedCard.EdgeDown is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeDown); return; }
            if (axis == Vector2I.Left && selectedCard.EdgeLeft is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeLeft); return; }
            if (axis == Vector2I.Right && selectedCard.EdgeRight is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeRight); return; }
        }

        Card? card = SearchForCardInBoard(selectedCardPosition, axis, 1, 10);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnBoardEdge is not null && GetCanReceivePlayerInput()) OnBoardEdge(this, axis);
            return;
        }

        selectedCardPosition = card.PositionInBoard;
        SelectCardField(playingPlayer, selectedCardPosition);
        GD.Print($"[{playingPlayer.Name}.PlayerBoard.OnAxisChangeHandler] SelectCardField in board for position {selectedCardPosition}");
    }

    void CancelPlaceCard()
    {
        OnPlaceCardCancel(CardToPlace);
        CardToPlace = null;
    }


    protected void StartPlaceCard(Card cardtoPlace)
    {
        if (OnPlaceCardStart is not null) OnPlaceCardStart(cardtoPlace);
    }

    // --- Public API ---

    public void SetAllCardsAsActive()
    {
        List<Card> cards = this.TryGetAllChildOfType<Card>(true);

        foreach (Card card in cards)
        {
            card.SetIsSideWays(false);
        }
    }

    public static Card FindLastEmptyFieldInRow(List<Card> row) => row.Find(card => card.IsEmptyField == true);
    public virtual void PlaceCardInBoardFromHand<T>(T cardToPlace) where T : Card
    {
        Player playingPlayer = GetPlayerPlayingTurn();
        T? selectedCard = GetSelectedCard<T>(playingPlayer);
        if (selectedCard is null) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place cannot be found!"); return; }
        if (!selectedCard.CanPlayerPlaceInThisField()) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place is not placeable!"); return; }
        var attributes = cardToPlace.GetAttributes<CardDTO>();
        GD.Print($"[PlaceCardInBoardFromHand] Placing {attributes.name}!");
        selectedCard.UpdateAttributes(attributes);
        OnPlaceCardEnd(cardToPlace);
        CardToPlace = null;
    }
}
