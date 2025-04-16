using Godot;

public static class ALExtensions
{
    public static ALCard CastToALCard(this Card card)
    {
        if (card is null) return null;
        if (card is not ALCard alCard)
        {
            GD.PushError($"[CastToALCard] Cannot play a card not belonging to AzurLane TCG, {card.Name} is {card.GetType()} ");
            return null;
        }
        return alCard;
    }
}