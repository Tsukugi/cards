
using System.Collections.Generic;
using Godot;

public partial class PlayerBoard : CardGroup
{

    public override void _Process(double delta)
    {
        if (!isGroupActive) return;
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    void OnAxisChangeHandler(Vector2 axis)
    {
        List<Card> cards = GetCards();
        if (cards.Count == 0) return;
        if (axis.X != 0)
        {
            int newSelectedCardIndex = selectedCardIndex + (int)axis.X;
            SelectCard(cards.Count.ApplyCircularBounds(newSelectedCardIndex));
        }
    }
}
