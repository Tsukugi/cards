using System.Collections.Generic;
using Godot;

public partial class ALHand : PlayerHand
{
    public new ALCard GetSelectedCard() => SelectedCard is ALCard card ? card : null;
    protected new PackedScene cardTemplate = GD.Load<PackedScene>("AzurLane/AzurLaneCard.tscn");
    public void AddCardToHand(ALCardDTO cardDTO)
    {
        ALCard newCard = cardTemplate.Instantiate<ALCard>();

        int numCardsInHand = GetCardsInHand().Count;
        AddChild(newCard);
        newCard.Position = new Vector3((numCardsInHand + SelectCardPosition.X) * -numCardsInHand, 0, 0); // Card size
        newCard.RotationDegrees = new Vector3(0, 0, 1); // To add the card stacking
        newCard.PositionInBoard = new Vector2I(numCardsInHand, 0);
        newCard.UpdateAttributes(cardDTO);
        RepositionHandCards();
    }
    protected new List<ALCard> GetCardsInHand()
    {
        return this.TryGetAllChildOfType<ALCard>();
    }
}