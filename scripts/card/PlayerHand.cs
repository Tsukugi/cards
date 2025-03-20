
using System.Collections.Generic;
using Godot;


public partial class PlayerHand : Board
{
    public delegate void PlayCardEventHandler(Card card);
    public event PlayCardEventHandler OnPlayCard;


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
                    switch (playState)
                    {
                        case PlayState.Select: PlayCard(); break;
                    }
                    break;
                }

            case InputAction.Cancel:
                {
                    break;
                }
            case InputAction.Details:
                {
                    switch (playState)
                    {
                        case PlayState.Select: AddCardToHand(); break;
                    }
                    break;
                }
        }

    }
    void PlayCard()
    {
        if (SelectedCard is null) return;
        OnPlayCard(SelectedCard);
    }

    public void AddCardToHand()
    {
        Card newCard = cardTemplate.Instantiate<Card>();

        int numCardsInHand = GetCardsInHand().Count;
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + SelectCardPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        RepositionHandCards();
    }

    public void RemoveCardFromHand(CardField cardToRemove)
    {
        RemoveChild(cardToRemove);
        RepositionHandCards();
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        if (axis.X == 0) return;
        SelectCard(SelectCardPosition + axis);
        RepositionHandCards();
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
            cards[i].Position = new Vector3((i - SelectCardPosition.X) * -CardField.cardSize, 0, 0); // (cardIndex - selectedCardIndex) means the card that is the center
        }
    }
}
