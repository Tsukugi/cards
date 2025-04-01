using System;
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
        if (cards.Count > 0)
        {
            await PlayCardToBoard(cards);
        }
        await Task.Delay(actionDelay);
    }

    public async Task PlayCardToBoard(List<ALCard> availableCardsInHand, bool front = true)
    {
        var card = FindMostExpensiveCard(availableCardsInHand);
        player.OnCostPlayCardStartHandler(card);
        await WaitUntilPlayState(EPlayState.PlaceCard);
        SelectAvailableBoardEmptyField(front);
        player.Board.PlaceCardInBoardFromHand(card);
    }

    // Battle Phase
    public async Task BattlePhaseAttackFlagship()
    {
        List<ALCard> activeUnits = player.GetActiveUnitsInBoard();
        if (activeUnits.Count == 0) return;

        List<Task> ops = new();

        for (int i = 0; i < activeUnits.Count; i++)
        {
            ops.Add(Task.FromResult(() => AttackFlagshipProcess(activeUnits[i])));
        }


        //asyncHandler.RunSequentiallyAsync<List<Task>>(new Func<Task<List<Task>>>(() => ops));
    }
    async void AttackFlagshipProcess(ALCard attacker)
    {
        //Attacker
        await WaitUntilPlayState(EPlayState.Select);
        SelectAndTriggerCardUnit(attacker);
        await Task.Delay(actionDelay);
        GD.Print($"[BattlePhaseAttackFlagship] Attacker {attacker.Name}");
        // Target
        await WaitUntilPlayState(EPlayState.SelectTarget);
        GD.Print($"[BattlePhaseAttackFlagship] {player.Name}");
        SelectAndTriggerCardUnit(player.EnemyBoard.GetFlagship());
        await Task.Delay(actionDelay);
        GD.Print($"[BattlePhaseAttackFlagship] Attacked {player.EnemyBoard.GetFlagship()}");
    }

    // Generic
    public void SelectAndTriggerCardUnit(ALCard card)
    {
        // Select attacker
        card.GetBoard().SelectCardField(player, card.PositionInBoard);
        player.TriggerSelectedCardInBoard(card.GetBoard());
        GD.Print($"[SelectAndTriggerCardUnit] {player.Name} selects and triggers {card.Name}");
    }

    public void SelectAvailableBoardEmptyField(bool front = true)
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
        await asyncHandler.AwaitForCheck(() => { }, () =>
        {
            GD.Print($"[WaitUntilPhase] {player.GetCurrentPhase()} {phase}");
            return player.GetCurrentPhase() == phase;
        });
        await Task.Delay(actionDelay);
    }
    public async Task WaitUntilPlayState(EPlayState playState)
    {
        await asyncHandler.AwaitForCheck(() => { }, () =>
        {
            GD.Print($"[WaitUntilPlayState] {player.GetPlayState()} {playState}");
            return player.GetPlayState() == playState;
        });
        await Task.Delay(actionDelay);
    }

    public void PlayNextPhase()
    {
        player.TriggerPhaseButton();
    }
}