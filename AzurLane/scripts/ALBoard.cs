using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALBoard : PlayerBoard
{
    static readonly string[] UnitFieldNames = ["FrontRow", "FrontRow2", "FrontRow3", "BackRow", "Flagship", "BackRow2"];
    [Export]
    ALCard flagshipCard, enemyFlagshipCard;

    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    const string PlayerRootName = "Player";
    const string EnemyRootName = "EnemyPlayer";

    public Node3D GetUnitsRoot(ALBoardSide side) => GetNode<Node3D>($"{GetRootName(side)}/Units");
    public ALBoardArea GetCostArea(ALBoardSide side) => GetNode<ALBoardArea>($"{GetRootName(side)}/CostArea");
    public ALBoardArea GetDurabilityArea(ALBoardSide side) => GetNode<ALBoardArea>($"{GetRootName(side)}/FlagshipDurability");
    public ALCard GetDeckField(ALBoardSide side) => GetNode<ALCard>($"{GetRootName(side)}/Deck");
    public ALCard GetCubeDeckField(ALBoardSide side) => GetNode<ALCard>($"{GetRootName(side)}/CubeDeck");
    public ALCard GetRetreatField(ALBoardSide side) => GetNode<ALCard>($"{GetRootName(side)}/Retreat");
    public ALPhaseButton GetPhaseButton(ALBoardSide side) => GetNode<ALPhaseButton>($"{GetRootName(side)}/PhaseButton");
    public override async Task PlaceCardInBoardFromHand<T>(Player player, T cardToPlace)
    {
        ALCard selectedField = GetSelectedCard<ALCard>(player);
        if (selectedField is null)
        {
            GD.PrintErr($"[PlaceCardInBoardFromHand] Field cannot be found");
            return;
        }
        await base.PlaceCardInBoardFromHand(player, cardToPlace);
        selectedField.UpdateAttributes(cardToPlace.GetAttributes<ALCardDTO>());
        if (player.GetIsControllerPlayer())
        {
            ALCardDTO attrs = cardToPlace.GetAttributes<ALCardDTO>();
            string fieldPath = GetPathTo(selectedField).ToString();
            ALNetwork.Instance.SyncPlaceCard(attrs.id, Name, fieldPath);
        }
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
    public ALCard GetFlagship(ALBoardSide side)
    {
        if (side == ALBoardSide.Enemy)
        {
            if (enemyFlagshipCard is null) GD.PushWarning("[GetFlagship] This ALBoard doesn't have any enemy flagship assigned");
            return enemyFlagshipCard;
        }
        return GetFlagship();
    }
    public List<ALCard> GetUnitFields() => GetUnitFields(ALBoardSide.Player);
    public List<ALCard> GetUnitFields(ALBoardSide side)
    {
        Node3D unitsRoot = GetUnitsRoot(side);
        List<ALCard> fields = [];
        foreach (string fieldName in UnitFieldNames)
        {
            Node fieldNode = unitsRoot.GetNodeOrNull<Node>(fieldName) ?? throw new System.InvalidOperationException($"[GetUnitFields] Missing unit field {fieldName} under {unitsRoot.GetPath()}.");
            if (fieldNode is not ALCard field) throw new System.InvalidOperationException($"[GetUnitFields] Unit field {fieldName} is not an ALCard.");
            fields.Add(field);
        }
        return fields;
    }
    public List<ALCard> GetUnits() => GetUnitFields().FindAll(card => card.IsCardUnit());
    public List<ALCard> GetUnits(ALBoardSide side) => GetUnitFields(side).FindAll(card => card.IsCardUnit());

    public bool IsEnemyCard(Card card) => IsCardInSide(card, ALBoardSide.Enemy);
    public bool IsCardInSide(Card card, ALBoardSide side)
    {
        if (card is null)
        {
            throw new System.InvalidOperationException("[IsCardInSide] Card is required.");
        }
        string rootName = GetRootName(side);
        Node current = card;
        while (current is not null && current != this)
        {
            if (current.Name == rootName) return true;
            current = current.GetParent();
        }
        return false;
    }

    public Vector2I MapToOppositeSidePosition(Vector2I position)
    {
        Card sourceCard = FindCardInTree(position) ?? throw new System.InvalidOperationException($"[MapToOppositeSidePosition] No card found at {position} on board {Name}.");
        string sourcePath = GetPathTo(sourceCard).ToString();
        string mappedPath;
        if (sourcePath.StartsWith($"{PlayerRootName}/", System.StringComparison.Ordinal))
        {
            mappedPath = $"{EnemyRootName}/{sourcePath[(PlayerRootName.Length + 1)..]}";
        }
        else if (sourcePath.StartsWith($"{EnemyRootName}/", System.StringComparison.Ordinal))
        {
            mappedPath = $"{PlayerRootName}/{sourcePath[(EnemyRootName.Length + 1)..]}";
        }
        else
        {
            throw new System.InvalidOperationException($"[MapToOppositeSidePosition] Card path '{sourcePath}' is not under Player or Enemy roots.");
        }
        Node mappedNode = GetNodeOrNull(mappedPath);
        if (mappedNode is not Card mappedCard)
        {
            throw new System.InvalidOperationException($"[MapToOppositeSidePosition] Opposite card not found at '{mappedPath}'.");
        }
        return mappedCard.PositionInBoard;
    }

    static string GetRootName(ALBoardSide side) => side == ALBoardSide.Enemy ? EnemyRootName : PlayerRootName;
}

public enum ALBoardSide
{
    Player,
    Enemy
}
