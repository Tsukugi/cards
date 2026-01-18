using System;
using System.Threading.Tasks;
using Godot;

public partial class ALMainDebug
{
    readonly IALMainAutoMatchHost host;
    readonly IAutoMatchNetwork network;

    public ALMainDebug(IALMainAutoMatchHost host)
        : this(host, new AutoMatchNetworkAdapter())
    {
    }

    internal ALMainDebug(IALMainAutoMatchHost host, IAutoMatchNetwork network)
    {
        this.host = host ?? throw new InvalidOperationException("[ALMainDebug] Host is required.");
        this.network = network ?? throw new InvalidOperationException("[ALMainDebug] Network is required.");
    }

    public async void AutoSyncStart()
    {
        bool autoHost = host.IsAutoHostMatchEnabled();
        bool autoJoin = host.IsAutoJoinMatchEnabled();
        if (!autoHost && !autoJoin) return;
        if (autoHost && autoJoin)
        {
            throw new InvalidOperationException("[ALMainDebug.AutoSyncStart] Only one auto match mode can be enabled.");
        }

        if (autoHost)
        {
            await AutoHostMatch();
            return;
        }

        await AutoJoinMatch();
    }

    public async Task AutoHostMatch()
    {
        host.SetAutoMatchInProgress(true);
        try
        {
            await host.Wait(1f);
            host.OpenHostLobby();
            await host.Wait(1f);
            var createResult = host.TryConfirmJoinOrHost(out string createMessage);
            if (createResult != Error.Ok)
            {
                throw new InvalidOperationException($"[ALMainDebug.AutoHostMatch] {createMessage}");
            }

            await WaitForPlayerCount(2);
            if (network.IsServer)
            {
                await host.Wait(1f);
                host.StartMatch();
            }
        }
        finally
        {
            host.SetAutoMatchInProgress(false);
        }
    }

    public async Task AutoJoinMatch()
    {
        host.SetAutoMatchInProgress(true);
        try
        {
            var settings = host.GetJoinConnectionSettings();
            host.UpdateJoinInputs(settings.Address, settings.Port);

            await host.Wait(1f);
            host.OpenJoinLobby();
            await host.Wait(2f);
            var joinResult = host.TryConfirmJoinOrHost(out string joinMessage);
            if (joinResult != Error.Ok)
            {
                throw new InvalidOperationException($"[ALMainDebug.AutoJoinMatch] {joinMessage}");
            }
        }
        finally
        {
            host.SetAutoMatchInProgress(false);
        }
    }

    public async Task TryCreateGame()
    {
        await host.Wait(1f);
        host.OpenHostLobby();
    }

    public async Task TryJoinGame()
    {
        await host.Wait(1f);
        host.OpenJoinLobby();
    }

    public async Task TryStartGame()
    {
        await host.Wait(1f);
        host.StartMatch();
    }

    async Task WaitForPlayerCount(int requiredPlayers)
    {
        while (network.GetPlayerCount() < requiredPlayers)
        {
            await host.Wait(0.2f);
        }
    }
}

public interface IALMainAutoMatchHost
{
    bool IsGameCreated { get; }
    Task Wait(float seconds);
    void OpenJoinLobby();
    void OpenHostLobby();
    void UpdateJoinInputs(string address, int port);
    ALConnectionSettings GetJoinConnectionSettings();
    Error TryConfirmJoinOrHost(out string message);
    void StartMatch();
    bool IsAutoHostMatchEnabled();
    bool IsAutoJoinMatchEnabled();
    void SetAutoMatchInProgress(bool enabled);
}

public interface IAutoMatchNetwork
{
    int GetPlayerCount();
    bool IsServer { get; }
}

public sealed class AutoMatchNetworkAdapter : IAutoMatchNetwork
{
    public int GetPlayerCount()
    {
        if (Network.Instance is null)
        {
            throw new InvalidOperationException("[AutoMatchNetworkAdapter.GetPlayerCount] Network instance is required.");
        }
        return Network.Instance.GetPlayerCount();
    }

    public bool IsServer
    {
        get
        {
            if (Network.Instance is null)
            {
                throw new InvalidOperationException("[AutoMatchNetworkAdapter.IsServer] Network instance is required.");
            }
            return Network.Instance.Multiplayer.IsServer();
        }
    }
}
