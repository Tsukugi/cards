using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALBasicAI
{
    readonly AsyncHandler asyncHandler;
    readonly ALPlayer player;

    readonly int actionDelay = 500; // Miliseconds for every AI action

    readonly ALAIActions actions;

    public ALBasicAI(ALPlayer _player)
    {
        asyncHandler = new(_player);
        player = _player;
        actions = new(_player, actionDelay, asyncHandler);
    }

    public async Task SkipTurn()
    {
        await actions.WaitUntilPhase(EALTurnPhase.Main);
        actions.PlayNextPhase();
        await actions.WaitUntilPhase(EALTurnPhase.Battle);
        actions.PlayNextPhase();
    }

    public async Task SummonAndAttackFlagship()
    {
        await actions.WaitUntilPhase(EALTurnPhase.Main);
        await actions.MainPhasePlayExpensiveCard();
        actions.PlayNextPhase();
        await actions.WaitUntilPhase(EALTurnPhase.Battle);
        await actions.BattlePhaseAttackFlagship();
        actions.PlayNextPhase();
    }

    public async Task StartTurn()
    {
        // TODO: Make a proper handler for proper AI
        await SummonAndAttackFlagship();
    }
}