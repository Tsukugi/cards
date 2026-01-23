
using System.Collections.Generic;
using Godot;


public partial class PlayerHand : Board
{
    public delegate void PlayCardEventHandler(Card card);
    public event PlayCardEventHandler OnPlayCardStart;
    public override event BoardEdgeEvent OnBoardEdge;

    [Export]
    Vector3 positionOffsetWhenInactive = new();
    Vector3 originalPosition = new();
    bool showHandVisible = true;

    public override void _Ready()
    {
        base._Ready();
        originalPosition = Position;
    }

    public override void _Process(double delta)
    {
        // TODO add position for active enemy hand
        Position = showHandVisible ? originalPosition : originalPosition + positionOffsetWhenInactive;
    }

    public void AddCardToHand(CardDTO attributes)
    {
        Card newCard = cardTemplate.Instantiate<Card>();
        int numCardsInHand = GetCardsInHand().Count;
        Vector2I ownerSelectedPosition = GetOwnerSelectedCardPosition();
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + ownerSelectedPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateAttributes(attributes);
        RepositionHandCards();
    }

    public void RemoveCardFromHand(Player player, CardField cardToRemove)
    {
        GD.Print($"[RemoveCardFromHand] Removing {cardToRemove}");
        RemoveChild(cardToRemove);
        cardToRemove.QueueFree();
        RepositionHandCards();
        SelectCardField(player, GetSelectedCardPosition(player)); // Try to select again on that position
    }

    public override void SelectCardField(Player player, Vector2I position, bool syncToNet = true)
    {
        base.SelectCardField(player, position, syncToNet);
        RepositionHandCards();
    }

    public override void OnInputAxisChange(Player player, Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;
        Vector2I newPosition = GetSelectedCardPosition(player) + axis;

        Card? card = FindCardInTree(newPosition);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnBoardEdge is not null) OnBoardEdge(this, axis);
            return;
        }

        SelectCardField(player, newPosition);
        // GD.Print($"[PlayerHand.OnAxisChangeHandler] SelectCardField in board for position {newPosition}");
    }

    protected List<Card> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<Card>();
    }

    /* Will try to position cards in a way that the selected card is centered*/
    protected void RepositionHandCards()
    {
        Vector2I ownerSelectedPosition = GetOwnerSelectedCardPosition();
        List<Card> cards = GetCardsInHand();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].PositionInBoard.X = i; // This reassigns the position in board to fill gaps
            cards[i].Position = new Vector3((i - ownerSelectedPosition.X) * cards[i].CardWidth, 0, 0); // (cardIndex - SelectCardPosition.X) means the card that is the center
        }
    }

    public void SetShowHand(bool value) => showHandVisible = value;
}
