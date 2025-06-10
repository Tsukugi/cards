using Godot;
using Newtonsoft.Json;

public partial class ALNetwork : Network
{
    public static new ALNetwork Instance { get; private set; }
    public delegate void ALPlayerMatchPhaseEvent(int peerId, int matchPhase);
    public delegate void ALPlayerSyncCardEvent(int peerId, string cardId);
    public delegate void ALPlayerDrawEvent(int peerId, string cardId, ALDrawType drawType);
    public delegate void ALPlayerSyncPlaceCardEvent(int peerId, string cardId, string boardName, Vector2I position);
    public event ALPlayerMatchPhaseEvent OnSendMatchPhaseEvent;
    public new event ALPlayerDrawEvent OnDrawCardEvent;
    public event ALPlayerSyncCardEvent OnSyncFlagshipEvent;
    public event ALPlayerSyncPlaceCardEvent OnSyncPlaceCard;
    public event ALPlayerSyncPlaceCardEvent OnSyncPlaceCardGuard;

    ALGameMatchManager matchManager;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }
    public void RegisterMatchPlayer() => Rpc(MethodName.OnRegisterMatchPlayer, []);
    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.AfterStartMatch, filePath);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnRegisterMatchPlayer()
    {
        int playerId = Multiplayer.GetRemoteSenderId();
        matchManager.GetEnemyPlayer().MultiplayerId = playerId;
        GD.Print($"[OnRegisterMatchPlayer] {Multiplayer.GetUniqueId()}: {playerId}");
    }
    public void SendDeckSet(string userPlayerDeckSetId) => Rpc(MethodName.OnSendDeckSet, [userPlayerDeckSetId]);
    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.AfterStartMatch, filePath);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnSendDeckSet(string deckSetId)
    {
        int playerId = Multiplayer.GetRemoteSenderId();
        matchManager.OnEnemyDeckSetProvided(deckSetId);
        GD.Print($"[OnSendDeckSet]{Multiplayer.GetUniqueId()} {playerId} - {deckSetId}");
    }

    public void ALDrawCard(string cardId, ALDrawType drawType) => Rpc(MethodName.OnALDrawCard, [cardId, (int)drawType]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    protected void OnALDrawCard(string data, ALDrawType drawType)
    {
        GD.Print($"[OnALDrawCard] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {data}");
        if (OnDrawCardEvent is not null) OnDrawCardEvent(Multiplayer.GetRemoteSenderId(), data, drawType);
    }

    public void SyncFlagship(string cardId) => Rpc(MethodName.OnSyncFlagship, [cardId]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    protected void OnSyncFlagship(string cardId)
    {
        GD.Print($"[OnSyncFlagship] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {cardId}");
        if (OnSyncFlagshipEvent is not null) OnSyncFlagshipEvent(Multiplayer.GetRemoteSenderId(), cardId);
    }
    public void SendMatchPhase(int matchPhase) => Rpc(MethodName.OnSendMatchPhase, [matchPhase]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    protected void OnSendMatchPhase(int matchPhase)
    {
        GD.Print($"[OnSyncFlagship] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {matchPhase}");
        if (OnSendMatchPhaseEvent is not null) OnSendMatchPhaseEvent(Multiplayer.GetRemoteSenderId(), matchPhase);
    }


    public void OnMatchStart()
    {
        matchManager = GetNode<ALGameMatchManager>("/root/main");
        RegisterMatchPlayer();
    }
}

public enum ALDrawType
{
    Cube,
    Deck,
    Durability
}