using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALAIActions
{
    readonly ALPlayer player;
    readonly int actionDelay; // Miliseconds for every AI action
    readonly AsyncHandler asyncHandler;
    public ALAIActions(ALPlayer _player, int _actionDelay)
    {
        player = _player;
        actionDelay = _actionDelay;
        asyncHandler = _player.GetPlayerAsyncHandler();
    }

    // Main Phase
    public async Task MainPhasePlayExpensiveCard()
    {
        GD.Print($"[MainPhasePlayExpensiveCard]");
        List<ALCard> cards = GetPlayableCostCardsInHand();
        if (cards.Count > 0)
        {
            await PlayCardToBoard(cards);
        }
        await Task.Delay(actionDelay);
    }

    public async Task PlayCardToBoard(List<ALCard> availableCardsInHand, bool front = true)
    {
        GD.Print($"[PlayCardToBoard]");
        // Select from hand
        var card = FindMostExpensiveCard(availableCardsInHand);
        player.OnCostPlayCardStartHandler(card);

        await WaitUntilPlayState(EPlayState.PlaceCard);
        // Place card
        SelectAvailableBoardEmptyField(front);
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        board.PlaceCardInBoardFromHand(player, card);
    }

    // Battle Phase
    public async Task BattlePhaseAttackFlagship()
    {
        GD.Print($"[BattlePhaseAttackFlagship]");
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
    public async Task AttackProcess(ALCard attacker, ALCard target)
    {
        //Attacker
        GD.Print($"[AttackProcess] Start");
        await WaitUntilPlayState(EPlayState.Select);
        SelectAndTriggerCardUnit(attacker);
        await Task.Delay(actionDelay);
        GD.Print($"[AttackProcess] Attacker {attacker.Name}");
        // Target
        await WaitUntilPlayState(EPlayState.SelectTarget);
        GD.Print($"[AttackProcess] Attacking {target}");
        SelectAndTriggerCardUnit(target);
        await Task.Delay(actionDelay);
        GD.Print($"[AttackProcess] Attacked {target}");
    }

    // Generic
    public void SelectAndTriggerCardUnit(ALCard card)
    {
        GD.Print($"[SelectAndTriggerCardUnit]");
        // Select attacker
        card.GetBoard().SelectCardField(player, card.PositionInBoard);
        player.SelectAndTriggerCard(card);
        GD.Print($"[SelectAndTriggerCardUnit] {player.Name} selects and triggers {card.Name}");
    }

    public void SelectAvailableBoardEmptyField(bool front = true)
    {
        GD.Print($"[SelectAvailableBoardEmptyField]");
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
        GD.Print($"[FindMostExpensiveCard]");
        cards.Sort((cardA, cardB) => cardA.GetAttributes<ALCardDTO>().cost - cardB.GetAttributes<ALCardDTO>().cost);
        return cards[^1];
    }
    public List<ALCard> GetPlayableCostCardsInHand()
    {
        GD.Print($"[GetPlayableCostCardsInHand]");
        var availableCubes = player.GetActiveCubesInBoard().Count;
        ALHand hand = player.GetPlayerHand<ALHand>();
        return hand.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetAttributes<ALCardDTO>().cost <= availableCubes);
    }

    public async Task WaitUntilPhase(EALTurnPhase phase, float timeout = -1)
    {
        await asyncHandler.AwaitForCheck(
            () =>
            {
                GD.Print($"[WaitUntilPhase] Success {player.Phase.GetCurrentPhase()} {phase}");
            },
            () =>
            {
                // GD.Print($"[WaitUntilPhase] {asyncHandler} {player.Phase.GetCurrentPhase()} {phase}");
                return player.Phase.GetCurrentPhase() == phase;
            },
             timeout);
        await Task.Delay(actionDelay);
    }
    public async Task WaitUntilPlayState(EPlayState playState, float timeout = -1)
    {
        await asyncHandler.AwaitForCheck(
            () =>
            {
                GD.Print($"[WaitUntilPlayState] Success {player.GetPlayState()} {playState}");
            },
            () =>
            {
                // GD.Print($"[WaitUntilPlayState] {asyncHandler} {player.GetPlayState()} {playState}");
                return player.GetPlayState() == playState;
            },
            timeout);
        await Task.Delay(actionDelay);
    }

    public async Task PlayNextPhase()
    {
        await Task.Delay(actionDelay);
        player.TriggerPhaseButton();
    }
}