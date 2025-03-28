using System.Collections.Generic;
using Godot;

public partial class ALGameMatchManager : Node
{
    [Export]
    ALPlayer userPlayer, enemyPlayer;
    List<ALPlayer> orderedPlayers = [];
    int playerIndexPlayingTurn = 0; // First to start

    public override void _Ready()
    {
        base._Ready();
        orderedPlayers = [enemyPlayer, userPlayer]; // TODO add some shuffling, with a minigame
        orderedPlayers.ForEach(player =>
        {
            player.OnTurnEnd -= OnTurnEndHandler;
            player.OnTurnEnd += OnTurnEndHandler;
        });
        StartTurn();
    }

    void OnTurnEndHandler()
    {
        ALPlayer playingPlayer = orderedPlayers[playerIndexPlayingTurn];
        GD.Print($"[StartTurn] {playingPlayer.Name} Turn ended!");
        // Pick next player
        playerIndexPlayingTurn = orderedPlayers.Count.ApplyCircularBounds(playerIndexPlayingTurn + 1);
        StartTurn();
    }

    void StartTurn()
    {
        ALPlayer playingPlayer = orderedPlayers[playerIndexPlayingTurn];
        GD.Print($"[StartTurn] {playingPlayer.Name} Starting turn!");
        playingPlayer.StartTurn();
    }

}