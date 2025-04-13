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

    // --- State ---
    ALCard attackerCard, attackedCard;

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
            player.OnAttackStart -= OnAttackStartHandler;
            player.OnAttackStart += OnAttackStartHandler;
            player.OnAttackTargetAdquired -= OnAttackTargetAdquiredHandler;
            player.OnAttackTargetAdquired += OnAttackTargetAdquiredHandler;
            player.OnGuardProvided -= OnGuardProvidedHandler;
            player.OnGuardProvided += OnGuardProvidedHandler;
            player.OnAttackGuardStart -= OnAttackGuardStartHandler;
            player.OnAttackGuardStart += OnAttackGuardStartHandler;
            player.OnAttackGuardEnd -= OnAttackGuardEndHandler;
            player.OnAttackGuardEnd += OnAttackGuardEndHandler;
            player.OnAttackEnd -= OnAttackEndHandler;
            player.OnAttackEnd += OnAttackEndHandler;
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

    void OnAttackStartHandler(Player guardingPlayer, Card card)
    {
        attackerCard = card.CastToALCard();
        GD.Print($"[OnAttackTargetAdquiredHandler] {GetAttackerCard().Name} starts an attack!");
    }

    void OnAttackTargetAdquiredHandler(Player guardingPlayer, Card card)
    {
        GetAttackerCard().SetIsInActiveState(false);
        attackedCard = card.CastToALCard();
        GD.Print($"[OnAttackTargetAdquiredHandler] {GetAttackerCard().Name} attacks {GetAttackedCard().Name}!");
    }

    void OnAttackGuardStartHandler(Player attackerPlayer, Player attackedPlayer)
    {
        if (attackedPlayer is not ALPlayer)
        {
            GD.PrintErr($"[OnAttackGuardStartHandler] {attackedPlayer.GetType()} needs to be an ALPlayer instance.");
            return;
        }
        attackerPlayer.SetPlayState(EPlayState.AwaitEnemyInteraction);
        attackedPlayer.SetPlayState(EPlayState.EnemyInteraction);
        GD.Print($"[OnAttackGuardStartHandler]");
    }

    void OnAttackGuardEndHandler(Player guardingPlayer)
    {
        guardingPlayer.SetPlayState(EPlayState.Wait);
        ALPlayer attackerPlayer = GetAttackerCard().GetOwnerPlayer<ALPlayer>();
        attackerPlayer.SetPlayState(EPlayState.Wait);
        _ = attackerPlayer.SettleBattle();
        GD.Print($"[OnAttackGuardEndHandler]");
    }

    void OnGuardProvidedHandler(Player guardingPlayer, Card card)
    {
        GD.Print($"[OnGuardProvidedHandler]");
        GD.PrintErr($"[OnGuardProvidedHandler] TODO make a buff for the attacked card");
        // TODO make a buff for the attacked card
    }

    void OnAttackEndHandler(Player guardingPlayer)
    {
        attackerCard = null;
        attackedCard = null;
        GD.Print($"[OnAttackEndHandler]");
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
    public bool IsAttackInProgress() => attackedCard is not null && attackerCard is not null;
    public ALCard GetAttackerCard()
    {
        if (attackerCard is not null) return attackerCard;
        GD.PushError($"[GetAttackerCard] Attack not in progress");
        return null;
    }
    public ALCard GetAttackedCard()
    {
        if (IsAttackInProgress()) return attackedCard;
        GD.PushError($"[GetAttackedCard] Attack not in progress");
        return null;
    }
}