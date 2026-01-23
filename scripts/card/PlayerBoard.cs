using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class PlayerBoard : Board
{
    public override event BoardEdgeEvent OnBoardEdge;
    public override event BoardCardEvent OnSelectFixedCardEdge;
    public Card CardToPlace = null;
    public override void OnInputAxisChange(Player player, Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;
        Card selectedCard = GetSelectedCard<Card>(player);
        if (OnSelectFixedCardEdge is not null && selectedCard is not null)
        {
            // Override search with predefined edges
            if (axis == Vector2I.Up && selectedCard.EdgeUp is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeUp); return; }
            if (axis == Vector2I.Down && selectedCard.EdgeDown is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeDown); return; }
            if (axis == Vector2I.Left && selectedCard.EdgeLeft is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeLeft); return; }
            if (axis == Vector2I.Right && selectedCard.EdgeRight is not null) { OnSelectFixedCardEdge(this, selectedCard.EdgeRight); return; }
        }

        if (selectedCard is null)
        {
            throw new System.InvalidOperationException("[PlayerBoard.OnInputAxisChange] No selected card found.");
        }

        if (axis.Y > 0 && selectedCard.PositionInBoard.Y >= 2)
        {
            if (OnBoardEdge is not null) OnBoardEdge(this, axis);
            return;
        }

        Vector2I startingPosition = GetSelectedCardPosition(player);
        int searchRange = GetAxisSearchRange(startingPosition, axis);
        Card? card = SearchForCardInBoard(startingPosition, axis, searchRange, 10);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnBoardEdge is not null) OnBoardEdge(this, axis);
            return;
        }

        SelectCardField(player, card.PositionInBoard);
        // GD.Print($"[{player.Name}.PlayerBoard.OnAxisChangeHandler] SelectCardField in board for position {selectedCardPosition}");
    }

    int GetAxisSearchRange(Vector2I startingPosition, Vector2I axis)
    {
        int maxRange = 0;
        List<Card> cards = GetCardsInTree();
        foreach (Card card in cards)
        {
            if (!card.IsInputSelectable) continue;
            Vector2I delta = card.PositionInBoard - startingPosition;
            if (axis == Vector2I.Right && delta.X > 0)
            {
                maxRange = Math.Max(maxRange, delta.X);
                continue;
            }
            if (axis == Vector2I.Left && delta.X < 0)
            {
                maxRange = Math.Max(maxRange, -delta.X);
                continue;
            }
            if (axis == Vector2I.Down && delta.Y > 0)
            {
                maxRange = Math.Max(maxRange, delta.Y);
                continue;
            }
            if (axis == Vector2I.Up && delta.Y < 0)
            {
                maxRange = Math.Max(maxRange, -delta.Y);
            }
        }
        return maxRange;
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

    public static Card FindLastEmptyFieldInRow(List<Card> row) => row.Find(card => card.GetIsEmptyField());
    public virtual async Task PlaceCardInBoardFromHand<T>(Player player, T cardToPlace) where T : Card
    {
        T? selectedCard = GetSelectedCard<T>(player);
        if (selectedCard is null) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place cannot be found!"); return; }
        if (!selectedCard.CanPlayerPlaceInThisField()) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place is not placeable!"); return; }
        var attributes = cardToPlace.GetAttributes<CardDTO>();
        GD.Print($"[PlaceCardInBoardFromHand] Placing {attributes.name}!");
        selectedCard.UpdateAttributes(attributes);
        await selectedCard.TryToTriggerCardEffect(CardEffectTrigger.WhenPlayedIntoBoard);
        GetCardsInTree().ForEach(async card => await card.TryToTriggerCardEffect(CardEffectTrigger.AnyCardPlayed));
        CardToPlace = null;
    }

    public T GetCardInPosition<T>(ALPlayer player, Vector2I position) where T : Card
    {
        SelectCardField(player, position);
        return GetSelectedCard<T>(player);
    }
}
