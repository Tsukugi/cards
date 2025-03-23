
using System.Collections.Generic;
using Godot;


public partial class PlayerHand : Board
{
    public delegate void PlayCardEventHandler(Card card);
    public event PlayCardEventHandler OnPlayCard;
    public event BoardEdgeEvent OnEdgeBoardRequest;


    public override void _Process(double delta)
    {
        if (!isBoardActive) return;

        Vector2I axis = axisInputHandler.GetAxis();
        InputAction action = actionInputHandler.GetAction();
        OnAxisChangeHandler(axis);
        switch (action)
        {
            case InputAction.Ok:
                {
                    switch (player.GetPlayState())
                    {
                        case EPlayState.Select: PlayCard(); break;
                    }
                    break;
                }

            case InputAction.Cancel:
                {
                    break;
                }
            case InputAction.Details:
                {
                    break;
                }
        }

    }
    void PlayCard()
    {
        if (SelectedCard is null) { GD.Print($"[PlayCard] No selected card available"); return; }
        OnPlayCard(SelectedCard);
    }

    public void AddCardToHand(CardDTO cardDTO)
    {
        Card newCard = cardTemplate.Instantiate<Card>();

        int numCardsInHand = GetCardsInHand().Count;
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + SelectCardPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateDTO(cardDTO);
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

        Vector2I newPosition = SelectCardPosition + axis;

        // Going up should select the board
        if (axis == Vector2I.Up)
        {
            if (OnEdgeBoardRequest is not null) OnEdgeBoardRequest(axis);
            return;
        }

        Card? card = FindCardInTree(newPosition);
        if (card is null) // We didn't find a card with the specified position
        {
            if (OnEdgeBoardRequest is not null) OnEdgeBoardRequest(axis);
            return;
        }

        SelectCardPosition = newPosition;
        SelectCardField(SelectCardPosition);
        RepositionHandCards();
        GD.Print($"[PlayerHand.OnAxisChangeHandler] SelectCardField in board for position {newPosition}");
    }

    List<Card> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<Card>();
    }

    /* Will try to position cards in a way that the selected card is centered*/
    void RepositionHandCards()
    {
        List<Card> cards = GetCardsInHand();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].PositionInBoard.X = i; // This reassigns the position in board to fill gaps
            cards[i].Position = new Vector3((i - SelectCardPosition.X) * -cards[i].CardWidth, 0, 0); // (cardIndex - SelectCardPosition.X) means the card that is the center
        }
    }
}
