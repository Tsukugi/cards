
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

    public override void _Ready()
    {
        base._Ready();
        originalPosition = Position;
    }

    public override void _Process(double delta)
    {
        bool allowInput = GetCanReceivePlayerInput();
        if (true) { Position = allowInput ? originalPosition : originalPosition + positionOffsetWhenInactive; } // TODO add position for active enemy hand
        if (!allowInput) return;
        Vector2I axis = axisInputHandler.GetAxis();
        OnAxisChangeHandler(axis);
        ManageAction();
    }

    protected void ManageAction()
    {
        Player playingPlayer = GetPlayerPlayingTurn();
        InputAction action = actionInputHandler.GetAction();
        switch (action)
        {
            case InputAction.Ok:
                {
                    switch (playingPlayer.GetPlayState())
                    {
                        case EPlayState.Select: StartPlayCard(GetSelectedCard<Card>(playingPlayer)); break;
                    }
                    break;
                }
        }
    }

    protected void StartPlayCard(Card card)
    {
        if (card is null) { GD.Print($"[PlayCard] No selected card available"); return; }
        if (OnPlayCardStart is not null) OnPlayCardStart(card);
    }

    public void AddCardToHand(CardDTO attributes)
    {
        Card newCard = cardTemplate.Instantiate<Card>();
        int numCardsInHand = GetCardsInHand().Count;
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + selectedCardPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateAttributes(attributes);
        RepositionHandCards();
    }

    public void RemoveCardFromHand(CardField cardToRemove)
    {
        GD.Print($"[RemoveCardFromHand] Removing {cardToRemove}");
        RemoveChild(cardToRemove);
        RepositionHandCards();
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis == Vector2I.Zero) return;

        Player playingPlayer = GetPlayerPlayingTurn();
        Vector2I newPosition = selectedCardPosition + axis;

        Card? card = FindCardInTree(newPosition);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnBoardEdge is not null && GetCanReceivePlayerInput()) OnBoardEdge(this, axis);
            return;
        }

        selectedCardPosition = newPosition;
        SelectCardField(playingPlayer, selectedCardPosition);
        RepositionHandCards();
        GD.Print($"[PlayerHand.OnAxisChangeHandler] SelectCardField in board for position {newPosition}");
    }

    protected List<Card> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<Card>();
    }

    /* Will try to position cards in a way that the selected card is centered*/
    protected void RepositionHandCards()
    {
        List<Card> cards = GetCardsInHand();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].PositionInBoard.X = i; // This reassigns the position in board to fill gaps
            cards[i].Position = new Vector3((i - selectedCardPosition.X) * cards[i].CardWidth, 0, 0); // (cardIndex - SelectCardPosition.X) means the card that is the center
        }
    }
}
