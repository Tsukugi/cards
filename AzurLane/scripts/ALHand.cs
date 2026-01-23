using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALHand : PlayerHand
{
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    public async Task<ALCard> AddCardToHand(ALCardDTO attributes)
    {
        ALCard newCard = CreateHandCard(attributes);
        await newCard.TryToTriggerCardEffect(CardEffectTrigger.OnVisible);
        RepositionHandCards();
        return newCard;
    }

    public ALCard AddEnemyCardToHand(ALCardDTO attributes)
    {
        ALCard newCard = CreateHandCard(attributes);
        newCard.SetIsFaceDown(true);
        RepositionHandCards();
        return newCard;
    }

    public void RemoveEnemyCardFromHand(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[RemoveEnemyCardFromHand] Card id is required.");
        }
        ALCard cardToRemove = GetCardsInHand().Find(card => card.GetAttributes<ALCardDTO>().id == cardId) ?? throw new System.InvalidOperationException($"[RemoveEnemyCardFromHand] Card id not found in enemy hand: {cardId}");
        RemoveChild(cardToRemove);
        RepositionHandCards();
    }

    ALCard CreateHandCard(ALCardDTO attributes)
    {
        ALCard newCard = cardTemplate.Instantiate<ALCard>();

        int numCardsInHand = GetCardsInHand().Count;
        Vector2I ownerSelectedPosition = GetOwnerSelectedCardPosition();
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + ownerSelectedPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateAttributes(attributes);
        return newCard;
    }
    protected new List<ALCard> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<ALCard>();
    }
}
