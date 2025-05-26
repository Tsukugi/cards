using Godot;

public partial class ALNetwork : Network
{
    ALGameMatchManager matchManager;
    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.AfterStartMatch, filePath);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerReadyForMatch(string deckSetId)
    {
        int playerId = Multiplayer.GetRemoteSenderId();

        matchManager = GetNode<ALGameMatchManager>("/root/main");
        matchManager.OnEnemyDeckSetProvided(deckSetId);
        GD.Print($"[{playerId}.PlayerReadyForMatch] {deckSetId}");
    }
}