using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALBasicAI(ALPlayer _player)
{
    readonly AsyncHandler asyncHandler = new(_player);
    readonly ALPlayer player = _player;

    int actionDelay = 250; // Miliseconds for every AI action

    public async Task SkipTurn()
    {
        await WaitUntilPhase(EALTurnPhase.Main);
        PlayNextPhase();
        await WaitUntilPhase(EALTurnPhase.Battle);
        PlayNextPhase();
    }

    public async Task SummonAndAttackFlagship()
    {
        await WaitUntilPhase(EALTurnPhase.Main);
        await MainPhasePlayExpensiveCard();
        PlayNextPhase();
        await WaitUntilPhase(EALTurnPhase.Battle);
        await BattlePhaseAttackFlagship();
        PlayNextPhase();
    }




    // Main Phase
    public async Task MainPhasePlayExpensiveCard()
    {
        List<ALCard> cards = GetPlayableCostCardsInHand();
        if (cards.Count > 0) PlayCardToBoard(cards);
        await Task.Delay(actionDelay);
    }

    public void PlayCardToBoard(List<ALCard> availableCardsInHand, bool front = true)
    {
        SelectAvailableEmptyField(front);
        player.Board.PlaceCardInBoardFromHand(FindMostExpensiveCard(availableCardsInHand));
    }

    // Battle Phase
    public async Task BattlePhaseAttackFlagship()
    {
        List<ALCard> activeUnits = player.GetActiveUnitsInBoard();
        if (activeUnits.Count > 0) return;

        for (int i = 0; i < activeUnits.Count; i++)
        {
            //Attacker
            await WaitUntilPlayState(EPlayState.Select);
            SelectAndTriggerCardUnit(activeUnits[i]);
            await Task.Delay(actionDelay);
            // Target
            await WaitUntilPlayState(EPlayState.SelectTarget);
            SelectAndTriggerCardUnit(player.EnemyBoard.GetFlagship());
            await Task.Delay(actionDelay);
        }
    }

    // Generic
    public void SelectAndTriggerCardUnit(ALCard card)
    {
        // Select attacker
        card.GetBoard().SelectCardField(player, card.PositionInBoard);
        player.TriggerSelectedCardInBoard(card.GetBoard());
    }

    public void SelectAvailableEmptyField(bool front = true)
    {
        ALCard? field = player.FindAvailableEmptyFieldInRow(front);
        if (field is null)
        {
            GD.PrintErr($"[SelectAvailableEmptyField] Cannot find any available field");
            return;
        }

        player.Board.SelectCardField(player, field.PositionInBoard);
    }

    public static ALCard FindMostExpensiveCard(List<ALCard> cards)
    {
        cards.Sort((cardA, cardB) => cardA.GetAttributes<ALCardDTO>().cost - cardB.GetAttributes<ALCardDTO>().cost);
        return cards[^1];
    }
    public List<ALCard> GetPlayableCostCardsInHand()
    {
        var availableCubes = player.GetActiveCubesInBoard().Count;
        return player.Hand.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetAttributes<ALCardDTO>().cost <= availableCubes);
    }

    public async Task WaitUntilPhase(EALTurnPhase phase)
    {
        await asyncHandler.AwaitForCheck(() => { }, () => player.GetCurrentPhase() == phase);
        await Task.Delay(actionDelay);
    }
    public async Task WaitUntilPlayState(EPlayState playState)
    {
        await asyncHandler.AwaitForCheck(() => { }, () => player.GetPlayState() == playState);
        await Task.Delay(actionDelay);
    }

    public void PlayNextPhase()
    {
        player.TriggerPhaseButton();
    }
}