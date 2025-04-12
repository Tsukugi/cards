using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALBasicAI
{
    readonly AsyncHandler asyncHandler;

    readonly int actionDelay = 500; // Miliseconds for every AI action

    readonly ALAIActions actions;

    public ALBasicAI(ALPlayer _player)
    {
        asyncHandler = new(_player);
        actions = new(_player, actionDelay, asyncHandler);
    }

    public async Task SkipTurn()
    {
        await actions.WaitUntilPhase(EALTurnPhase.Main);
        await actions.PlayNextPhase();
        await actions.WaitUntilPhase(EALTurnPhase.Battle);
        await actions.PlayNextPhase();
    }

    public async Task SummonAndAttackFlagship()
    {
        await actions.WaitUntilPhase(EALTurnPhase.Main);
        await actions.MainPhasePlayExpensiveCard();
        await actions.PlayNextPhase();
        await actions.WaitUntilPhase(EALTurnPhase.Battle);
        await actions.BattlePhaseAttackFlagship();
        await actions.PlayNextPhase();
    }

    public async Task StartTurn()
    {
        // TODO: Make a proper handler for proper AI
        await SummonAndAttackFlagship();
    }
}