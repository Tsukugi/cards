
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
        if (selectedCard is null) return;
        OnPlayCard(selectedCard);
    }

    public void AddCardToHand()
    {
        CardField newCard = cardTemplate.Instantiate<CardField>();
        AddChild(newCard);
        int cardSize = GetCards().Count - 1;
        newCard.Position = new Vector3((cardSize + selectedCard.positionInBoard.X) * -cardSize, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        RepositionHandCards();
    }

    public void RemoveCardFromHand(CardField cardToRemove)
    {
        RemoveChild(cardToRemove);
        RepositionHandCards();
    }

    void OnAxisChangeHandler(Vector2I axis)
    {
        List<CardField> cards = GetCards();
        if (cards.Count == 0) return;
        if (axis.X != 0)
        {
            GD.Print(selectedCard);
            //  SelectCard(cards.GetSafely(selectedCard.positionInBoard.X + axis.X));
            RepositionHandCards();
        }
    }

    /* Will try to position cards in a way that the selected card is centered*/
    void RepositionHandCards()
    {
        List<CardField> cards = GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Position = new Vector3((i - selectedCard.positionInBoard.X) * -cardSize, 0, 0); // (cardIndex - selectedCardIndex) means the card that is the center
        }
    }
}
