using System.Collections.Generic;
using Godot;

public partial class ALBoard : PlayerBoard
{
    [Export]
    ALCard flagshipCard;

    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    public override void PlaceCardInBoardFromHand<T>(Player player, T cardToPlace)
    {
        ALCard selectedField = GetSelectedCard<ALCard>(player);
        if (selectedField is null)
        {
            GD.PrintErr($"[PlaceCardInBoardFromHand] Field cannot be found");
            return;
        }
        base.PlaceCardInBoardFromHand(player, cardToPlace);
        selectedField.UpdateAttributes(cardToPlace.GetAttributes<ALCardDTO>());
    }

    public static ALCard FindLastActiveCardInRow(List<ALCard> row)
    {
        var index = row.FindLastIndex(card => card.GetIsInActiveState());
        if (index == -1) GD.PrintErr("[FindLastActiveCardInRow] Cannot find last active index");
        return row[index];
    }

    public ALCard GetFlagship()
    {
        if (flagshipCard is null) GD.PushWarning("[GetFlagship] This ALBoard doesn't have any flagship assigned");
        return flagshipCard;
    }
    public List<ALCard> GetUnitFields() => GetNode<Node3D>("Units").TryGetAllChildOfType<ALCard>();
    public List<ALCard> GetUnits() => GetUnitFields().FindAll(card => card.IsCardUnit());
}