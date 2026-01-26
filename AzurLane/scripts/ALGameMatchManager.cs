using System.Collections.Generic;
using Godot;

public partial class ALGameMatchManager : Node
{
    readonly ALDatabase database = new();
    EALTurnPhase matchCurrentPhase = EALTurnPhase.Reset;
    [Export]
    ALPlayer userPlayer;
    ALRemotePlayer remotePlayer;
    readonly List<ALPlayer> orderedPlayers = [];
    int enemyPeerId = 0;
    int currentTurnPeerId = 0;
    Board remoteSelectedBoard;
    static readonly Color RemotePlayerColor = new(0.83f, 0.36f, 0.32f, 1f);

    // --- State ---
    ALCard attackerCard, attackedCard;
    ALPlayerUI playerUI;
    ALDebug debug;
    ALInteraction interaction;

    public override void _Ready()
    {
        base._Ready();

        userPlayer.MultiplayerId = Network.Instance.Multiplayer.GetUniqueId();
        ALNetwork.Instance.OnMatchStart();
        debug = new(this);
        interaction = new(this);

        playerUI = GetNode<ALPlayerUI>("Control");
        playerUI.SetPlayer(userPlayer); // Assign the controlling player
        playerUI.SyncDebugMenuState();

        database.LoadData();

        // --- Players --- 
        orderedPlayers.Clear();
        orderedPlayers.Add(userPlayer);
        currentTurnPeerId = Multiplayer.IsServer() ? userPlayer.MultiplayerId : -1;

        foreach (var player in orderedPlayers)
        {
            player.OnGameOver -= OnGameOverHandler;
            player.OnGameOver += OnGameOverHandler;
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
            player.OnRetaliation -= OnRetaliationHandler;
            player.OnRetaliation += OnRetaliationHandler;
            player.OnRetaliationCancel -= OnRetaliationCancel;
            player.OnRetaliationCancel += OnRetaliationCancel;
            player.Phase.OnPhaseChange -= OnPhaseChangeHandler;
            player.Phase.OnPhaseChange += OnPhaseChangeHandler;
            player.GetPlayerBoard<ALBoard>().OnInputAction -= interaction.OnBoardInputActionHandler;
            player.GetPlayerBoard<ALBoard>().OnInputAction += interaction.OnBoardInputActionHandler;
            player.GetPlayerHand<ALHand>().OnInputAction -= interaction.OnHandInputActionHandler;
            player.GetPlayerHand<ALHand>().OnInputAction += interaction.OnHandInputActionHandler;

        }
        ALNetwork.Instance.OnTurnEndEvent -= HandleOnTurnEndEvent;
        ALNetwork.Instance.OnTurnEndEvent += HandleOnTurnEndEvent;
        ALNetwork.Instance.OnSendMatchPhaseEvent -= HandleOnSendMatchPhaseEvent;
        ALNetwork.Instance.OnSendMatchPhaseEvent += HandleOnSendMatchPhaseEvent;
        ALNetwork.Instance.OnSendPlayStateEvent -= HandleOnSendPlayStateEvent;
        ALNetwork.Instance.OnSendPlayStateEvent += HandleOnSendPlayStateEvent;
        ALNetwork.Instance.OnDrawCardEvent -= HandleOnDrawCardEvent;
        ALNetwork.Instance.OnDrawCardEvent += HandleOnDrawCardEvent;
        ALNetwork.Instance.OnSyncFlagshipEvent -= HandleOnSyncFlagship;
        ALNetwork.Instance.OnSyncFlagshipEvent += HandleOnSyncFlagship;
        ALNetwork.Instance.OnSyncDurabilityDamageEvent -= HandleOnSyncDurabilityDamage;
        ALNetwork.Instance.OnSyncDurabilityDamageEvent += HandleOnSyncDurabilityDamage;
        ALNetwork.Instance.OnSyncPlaceCard -= HandleOnSyncPlaceCard;
        ALNetwork.Instance.OnSyncPlaceCard += HandleOnSyncPlaceCard;
        ALNetwork.Instance.OnSyncPlaceCardGuard -= HandleOnSyncPlaceCardGuard;
        ALNetwork.Instance.OnSyncPlaceCardGuard += HandleOnSyncPlaceCardGuard;
        ALNetwork.Instance.OnGuardPhaseStartEvent -= HandleOnGuardPhaseStartEvent;
        ALNetwork.Instance.OnGuardPhaseStartEvent += HandleOnGuardPhaseStartEvent;
        ALNetwork.Instance.OnGuardPhaseEndEvent -= HandleOnGuardPhaseEndEvent;
        ALNetwork.Instance.OnGuardPhaseEndEvent += HandleOnGuardPhaseEndEvent;
        ALNetwork.Instance.OnGuardProvidedEvent -= HandleOnGuardProvidedEvent;
        ALNetwork.Instance.OnGuardProvidedEvent += HandleOnGuardProvidedEvent;
        ALNetwork.Instance.OnBattleResolutionEvent -= HandleOnBattleResolutionEvent;
        ALNetwork.Instance.OnBattleResolutionEvent += HandleOnBattleResolutionEvent;
        ALNetwork.Instance.OnCardActiveStateEvent -= HandleOnCardActiveStateEvent;
        ALNetwork.Instance.OnCardActiveStateEvent += HandleOnCardActiveStateEvent;
        ALNetwork.Instance.OnSendSelectCardEvent -= HandleOnCardSelectEvent;
        ALNetwork.Instance.OnSendSelectCardEvent += HandleOnCardSelectEvent;
        ALNetwork.Instance.OnSendInputActionEvent -= HandleOnInputActionEvent;
        ALNetwork.Instance.OnSendInputActionEvent += HandleOnInputActionEvent;

        Callable.From(StartMatchForPlayer).CallDeferred();
        Callable.From(TryStartGameplayTest).CallDeferred();
    }

    // ----- API -----
    public ALPlayerUI GetPlayerUI() => playerUI;
    public List<ALPlayer> GetOrderedPlayers() => orderedPlayers;
    public ALPlayer GetPlayerPlayingTurn()
    {
        if (!IsLocalTurn())
        {
            throw new System.InvalidOperationException("[GetPlayerPlayingTurn] Remote turn has no local player instance.");
        }
        return userPlayer;
    }
    public ALPlayer GetControlledPlayer() => userPlayer;
    public ALRemotePlayer GetRemotePlayer() => remotePlayer;
    public bool IsLocalTurn() => currentTurnPeerId == userPlayer.MultiplayerId;
    public int GetEnemyPeerId() => enemyPeerId;
    public int GetCurrentTurnPeerId() => currentTurnPeerId;
    public EALTurnPhase GetMatchPhase() => matchCurrentPhase;
    public ALDatabase GetDatabase() => database;
    public bool IsAttackInProgress() => attackedCard is not null && attackerCard is not null;
    public ALPlayer GetUserPlayer() => userPlayer;
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

    public ALDebug GetDebug() => debug;
}
