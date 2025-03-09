
using System.Collections.Generic;
using Godot;


public partial class PlayerHand : CardGroup
{
    public delegate void PlayCardEventHandler(Card card);
    public event PlayCardEventHandler OnPlayCard;

    public override void _Ready()
    {
        SelectCard(selectedCardIndex);
    }

    public override void _Process(double delta)
    {
        if (!isGroupActive) return;

        Vector2 axis = axisInputHandler.GetAxis();
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
        Card newCard = cardTemplate.Instantiate<Card>();
        AddChild(newCard);
        int cardSize = GetCards().Count - 1;
        newCard.Position = new Vector3((cardSize + selectedCardIndex) * -cardSize, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        RepositionHandCards();
    }

    public void RemoveCardFromHand(Card cardToRemove)
    {
        if (FindCardIndex(cardToRemove) == -1)
        {
            GD.PrintErr("[RemoveCardFromHand] Attempted to remove a non existent card from hand");
            return;
        }
        RemoveChild(cardToRemove);
        RepositionHandCards();
    }

    void OnAxisChangeHandler(Vector2 axis)
    {
        List<Card> cards = GetCards();
        if (cards.Count == 0) return;
        if (axis.X != 0)
        {
            int newSelectedCardIndex = selectedCardIndex + (int)axis.X;
            SelectCard(cards.Count.ApplyCircularBounds(newSelectedCardIndex));
            RepositionHandCards();
        }
    }

    /* Will try to position cards in a way that the selected card is centered*/
    void RepositionHandCards()
    {
        List<Card> cards = GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Position = new Vector3((i - selectedCardIndex) * -Card.cardSize, 0, 0); // (cardIndex - selectedCardIndex) means the card that is the center
        }
    }
}
