using System.Collections.Generic;
using Godot;

public partial class PlayerBoard : Board
{
    public event PlaceCardEvent OnPlaceCardStart;
    public event PlaceCardEvent OnPlaceCardEnd;
    public event PlaceCardEvent OnPlaceCardCancel;
    public override event BoardEdgeEvent OnBoardEdge;
    public override event BoardCardEvent OnSelectFixedCardEdge;
    public Card CardToPlace = null;
    public override void OnActionHandler(Player player, InputAction action)
    {
        base.OnActionHandler(player, action);
        switch (action)
        {
            case InputAction.Ok:
                {
                    switch (player.GetPlayState())
                    {
                        case EPlayState.PlaceCard: StartPlaceCard(CardToPlace); break;
                        case EPlayState.Select: TriggerCard(player); break;
                        case EPlayState.SelectTarget: TriggerCard(player); break;
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

    public override void OnInputAxisChange(Player player, Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;
        if (OnSelectFixedCardEdge is not null && GetSelectedCard<Card>(player) is Card selectedCard)
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
            if (OnBoardEdge is not null) OnBoardEdge(this, axis);
            return;
        }

        selectedCardPosition = card.PositionInBoard;
        SelectCardField(player, selectedCardPosition);
        // GD.Print($"[{player.Name}.PlayerBoard.OnAxisChangeHandler] SelectCardField in board for position {selectedCardPosition}");
    }

    void CancelPlaceCard()
    {
        if (OnPlaceCardCancel is not null) OnPlaceCardCancel(CardToPlace);
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
    public virtual void PlaceCardInBoardFromHand<T>(Player player, T cardToPlace) where T : Card
    {
        T? selectedCard = GetSelectedCard<T>(player);
        if (selectedCard is null) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place cannot be found!"); return; }
        if (!selectedCard.CanPlayerPlaceInThisField()) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place is not placeable!"); return; }
        var attributes = cardToPlace.GetAttributes<CardDTO>();
        GD.Print($"[PlaceCardInBoardFromHand] Placing {attributes.name}!");
        selectedCard.UpdateAttributes(attributes);
        selectedCard.TryToTriggerCardEffect(CardEffectTrigger.WhenPlayed);
        GetCardsInTree().ForEach(card => card.TryToTriggerCardEffect(CardEffectTrigger.OnCardPlayed));
        OnPlaceCardEnd(cardToPlace);
        CardToPlace = null;
    }

    public T GetCardInPosition<T>(ALPlayer player, Vector2I position) where T : Card
    {
        SelectCardField(player, position);
        return GetSelectedCard<T>(player);
    }
}
