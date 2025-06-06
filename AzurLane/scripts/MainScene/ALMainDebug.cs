using System.Threading.Tasks;

public partial class ALMainDebug(ALMain main)
{
    readonly ALMain main = main;

    public async void AutoSyncStart()
    {
        await main.Wait(3f);
        if (!main.IsGameCreated) await TryJoinGame();
    }
    public async Task TryCreateGame()
    {
        await main.Wait(1f);
        main.OnCreateGamePressed();
    }

    public async Task TryJoinGame()
    {
        await main.Wait(1f);
        main.OnJoinPressed();
    }
    public async Task TryStartGame()
    {
        await main.Wait(1f);
        main.OnStartPressed();
    }
}