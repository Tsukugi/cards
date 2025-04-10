using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALBasicAI(ALPlayer _player)
{
    readonly AsyncHandler asyncHandler = new(_player);
    readonly ALPlayer player = _player;

    readonly int actionDelay = 500; // Miliseconds for every AI action

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
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        board.PlaceCardInBoardFromHand(card);
    }

    // Battle Phase
    public async Task BattlePhaseAttackFlagship()
    {
        List<ALCard> activeUnits = player.GetActiveUnitsInBoard();
        if (activeUnits.Count == 0) return;
        ALBoard enemyBoard = player.GetEnemyPlayerBoard<ALBoard>();
        ALCard enemyFlagship = enemyBoard.GetFlagship();

        List<Func<Task>> operations = [];

        for (int i = 0; i < activeUnits.Count; i++)
        {
            var currentUnit = activeUnits[i];
            GD.Print($"[BattlePhaseAttackFlagship] Register attack for {currentUnit.Name}");
            operations.Add(() => AttackProcess(currentUnit, enemyFlagship));
        }
        GD.Print($"[BattlePhaseAttackFlagship] {operations.Count}");
        await AsyncHandler.RunAsyncFunctionsSequentially(operations);
    }
    async Task AttackProcess(ALCard attacker, ALCard target)
    {
        //Attacker
        GD.Print($"[BattlePhaseAttackFlagship] Start");
        await WaitUntilPlayState(EPlayState.Select);
        SelectAndTriggerCardUnit(attacker);
        await Task.Delay(actionDelay);
        GD.Print($"[BattlePhaseAttackFlagship] Attacker {attacker.Name}");
        // Target
        await WaitUntilPlayState(EPlayState.SelectTarget);
        GD.Print($"[BattlePhaseAttackFlagship] Attacking {target}");
        SelectAndTriggerCardUnit(target);
        await Task.Delay(actionDelay);
        GD.Print($"[BattlePhaseAttackFlagship] Attacked {target}");
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

        ALBoard board = player.GetPlayerBoard<ALBoard>();
        board.SelectCardField(player, field.PositionInBoard);
    }

    public static ALCard FindMostExpensiveCard(List<ALCard> cards)
    {
        cards.Sort((cardA, cardB) => cardA.GetAttributes<ALCardDTO>().cost - cardB.GetAttributes<ALCardDTO>().cost);
        return cards[^1];
    }
    public List<ALCard> GetPlayableCostCardsInHand()
    {
        var availableCubes = player.GetActiveCubesInBoard().Count;
        ALHand hand = player.GetPlayerHand<ALHand>();
        return hand.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetAttributes<ALCardDTO>().cost <= availableCubes);
    }

    public async Task WaitUntilPhase(EALTurnPhase phase)
    {
        await asyncHandler.AwaitForCheck(() =>
            GD.Print($"[WaitUntilPhase] {player.GetCurrentPhase()} {phase}"),
            () => player.GetCurrentPhase() == phase);
        await Task.Delay(actionDelay);
    }
    public async Task WaitUntilPlayState(EPlayState playState)
    {
        await asyncHandler.AwaitForCheck(() =>
            GD.Print($"[WaitUntilPlayState] Success {player.GetPlayState()} {playState}"),
            () => player.GetPlayState() == playState);
        await Task.Delay(actionDelay);
    }

    public void PlayNextPhase()
    {
        player.TriggerPhaseButton();
    }
}