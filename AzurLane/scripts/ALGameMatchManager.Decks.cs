using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALGameMatchManager
{
    async Task AssignDeckSet()
    {
        var userPlayerDeckSetId = Multiplayer.IsServer() ? "SD03" : "SD02";
        userPlayer.AssignDeck(BuildDeckSet(userPlayerDeckSetId));
        ALNetwork.Instance.SendDeckSet(userPlayerDeckSetId);
        await userPlayer.GetAsyncHandler().AwaitForCheck(null, userPlayer.HasValidDeck, -1);
        await userPlayer.GetAsyncHandler().AwaitForCheck(null, userPlayer.HasValidEnemyDeck, -1);
    }

    public ALDeckSet BuildDeckSet(string deckId)
    {
        ALDeckDTO deckDefinition = database.decks[deckId];
        ALDeckSet deckToUse = new()
        {
            name = deckDefinition.name,
            flagship = database.cards[deckDefinition.flagship],
            deck = TransformCardDictToList(deckDefinition.cards, database.cards).Shuffle(),
            cubeDeck = TransformCardDictToList(deckDefinition.cubes, database.cards).Shuffle()
        };
        return deckToUse;
    }

    public static List<ALCardDTO> TransformCardDictToList(Dictionary<string, int> deckDict, Dictionary<string, ALCardDTO> cardsDatabase)
    {
        List<ALCardDTO> cardList = [];
        foreach (var card in deckDict)
        {
            for (int i = 0; i < card.Value; i++)
            {
                cardList.Add(cardsDatabase[card.Key]);
            }
        }
        return cardList;
    }

    public void OnEnemyDeckSetProvided(string enemyDeckId)
    {
        GD.Print($"[OnEnemyDeckSetProvided] {enemyDeckId}");
        userPlayer.AssignEnemyDeck(BuildDeckSet(enemyDeckId));
    }
}
