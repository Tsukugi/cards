using System.Threading.Tasks;
using Godot;

public class ALBasicAI
{
    readonly int actionDelay = 500; // Miliseconds for every AI action
    readonly ALAIActions actions;
    readonly ALPlayer player;
    public ALBasicAI(ALPlayer _player)
    {
        player = _player;
        actions = new(_player, actionDelay);
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

    public async Task SummonAndAttackRandom()
    {
        await actions.WaitUntilPhase(EALTurnPhase.Main);
        await actions.MainPhasePlayExpensiveCard();
        await actions.PlayNextPhase();
        await actions.WaitUntilPhase(EALTurnPhase.Battle);
        await actions.BattlePhaseAttackRandom();
        await actions.PlayNextPhase();
    }

    public async Task SkipAttackGuards()
    {
        await actions.WaitUntilPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
        player.TriggerAction(InputAction.Cancel, player);
        await player.GetPlayerAsyncHandler().AwaitBefore(SkipAttackGuards, 0.5f);
    }
    public async Task StartTurn()
    {
        GD.Print($"[StartTurn] AI playing turn for player {player.Name}");
        _ = SkipAttackGuards();
        // TODO: Make a proper handler for proper AI
        await SummonAndAttackRandom();
    }
}