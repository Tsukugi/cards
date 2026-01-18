using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALHand : PlayerHand
{
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    public async Task<ALCard> AddCardToHand(ALCardDTO attributes)
    {
        ALCard newCard = cardTemplate.Instantiate<ALCard>();

        int numCardsInHand = GetCardsInHand().Count;
        Vector2I ownerSelectedPosition = GetOwnerSelectedCardPosition();
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + ownerSelectedPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateAttributes(attributes);
        await newCard.TryToTriggerCardEffect(CardEffectTrigger.OnVisible);
        RepositionHandCards();
        return newCard;
    }
    protected new List<ALCard> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<ALCard>();
    }
}
