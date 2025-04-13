using System.Collections.Generic;
using Godot;

public partial class ALGameMatchManager : Node
{
    readonly ALDatabase database = new();
    EALTurnPhase matchCurrentPhase = EALTurnPhase.Reset;
    [Export]
    ALPlayer userPlayer, enemyPlayer;
    List<ALPlayer> orderedPlayers = [];
    int playerIndexPlayingTurn = 0; // First to start

    public override void _Ready()
    {
        base._Ready();
        database.LoadData();
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
            player.OnAttackGuardStart -= OnAttackGuardStartHandler;
            player.OnAttackGuardStart += OnAttackGuardStartHandler;
            player.OnTurnEnd -= OnTurnEndHandler;
            player.OnTurnEnd += OnTurnEndHandler;
            player.Phase.OnPhaseChange -= OnPhaseChangeHandler;
            player.Phase.OnPhaseChange += OnPhaseChangeHandler;
            player.GetPlayerBoard<ALBoard>().OnSkipInteraction -= OnSkipInteractionHandler;
            player.GetPlayerBoard<ALBoard>().OnSkipInteraction += OnSkipInteractionHandler;
            player.GetPlayerHand<ALHand>().OnSkipInteraction -= OnSkipInteractionHandler;
            player.GetPlayerHand<ALHand>().OnSkipInteraction += OnSkipInteractionHandler;
        });

        Callable.From(GetPlayerPlayingTurn().StartTurn).CallDeferred();
    }

    void OnSkipInteractionHandler(Player player, Board triggeringBoard)
    {
        player.SetPlayState(EPlayState.Wait);
        ALPlayer playingPlayer = GetPlayerPlayingTurn();
        if (playingPlayer.IsAwaitingBattleGuard()) _ = playingPlayer.SettleBattle();
    }

    void OnAttackGuardStartHandler(Player attackerPlayer, Player attackedPlayer)
    {
        attackerPlayer.SetPlayState(EPlayState.AwaitEnemyInteraction);
        attackedPlayer.SetPlayState(EPlayState.EnemyInteraction);
    }

    void OnPhaseChangeHandler(EALTurnPhase phase)
    {
        matchCurrentPhase = phase;
    }

    void OnTurnEndHandler()
    {
        _ = this.Wait(1f, () =>
        {
            ALPlayer playingPlayer = GetPlayerPlayingTurn();
            GD.Print($"[OnTurnEndHandler] {playingPlayer.Name} Turn ended!");
            PickNextPlayer().StartTurn();
        });
    }

    public ALPlayer GetPlayerPlayingTurn() => orderedPlayers[playerIndexPlayingTurn];

    ALPlayer PickNextPlayer()
    {
        playerIndexPlayingTurn = orderedPlayers.Count.ApplyCircularBounds(playerIndexPlayingTurn + 1);
        return GetPlayerPlayingTurn();
    }

    public EALTurnPhase GetMatchPhase() => matchCurrentPhase;
    public ALDatabase GetDatabase() => database;

}