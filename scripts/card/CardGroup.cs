
using System.Collections.Generic;
using Godot;

public partial class CardGroup : Node3D
{

    [Export]
    public int GroupIndex = 0; // Modify this so it can be accessed easily from a List<CardGroup>;
    [Export]
    public bool IsDebugging = false;
    protected bool isGroupActive = false;
    protected int selectedCardIndex = 0;
    protected Card selectedCard = null;

    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");

    public PlayState playState = PlayState.Select;
    public Card SelectedCard { get => selectedCard; }

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
        if (cards.Count == 0)
        {
            selectedCard = null;
            selectedCardIndex = -1;
            GD.PushWarning("[SelectCard] Group is empty, clearing selection");
            return;
        }
        cards[index].IsSelected = true;
        selectedCardIndex = index;
        selectedCard = cards[index];
    }

    public void SelectCard(Card card)
    {
        int cardInCardGroupIndex = FindCardIndex(card);
        if (cardInCardGroupIndex == -1)
        {
            GD.PrintErr("[SelectCard] Card is not part of the current cardGroup");
            return;
        }
        DeselectAllCards();
        card.IsSelected = true;
        selectedCardIndex = cardInCardGroupIndex;
        selectedCard = card;
    }

    public int FindCardIndex(Card card)
    {
        List<Card> cards = GetCards();
        return cards.FindIndex(0, cards.Count, _card => _card == card);
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