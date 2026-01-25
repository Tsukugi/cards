using Godot;
using Newtonsoft.Json;

public partial class ALNetwork : Network
{
    public static new ALNetwork Instance { get; private set; }
    public delegate void ALPlayerMatchPhaseEvent(int peerId, int matchPhase);
    public delegate void ALPlayerSyncCardEvent(int peerId, string cardId);
    public delegate void ALPlayerDrawEvent(int peerId, string cardId, ALDrawType drawType);
    public delegate void ALPlayerSyncPlaceCardEvent(int peerId, string cardId, string boardName, string fieldPath);
    public delegate void ALPlayerGuardPhaseStartEvent(int peerId, string attackerCardId, string attackedCardId);
    public delegate void ALPlayerGuardPhaseEndEvent(int peerId);
    public delegate void ALPlayerGuardProvidedEvent(int peerId, string guardCardId);
    public delegate void ALPlayerBattleResolutionEvent(int peerId, string attackerCardId, string attackedCardId, bool isAttackSuccessful);
    public delegate void ALPlayerCardActiveStateEvent(int peerId, string cardId, bool isActive);
    public event ALPlayerMatchPhaseEvent OnSendMatchPhaseEvent;
    public new event ALPlayerDrawEvent OnDrawCardEvent;
    public event ALPlayerSyncCardEvent OnSyncFlagshipEvent;
    public event ALPlayerSyncCardEvent OnSyncDurabilityDamageEvent;
    public event ALPlayerSyncPlaceCardEvent OnSyncPlaceCard;
    public event ALPlayerSyncPlaceCardEvent OnSyncPlaceCardGuard;
    public event ALPlayerGuardPhaseStartEvent OnGuardPhaseStartEvent;
    public event ALPlayerGuardPhaseEndEvent OnGuardPhaseEndEvent;
    public event ALPlayerGuardProvidedEvent OnGuardProvidedEvent;
    public event ALPlayerBattleResolutionEvent OnBattleResolutionEvent;
    public event ALPlayerCardActiveStateEvent OnCardActiveStateEvent;

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
        matchManager.RegisterEnemyPeer(playerId);
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
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnALDrawCard(string data, ALDrawType drawType)
    {
        GD.Print($"[OnALDrawCard] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {data}");
        if (OnDrawCardEvent is not null) OnDrawCardEvent(Multiplayer.GetRemoteSenderId(), data, drawType);
    }

    public void SyncFlagship(string cardId) => Rpc(MethodName.OnSyncFlagship, [cardId]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncFlagship(string cardId)
    {
        GD.Print($"[OnSyncFlagship] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {cardId}");
        if (OnSyncFlagshipEvent is not null) OnSyncFlagshipEvent(Multiplayer.GetRemoteSenderId(), cardId);
    }

    public void SyncDurabilityDamage(string cardId) => Rpc(MethodName.OnSyncDurabilityDamage, [cardId]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncDurabilityDamage(string cardId)
    {
        GD.Print($"[OnSyncDurabilityDamage] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {cardId}");
        if (OnSyncDurabilityDamageEvent is not null) OnSyncDurabilityDamageEvent(Multiplayer.GetRemoteSenderId(), cardId);
    }
    public void SendMatchPhase(int matchPhase) => Rpc(MethodName.OnSendMatchPhase, [matchPhase]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSendMatchPhase(int matchPhase)
    {
        GD.Print($"[OnSyncFlagship] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -------------> {matchPhase}");
        if (OnSendMatchPhaseEvent is not null) OnSendMatchPhaseEvent(Multiplayer.GetRemoteSenderId(), matchPhase);
    }

    public void SyncPlaceCard(string cardId, string boardName, string fieldPath) => Rpc(MethodName.OnSyncPlaceCardRpc, [cardId, boardName, fieldPath]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncPlaceCardRpc(string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[OnSyncPlaceCard] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {cardId} {boardName} {fieldPath}");
        if (OnSyncPlaceCard is not null) OnSyncPlaceCard(Multiplayer.GetRemoteSenderId(), cardId, boardName, fieldPath);
    }

    public void SyncPlaceCardGuard(string cardId, string boardName, string fieldPath) => Rpc(MethodName.OnSyncPlaceCardGuardRpc, [cardId, boardName, fieldPath]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncPlaceCardGuardRpc(string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[OnSyncPlaceCardGuard] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {cardId} {boardName} {fieldPath}");
        if (OnSyncPlaceCardGuard is not null) OnSyncPlaceCardGuard(Multiplayer.GetRemoteSenderId(), cardId, boardName, fieldPath);
    }

    public void SyncGuardPhaseStart(string attackerCardId, string attackedCardId) => Rpc(MethodName.OnSyncGuardPhaseStartRpc, [attackerCardId, attackedCardId]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncGuardPhaseStartRpc(string attackerCardId, string attackedCardId)
    {
        GD.Print($"[OnSyncGuardPhaseStart] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {attackerCardId} {attackedCardId}");
        if (OnGuardPhaseStartEvent is not null) OnGuardPhaseStartEvent(Multiplayer.GetRemoteSenderId(), attackerCardId, attackedCardId);
    }

    public void SyncGuardPhaseEnd() => Rpc(MethodName.OnSyncGuardPhaseEndRpc, []);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncGuardPhaseEndRpc()
    {
        GD.Print($"[OnSyncGuardPhaseEnd] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()}");
        if (OnGuardPhaseEndEvent is not null) OnGuardPhaseEndEvent(Multiplayer.GetRemoteSenderId());
    }

    public void SyncGuardProvided(string guardCardId) => Rpc(MethodName.OnSyncGuardProvidedRpc, [guardCardId]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncGuardProvidedRpc(string guardCardId)
    {
        GD.Print($"[OnSyncGuardProvided] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {guardCardId}");
        if (OnGuardProvidedEvent is not null) OnGuardProvidedEvent(Multiplayer.GetRemoteSenderId(), guardCardId);
    }

    public void SyncBattleResolution(string attackerCardId, string attackedCardId, bool isAttackSuccessful) => Rpc(MethodName.OnSyncBattleResolutionRpc, [attackerCardId, attackedCardId, isAttackSuccessful]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncBattleResolutionRpc(string attackerCardId, string attackedCardId, bool isAttackSuccessful)
    {
        GD.Print($"[OnSyncBattleResolution] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {attackerCardId} {attackedCardId} success={isAttackSuccessful}");
        if (OnBattleResolutionEvent is not null) OnBattleResolutionEvent(Multiplayer.GetRemoteSenderId(), attackerCardId, attackedCardId, isAttackSuccessful);
    }

    public void SyncCardActiveState(string cardId, bool isActive) => Rpc(MethodName.OnSyncCardActiveStateRpc, [cardId, isActive]);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void OnSyncCardActiveStateRpc(string cardId, bool isActive)
    {
        GD.Print($"[OnSyncCardActiveState] {Multiplayer.GetUniqueId()}: {Multiplayer.GetRemoteSenderId()} -> {cardId} active={isActive}");
        if (OnCardActiveStateEvent is not null) OnCardActiveStateEvent(Multiplayer.GetRemoteSenderId(), cardId, isActive);
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
