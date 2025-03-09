using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Node3D
{
    [Export]
    public bool IsDebugging = false;
    readonly AxisInputHandler axisInputHandler = new();
    readonly ActionInputHandler actionInputHandler = new();

    public PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    int selectedCardIndex = 0;
    Card selectedCard = null;
    Node3D selectedGroup;
    Node3D hand;
    Node3D board;

    int cardSize = 4;

    public override void _Ready()
    {
        hand = GetNode<Node3D>("Hand");
        board = GetNode<Node3D>("Board");
        selectedGroup = hand;
        SelectCard(GetCards(selectedGroup), selectedCardIndex);
    }
    public override void _Process(double delta)
    {
        Vector2 axis = axisInputHandler.GetAxis();
        InputAction action = actionInputHandler.GetAction();
        onAxisChangeHandler(axis);
        if (action != InputAction.None) DebugLog(action.ToString());
        switch (action)
        {
            case InputAction.Details:
                {
                    Card newCard = cardTemplate.Instantiate<Card>();
                    hand.AddChild(newCard);
					int cardSize = GetCards(hand).Count - 1;
                    newCard.Position = new Vector3((cardSize + selectedCardIndex) * -cardSize, 0, 0); // Card size
                    newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
					RepositionCards();
                    break;
                }
            case InputAction.Cancel:
                {
                    hand.RemoveChild(selectedCard);
                    selectedCard = null;
                    selectedCardIndex--;
                    if (selectedCardIndex < 0) selectedCardIndex = 0;
                    SelectCard(GetCards(hand), selectedCardIndex);
                    RepositionCards();
                    break;
                }
        }
    }

    void onAxisChangeHandler(Vector2 axis)
    {
        List<Card> cards = GetCards(selectedGroup);
        switch ((int)axis.Y)
        {
            case 1:
                DeselectAllCards(cards);
                selectedGroup = hand;
                SelectCard(GetCards(selectedGroup), 0);
                break;
            case -1:
                DeselectAllCards(cards);
                selectedGroup = board;
                SelectCard(GetCards(selectedGroup), 0);
                break;
        }

        if (axis.X != 0)
        {
            int newSelectedCardIndex = selectedCardIndex + (int)axis.X;
            SelectCard(cards, ApplyCircularBounds(cards.Count, newSelectedCardIndex));
            RepositionCards();
        }
    }

    public void DebugLog(string message)
    {
        if (IsDebugging) GD.Print(Name + " -> " + message);
    }

    void SelectCard(List<Card> cards, int index)
    {
        DeselectAllCards(cards);
        cards[index].IsSelected = true;
        selectedCardIndex = index;
        selectedCard = cards[index];
    }

    void DeselectAllCards(List<Card> cards)
    {
        cards.ForEach(card => { card.IsSelected = false; });
    }

    public List<Card> GetCards(Node3D root)
    {
        List<Card> cards = root.TryGetAllChildOfType<Card>();
        return cards;
    }

    void RepositionCards()
    {
        List<Card> cards = GetCards(hand);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Position = new Vector3((i - selectedCardIndex) * -cardSize, 0, 0);
        }
    }


    // Applies circular selection.  size -> 0 || -1 -> size -1
    int ApplyCircularBounds(int size, int index)
    {
        if (IsInsideBounds(size, index)) return index;
        else if (index < 0) return size - 1;
        else return 0;
    }
    bool IsInsideBounds(int size, int index)
    {
        return index >= 0 && index < size;
    }
}
