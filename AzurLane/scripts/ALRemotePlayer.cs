using System;
using Godot;

public partial class ALRemotePlayer : Player
{
    PlayState remotePlayState = new();

    public override void _Ready()
    {
        // Remote players are network-driven; skip local input setup.
    }

    public override void _Process(double delta)
    {
        // Intentionally empty: remote players do not process local input.
    }

    public void Initialize(int peerId, string displayName, Color color)
    {
        if (peerId <= 0)
        {
            throw new InvalidOperationException($"[ALRemotePlayer.Initialize] Invalid peer id {peerId}.");
        }
        MultiplayerId = peerId;
        Name = displayName;
        isControlledPlayer = false;
        SetPlayerColor(color);
    }

    public void SetRemotePlayState(EPlayState state, string interactionState)
    {
        remotePlayState.state = state;
        remotePlayState.interactionState = string.IsNullOrWhiteSpace(interactionState)
            ? ALInteractionState.None
            : interactionState;
    }

    public PlayState GetRemotePlayState() => remotePlayState;
    public EPlayState GetRemoteInputPlayState() => remotePlayState.state;
    public string GetRemoteInteractionState() => remotePlayState.interactionState;
}
