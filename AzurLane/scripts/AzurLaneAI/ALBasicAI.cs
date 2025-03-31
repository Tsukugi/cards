using System.Threading.Tasks;

public class ALBasicAI(ALPlayer _player)
{
    readonly AsyncHandler asyncHandler = new(_player);
    readonly ALPlayer player = _player;

    int actionDelay = 250; // Miliseconds for every AI action

    public async Task SkipTurn()
    {
        await WaitUntilPhase(EALTurnPhase.Main);
        await Task.Delay(actionDelay);
        PlayNextPhase();
        await WaitUntilPhase(EALTurnPhase.Battle);
        await Task.Delay(actionDelay);
        PlayNextPhase();
    }

    // Generic
    public async Task WaitUntilPhase(EALTurnPhase phase)
    {
        await asyncHandler.AwaitForCheck(() => { }, () => player.GetCurrentPhase() == phase);
    }

    public void PlayNextPhase()
    {
        player.TriggerPhaseButton();
    }
}