using System.Collections.Generic;
using Godot;

public partial class ALBoard : PlayerBoard
{
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");

    public override void PlaceCardInBoardFromHand(Card cardToPlace)
    {
        ALCard card = cardToPlace.CastToALCard();
        base.PlaceCardInBoardFromHand(cardToPlace);
        GetSelectedCard<ALCard>().UpdateAttributes(card.GetAttributes<ALCardDTO>());
    }

    public ALCard GetCardInPosition(Vector2I position)
    {
        SelectCardField(position);
        return GetSelectedCard<ALCard>();
    }
    public static ALCard FindLastActiveCardInRow(List<ALCard> row)
    {
        var index = row.FindLastIndex(card => card.GetIsInActiveState());
        if (index == -1) GD.PrintErr("[FindLastActiveCardInRow] Cannot find last active index");
        return row[index];
    }
}