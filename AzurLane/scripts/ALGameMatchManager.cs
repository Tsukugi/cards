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

        ALHand userHand = userPlayer.GetPlayerHand<ALHand>();
        ALHand enemyHand = enemyPlayer.GetPlayerHand<ALHand>();
        ALBoard userBoard = userPlayer.GetPlayerBoard<ALBoard>();
        ALBoard enemyBoard = enemyPlayer.GetPlayerBoard<ALBoard>();

        // IsEnemyBoard is needed for thinks like flipping the Input axis
        enemyHand.SetIsEnemyBoard(true);
        enemyBoard.SetIsEnemyBoard(true);

        // Assign Enemy boards is needed to handle onBoardEdges
        userPlayer.AssignEnemyBoards(enemyHand, enemyBoard);
        enemyPlayer.AssignEnemyBoards(userHand, userBoard);

        orderedPlayers.ForEach(player =>
        {
            player.OnTurnEnd -= OnTurnEndHandler;
            player.OnTurnEnd += OnTurnEndHandler;
            player.OnPhaseChange -= OnPhaseChangeHandler;
            player.OnPhaseChange += OnPhaseChangeHandler;
        });

        StartTurn();
    }

    void OnPhaseChangeHandler(EALTurnPhase phase)
    {
        orderedPlayers.ForEach(player => player.SyncPhase(phase));
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

    public Player GetPlayerPlayingTurn() => orderedPlayers[playerIndexPlayingTurn];

}