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
        // Select from hand
        ALCard card = FindMostExpensiveCard(availableCardsInHand);
        await player.OnCostPlayCardStartHandler(card);

        await WaitUntilPlayState(EPlayState.SelectTarget, ALInteractionState.SelectBoardFieldToPlaceCard);
        // Place card
        SelectAvailableBoardEmptyField(front);
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        await board.PlaceCardInBoardFromHand(player, card);
        GD.Print($"[PlayCardToBoard] {card.GetAttributes<ALCardDTO>().name}");
    }

    // Battle Phase
    public delegate ALCard OnTargetSelect();
    public async Task BattlePhaseAttack(OnTargetSelect targetSelect)
    {
        GD.Print($"[BattlePhaseAttack]");
        List<ALCard> activeUnits = player.GetActiveUnitsInBoard().FindAll(card => !card.GetEffectManager<ALEffectManager>().HasActiveEffect(ALCardStatusEffects.BattlefieldDelay)); // Also filter recently placed cards
        if (activeUnits.Count == 0) return;

        List<Func<Task>> operations = [];
        for (int i = 0; i < activeUnits.Count; i++)
        {
            var currentUnit = activeUnits[i];
            GD.Print($"[BattlePhaseAttack] Register attack for {currentUnit.Name}");
            operations.Add(async () =>
            {
                ALCard target = targetSelect();
                GD.Print($"[BattlePhaseAttack] target {target.Name}");
                await AttackProcess(currentUnit, target);
            });
        }
        GD.Print($"[BattlePhaseAttack] {operations.Count}");
        await AsyncHandler.RunAsyncFunctionsSequentially(operations);
    }
    public async Task BattlePhaseAttackFlagship()
    {
        GD.Print($"[BattlePhaseAttackFlagship]");
        await BattlePhaseAttack(() =>
        {
            ALBoard enemyBoard = player.GetEnemyPlayerBoard<ALBoard>();
            return enemyBoard.GetFlagship();
        });
    }
    public async Task BattlePhaseAttackRandom()
    {
        GD.Print($"[BattlePhaseAttackRandom]");
        await BattlePhaseAttack(() =>
        {
            List<ALCard> enemyUnits = player.GetEnemyPlayerBoard<ALBoard>().GetUnits(); // Find all placed cards
            var selected = enemyUnits.GetRandomFromList();
            GD.Print($"[BattlePhaseAttackRandom] {selected.GetAttributes<CardDTO>().name}");
            return selected;
        });
    }
    public async Task AttackProcess(ALCard attacker, ALCard target)
    {
        //Attacker
        GD.Print($"[AttackProcess] Start");
        await WaitUntilPlayState(EPlayState.SelectTarget, ALInteractionState.SelectAttackerUnit);
        SelectAndTriggerCardUnit(attacker);
        await Task.Delay(actionDelay);
        GD.Print($"[AttackProcess] Attacker {attacker.Name}");
        // Target
        await WaitUntilPlayState(EPlayState.SelectTarget, ALInteractionState.SelectAttackTarget);
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
        cards.FindAll(card => card.IsCardUnit()).Sort((cardA, cardB) => cardA.GetAttributes<ALCardDTO>().cost - cardB.GetAttributes<ALCardDTO>().cost);
        var expensiveCard = cards[^1];
        GD.Print($"[FindMostExpensiveCard] {expensiveCard.GetAttributes<ALCardDTO>().name}");
        return expensiveCard;
    }
    public List<ALCard> GetPlayableCostCardsInHand()
    {
        var availableCubes = player.GetActiveCubesInBoard().Count;
        ALHand hand = player.GetPlayerHand<ALHand>();
        var cards = hand.TryGetAllChildOfType<ALCard>().FindAll(card =>
            {
                var attrs = card.GetAttributes<ALCardDTO>();
                return attrs.cost <= availableCubes && attrs.cardType == ALCardType.Ship;
            });

        GD.Print($"[GetPlayableCostCardsInHand] Card list {cards.Count}");
        return cards;
    }

    public async Task WaitUntilPhase(EALTurnPhase phase, float timeout = -1)
    {
        await asyncHandler.AwaitForCheck(
            () =>
            {
                // GD.Print($"[WaitUntilPhase] Success {player.Phase.GetCurrentPhase()} {phase}");
            },
            () =>
            {
                // GD.Print($"[WaitUntilPhase] {asyncHandler} {player.Phase.GetCurrentPhase()} {phase}");
                return player.Phase.GetCurrentPhase() == phase;
            },
             timeout);
        await Task.Delay(actionDelay);
    }
    public async Task WaitUntilPlayState(EPlayState playState, string interactionState = null, float timeout = -1)
    {
        var checkInteractionState = interactionState is null ? ALInteractionState.None : interactionState;
        await asyncHandler.AwaitForCheck(
            () =>
            {
                //  GD.Print($"[WaitUntilPlayState] Success : {player.GetPlayState()} - {playState} : {player.GetInteractionState()} - {checkInteractionState}");
            },
            () =>
            {
                //  GD.Print($"[WaitUntilPlayState] {asyncHandler} : {player.GetPlayState()} - {playState} : {player.GetInteractionState()} - {checkInteractionState}");
                return player.GetInputPlayState() == playState && player.GetInteractionState() == checkInteractionState;
            },
            timeout);
        await Task.Delay(actionDelay);
    }

    public async Task PlayNextPhase()
    {
        GD.Print($"[PlayNextPhase]");
        await Task.Delay(actionDelay);
        player.TriggerPhaseButton(player);
    }
}