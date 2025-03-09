
using System.Collections.Generic;
using Godot;

public partial class PlayerHand : CardGroup
{
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
            case InputAction.Details:
                {
                    AddCardToHand();
                    break;
                }
            case InputAction.Cancel:
                {
                    RemoveCardFromHand();
                    break;
                }
        }
    }

    void AddCardToHand()
    {
        Card newCard = cardTemplate.Instantiate<Card>();
        AddChild(newCard);
        int cardSize = GetCards().Count - 1;
        newCard.Position = new Vector3((cardSize + selectedCardIndex) * -cardSize, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        RepositionHandCards();
    }

    void RemoveCardFromHand()
    {
        RemoveChild(selectedCard);
        selectedCard = null;
        selectedCardIndex--;
        if (selectedCardIndex < 0) selectedCardIndex = 0;
        if (GetCards().Count == 0) return;
        SelectCard(selectedCardIndex);
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

    void RepositionHandCards()
    {
        List<Card> cards = GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Position = new Vector3((i - selectedCardIndex) * -Card.cardSize, 0, 0); // (cardIndex - selectedCardIndex) means the card that is the center
        }
    }
}
