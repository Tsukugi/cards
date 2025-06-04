using Godot;
using Newtonsoft.Json;

public partial class ALNetwork : Network
{
    public new delegate void PlayerCardEvent(int peerId, ALCardDTO card);
    public new event PlayerCardEvent OnDrawCardEvent;
    ALGameMatchManager matchManager;
    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.AfterStartMatch, filePath);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnSendDeckSet(string deckSetId)
    {
        int playerId = Multiplayer.GetRemoteSenderId();
        matchManager.OnEnemyDeckSetProvided(deckSetId);
        GD.Print($"[{playerId}.OnSendDeckSet] {deckSetId}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected override void OnDrawCard(string data)
    {
        if (OnDrawCardEvent is not null) OnDrawCardEvent(Multiplayer.GetRemoteSenderId(), JsonConvert.DeserializeObject<ALCardDTO>(data));
    }

    public void OnMatchStart() => matchManager = GetNode<ALGameMatchManager>("/root/main");
    public void SendDeckSet(string userPlayerDeckSetId) => Rpc(MethodName.OnSendDeckSet, [userPlayerDeckSetId]);
}