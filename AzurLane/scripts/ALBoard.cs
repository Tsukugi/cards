using System.Collections.Generic;
using Godot;

public partial class ALBoard : PlayerBoard
{
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");

    public new ALCard GetSelectedCard()
    {
        if (SelectedCard is null)
        {
            GD.PrintErr($"[ALBoard.GetSelectedCard] Selected card is null!");
            return null;
        }
        if (SelectedCard is not ALCard card)// Hack to force a type, we should use ALCards anyways
        {
            GD.PrintErr($"[ALBoard.GetSelectedCard] Cannot play a card not belonging to AzurLane TCG, {SelectedCard.Name} is {SelectedCard.GetType()} ");
            return null;
        }
        return card;
    }
    public ALCard GetCardInPosition(Vector2I position)
    {
        SelectCardField(position);
        return GetSelectedCard();
    }
    public static ALCard FindLastActiveCardInRow(List<ALCard> row)
    {
        var index = row.FindLastIndex(card => card.GetIsInActiveState());
        if (index == -1) GD.PrintErr("[FindLastActiveCardInRow] Cannot find last active index");
        return row[index];
    }
}