using System.Threading.Tasks;
using Godot;

public partial class Network : Node
{
    public static Network Instance { get; private set; }
    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Godot.Collections.Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();
    public delegate void PlayerInputActionEvent(int peerId, InputAction inputAction);
    public delegate void PlayerCardEvent(int peerId, string cardId);
    public delegate void PlayerSelectCardEvent(int peerId, string boardType, bool isEnemyBoard, Vector2I position);
    public delegate void PlayerOrderEvent(int peerId, int order);
    public delegate void PlayerPlayStateEvent(int peerId, EPlayState state, string interactionState);
    public delegate void ALPlayerEvent(int peerId);

    public event PlayerInputActionEvent OnSendInputActionEvent;
    public event PlayerCardEvent OnDrawCardEvent;
    public event PlayerOrderEvent OnSendPlayOrderEvent;
    public event PlayerPlayStateEvent OnSendPlayStateEvent;
    public event PlayerSelectCardEvent OnSendSelectCardEvent;
    public event ALPlayerEvent OnTurnEndEvent;

    private const int Port = 7000;
    public const string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    private const int MaxConnections = 20;

    // This will contain player info for every player,
    // with the keys being each player's unique IDs.
    private Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> _players = [];

    // This is the local player info. This should be modified locally
    // before the connection is made. It will be passed to every other peer.
    // For example, the value of "name" can be set to something the player
    // entered in a UI scene.
    private Godot.Collections.Dictionary<string, string> _playerInfo = new()
    {
        { "Name", "PlayerName" },
    };


    public override void _Ready()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.ConnectionFailed += OnConnectionFail;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public Error JoinGame(string address = "")
    {
        if (string.IsNullOrEmpty(address))
        {
            address = DefaultServerIP;
        }

        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(address, Port);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    public Error CreateGame()
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, MaxConnections);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        _players[1] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, 1, _playerInfo);
        return Error.Ok;
    }

    public void SendInputAction(InputAction action) => Rpc(MethodName.OnSendInput, [(int)action]);
    public void RequestStartMatch(string path) => Rpc(MethodName.StartMatch, [path]);
    public void SendPlayOrder(int order) => Rpc(MethodName.OnSendPlayOrder, [order]);
    public void DrawCard(string cardId) => Rpc(MethodName.OnDrawCard, [cardId]);
    public void SendPlayState(int peerId, int state, string interactionState) => Rpc(MethodName.OnSendPlayState, [peerId, state, interactionState]);
    public void SendSelectCardField(int peerId, Board board, Vector2I position) => Rpc(MethodName.OnSendSelectCardField, [peerId, board.Name, board.GetIsEnemyBoard(), position]);

    public void CloseConnection()
    {
        Multiplayer.MultiplayerPeer?.Close();
        Multiplayer.MultiplayerPeer = null;
    }

    private void RemoveMultiplayerPeer()
    {
        CloseConnection();
        _players.Clear();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnSendPlayOrder(int order)
    {
        if (OnSendPlayOrderEvent is not null) OnSendPlayOrderEvent(Multiplayer.GetRemoteSenderId(), order);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnSendPlayState(int peerId, int state, string interactionState)
    {
        if (OnSendPlayStateEvent is not null) OnSendPlayStateEvent(peerId, (EPlayState)state, interactionState);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected virtual void OnDrawCard(string data)
    {
        if (OnDrawCardEvent is not null) OnDrawCardEvent(Multiplayer.GetRemoteSenderId(), data);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected virtual void OnSendSelectCardField(int peerId, string boardName, bool isEnemyBoard, Vector2I position)
    {
        if (OnSendSelectCardEvent is not null) OnSendSelectCardEvent(peerId, boardName, isEnemyBoard, position);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected virtual void OnSendInput(int inputAction)
    {
        if (OnSendInputActionEvent is not null) OnSendInputActionEvent(Multiplayer.GetRemoteSenderId(), (InputAction)inputAction);
    }
    public void SendTurnEnd() => Rpc(MethodName.OnSendTurnEnd, []);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    protected void OnSendTurnEnd()
    {
        GD.Print($"[OnSendTurnEnd] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------!");
        if (OnTurnEndEvent is not null) OnTurnEndEvent(Multiplayer.GetRemoteSenderId());
    }

    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.StartMatch, filePath);
    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartMatch(string gameScenePath)
    {
        if (!CheckConnection()) return;
        GetTree().ChangeSceneToFile(gameScenePath);
    }

    // Every peer will call this when they have loaded the game scene.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerLoaded()
    {
        GD.Print($"[{Multiplayer.GetUniqueId()} - PlayerLoaded]");
        if (Multiplayer.IsServer())
        {
            GD.Print($"[Server - PlayerLoaded] Players {_playerInfo.Count}");
        }
    }

    // When a peer connects, send them my player info.
    // This allows transfer of all desired data for each player, not only the unique ID.
    private void OnPlayerConnected(long id)
    {
        RpcId(id, MethodName.RegisterPlayer, _playerInfo);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Godot.Collections.Dictionary<string, string> newPlayerInfo)
    {
        int newPlayerId = Multiplayer.GetRemoteSenderId();
        _players[newPlayerId] = newPlayerInfo;
        foreach (var item in _players)
        {
            GD.Print($"[RegisterPlayer] Players: {item}");
        }
        EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerInfo);
    }

    private void OnPlayerDisconnected(long id)
    {
        _players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

    private void OnConnectOk()
    {
        int peerId = Multiplayer.GetUniqueId();
        _players[peerId] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, peerId, _playerInfo);
    }

    private void OnConnectionFail() => CloseConnection();

    private void OnServerDisconnected()
    {
        CloseConnection();
        _players.Clear();
        EmitSignal(SignalName.ServerDisconnected);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Ping()
    {
        int peerId = Multiplayer.GetRemoteSenderId();
        GD.Print($"[{Multiplayer.GetUniqueId()}.Test] {peerId} sent a ping");
    }

    public bool CheckConnection()
    {
        var res = Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
        if (!res) GD.PrintErr($"[CheckConnection] Connection has encountered a problem. Connection Status: {Multiplayer.MultiplayerPeer.GetConnectionStatus()}");
        return res;
    }
    public async Task PollPing()
    {
        while (true)
        {
            Rpc(MethodName.Ping);
            await this.Wait(1f);
        }
    }

    public int GetPlayerCount() => _playerInfo.Count;
}