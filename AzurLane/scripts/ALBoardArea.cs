using Godot;
using System;
using System.Collections.Generic;

public partial class ALBoardArea : Node3D
{
    Label3D count;
    List<ALCard> cards = [];
    Board board;

    public override void _Ready()
    {
        base._Ready();
        count = GetNode<Label3D>("Count");
        board = this.TryFindParentNodeOfType<Board>();
        cards = this.TryGetAllChildOfType<ALCard>();
        cards.ForEach(card =>
        {
            card.OnFieldIsEmptyUpdate -= OnCardActiveStateUpdateHandler;
            card.OnFieldIsEmptyUpdate += OnCardActiveStateUpdateHandler;
            card.OnCardIsSidewaysUpdate -= OnCardActiveStateUpdateHandler;
            card.OnCardIsSidewaysUpdate += OnCardActiveStateUpdateHandler;
        });
        Callable.From(UpdateCardCount).CallDeferred();
    }

    void OnCardActiveStateUpdateHandler(CardField card)
    {
        UpdateCardCount();
    }

    void UpdateCardCount()
    {
        List<ALCard> validCards = cards.FindAll(card => !card.GetIsEmptyField());
        List<ALCard> activeCards = validCards.FindAll(card => card.GetIsInActiveState());
        count.Text = $"{activeCards.Count}/{validCards.Count}";
    }
}
