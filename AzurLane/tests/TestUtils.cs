using System;
using System.Threading.Tasks;

public static class TestUtils
{
    public static async Task AwaitMatchReady(ALPlayer player, ALGameMatchManager matchManager)
    {
        if (player is null)
        {
            throw new InvalidOperationException("[TestUtils.AwaitMatchReady] Player is required.");
        }
        if (matchManager is null)
        {
            throw new InvalidOperationException("[TestUtils.AwaitMatchReady] Match manager is required.");
        }
        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetEnemyPeerId() != 0,
            -1);
        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetMatchPhase() == EALTurnPhase.Main,
            -1);
    }
}
