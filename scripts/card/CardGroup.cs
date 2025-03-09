
using System.Collections.Generic;
using Godot;

public partial class CardGroup : Node3D
{
    [Export]
    public bool IsDebugging = false;
    protected bool isGroupActive = false;
    protected int selectedCardIndex = 0;
    protected Card selectedCard = null;

    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");

    public List<Card> GetCards()
    {
        List<Card> cards = this.TryGetAllChildOfType<Card>();
        return cards;
    }

    public void DeselectAllCards()
    {
        GetCards().ForEach(card => { card.IsSelected = false; });
    }

    public void SelectCard(int index)
    {
        List<Card> cards = GetCards();
        DeselectAllCards();
        cards[index].IsSelected = true;
        selectedCardIndex = index;
        selectedCard = cards[index];
    }

    public void DebugLog(string message)
    {
        if (IsDebugging) GD.Print(Name + " -> " + message);
    }

    public void SetIsGroupActive(bool value)
    {
        isGroupActive = value;
        if (isGroupActive) SelectCard(0);
        else DeselectAllCards();
    }
}