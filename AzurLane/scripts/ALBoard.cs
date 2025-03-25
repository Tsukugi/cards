using System.Collections.Generic;
using Godot;

public partial class ALBoard : PlayerBoard
{
    public new ALCard GetSelectedCard() => SelectedCard is ALCard card ? card : null; // Hack to force a type, we should use ALCards anyways
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    public static ALCard FindLastActiveCardInRow(List<ALCard> row)
    {
        var index = row.FindLastIndex(card => card.GetIsInActiveState());
        if (index == -1) GD.PrintErr("[FindLastActiveCardInRow] Cannot find last active index");
        return row[index];
    }
}