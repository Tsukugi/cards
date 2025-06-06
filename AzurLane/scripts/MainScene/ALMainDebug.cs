using System.Threading.Tasks;
using Godot;

public partial class ALMainDebug(ALMain main)
{
    readonly ALMain main = main;

    public async Task AutoSyncStart()
    {
        await main.Wait(3f);
        if (!main.IsGameCreated) await TryJoinGame();
        else await AutoSyncStart();
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

    bool CheckConnection()
    {
        var playerCount = Network.Instance.GetPlayerCount();
        var connectionExists = playerCount > 1;
        GD.Print(playerCount);
        return connectionExists;
    }
}