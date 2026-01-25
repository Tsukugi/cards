using System.Collections.Generic;
using System.Threading.Tasks;
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

    async void HandleOnSendMatchPhaseEvent(int peerId, int phase)
    {
        GD.Print($"[HandleOnSendMatchPhaseEvent] To {userPlayer.MultiplayerId}: From {peerId}: {phase}");
        matchCurrentPhase = (EALTurnPhase)phase;
        await Task.CompletedTask;
    }
    async void HandleOnSendPlayStateEvent(int peerId, EPlayState state, string interactionState)
    {
        GD.Print($"[HandleOnSendPlayStateEvent] To {userPlayer.MultiplayerId}: Update {peerId} - {state} - {interactionState}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        remotePlayer.SetRemotePlayState(state, interactionState);
        await Task.CompletedTask;
    }
    async void HandleOnSyncFlagship(int peerId, string cardId)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnSyncFlagship] To {userPlayer.MultiplayerId}: From {peerId}: {synchedCard.name}");
        userPlayer.UpdateEnemyFlagship(synchedCard);
        await Task.CompletedTask;
    }
    async void HandleOnSyncDurabilityDamage(int peerId, string cardId)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnSyncDurabilityDamage] To {userPlayer.MultiplayerId}: From {peerId}: {synchedCard.name}");
        userPlayer.ApplyEnemyDurabilityDamage(synchedCard);
        await Task.CompletedTask;
    }
    async void HandleOnDrawCardEvent(int peerId, string cardId, ALDrawType drawType)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnDrawCardEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {synchedCard.name} - {drawType}");
        switch (drawType)
        {
            case ALDrawType.Deck:
                userPlayer.DrawFromEnemyDeck();
                userPlayer.AddEnemyCardToHand(synchedCard);
                break;
            case ALDrawType.Cube:
                userPlayer.DrawFromEnemyCubeDeck();
                await userPlayer.PlaceEnemyCubeToBoard(synchedCard);
                break;
            case ALDrawType.Durability:
                userPlayer.DrawFromEnemyDeck();
                await userPlayer.PlaceEnemyDurabilityCard(synchedCard);
                break;
        }
    }
    async void HandleOnSyncPlaceCard(int peerId, string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[HandleOnSyncPlaceCard] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} {boardName} {fieldPath}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        if (!database.cards.TryGetValue(cardId, out ALCardDTO synchedCard))
        {
            throw new System.InvalidOperationException($"[HandleOnSyncPlaceCard] Card id not found: {cardId}");
        }
        await userPlayer.PlaceEnemyCardToBoard(synchedCard, boardName, fieldPath);
    }

    async void HandleOnSyncPlaceCardGuard(int peerId, string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[HandleOnSyncPlaceCardGuard] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} {boardName} {fieldPath}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        if (!database.cards.TryGetValue(cardId, out ALCardDTO synchedCard))
        {
            throw new System.InvalidOperationException($"[HandleOnSyncPlaceCardGuard] Card id not found: {cardId}");
        }
        await userPlayer.PlaceEnemyCardToBoard(synchedCard, boardName, fieldPath);
    }

    async void HandleOnGuardPhaseStartEvent(int peerId, string attackerCardId, string attackedCardId)
    {
        GD.Print($"[HandleOnGuardPhaseStartEvent] To {userPlayer.MultiplayerId}: From {peerId}: attacker={attackerCardId} attacked={attackedCardId}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        SetAttackContextFromRemote(attackerCardId, attackedCardId);
        await userPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }

    async void HandleOnGuardPhaseEndEvent(int peerId)
    {
        GD.Print($"[HandleOnGuardPhaseEndEvent] To {userPlayer.MultiplayerId}: From {peerId}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        await ResolveGuardPhaseFromRemote();
    }

    async void HandleOnGuardProvidedEvent(int peerId, string guardCardId)
    {
        GD.Print($"[HandleOnGuardProvidedEvent] To {userPlayer.MultiplayerId}: From {peerId}: guard={guardCardId}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        await ApplyRemoteGuard(guardCardId);
    }

    async void HandleOnBattleResolutionEvent(int peerId, string attackerCardId, string attackedCardId, bool isAttackSuccessful)
    {
        GD.Print($"[HandleOnBattleResolutionEvent] To {userPlayer.MultiplayerId}: From {peerId}: attacker={attackerCardId} attacked={attackedCardId} success={isAttackSuccessful}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        SetAttackContextFromRemote(attackerCardId, attackedCardId);
        await ResolveBattleFromRemote(isAttackSuccessful);
    }

    async void HandleOnCardActiveStateEvent(int peerId, string cardId, bool isActive)
    {
        GD.Print($"[HandleOnCardActiveStateEvent] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} active={isActive}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        RegisterEnemyPeer(peerId);
        ALCard targetCard = FindCardById(cardId);
        targetCard.SetIsInActiveState(isActive, false);
        await Task.CompletedTask;
    }
    async void HandleOnCardSelectEvent(int peerId, int targetOwnerPeerId, string boardName, Vector2I position)
    {
        GD.Print($"[HandleOnCardSelectEvent] To {userPlayer.MultiplayerId}: From {peerId}: owner={targetOwnerPeerId} -> {boardName} - {position}");
        if (peerId == userPlayer.MultiplayerId)
        {
            GD.Print($"[HandleOnCardSelectEvent] Ignore self selection from {peerId}.");
            return;
        }

        RegisterEnemyPeer(peerId);
        Board board = ResolveSelectionBoard(boardName, targetOwnerPeerId);
        if (board is null)
        {
            await Task.CompletedTask;
            return;
        }
        if (remoteSelectedBoard is not null && remoteSelectedBoard != board)
        {
            remoteSelectedBoard.ClearSelectionForPlayer(remotePlayer);
        }
        Vector2I mappedPosition = position;
        if (board is ALBoard alBoard)
        {
            mappedPosition = alBoard.MapToOppositeSidePosition(position);
        }
        GD.Print($"[HandleOnCardSelectEvent.Resolve] selector={remotePlayer.Name}({remotePlayer.MultiplayerId}) board={board.Name} pos={position} mapped={mappedPosition}");
        board.SelectCardField(remotePlayer, mappedPosition, false);
        remoteSelectedBoard = board;
        await Task.CompletedTask;
    }

    Board ResolveSelectionBoard(string boardName, int targetOwnerPeerId)
    {
        if (boardName == "Board")
        {
            Board board = userPlayer.GetPlayerBoard<PlayerBoard>();
            if (board is null)
            {
                throw new System.InvalidOperationException($"[ResolveSelectionBoard] Board '{boardName}' not found for player {userPlayer.Name}.");
            }
            return board;
        }
        if (boardName == "Hand")
        {
            if (targetOwnerPeerId != userPlayer.MultiplayerId)
            {
                Board enemyHand = userPlayer.GetEnemyPlayerHand<PlayerHand>() ?? throw new System.InvalidOperationException($"[ResolveSelectionBoard] Enemy hand not found for remote selection. owner={targetOwnerPeerId} local={userPlayer.MultiplayerId}");
                return enemyHand;
            }
            Board board = userPlayer.GetPlayerHand<PlayerHand>() ?? throw new System.InvalidOperationException($"[ResolveSelectionBoard] Hand '{boardName}' not found for player {userPlayer.Name}.");
            return board;
        }
        throw new System.InvalidOperationException($"[ResolveSelectionBoard] Unknown board name '{boardName}'.");
    }

    async void HandleOnInputActionEvent(int peerId, InputAction inputAction)
    {
        GD.Print($"[HandleOnInputActionEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {inputAction}");
        if (peerId == userPlayer.MultiplayerId)
        {
            await Task.CompletedTask;
            return;
        }
        GD.Print($"[HandleOnInputActionEvent] Remote input ignored; apply explicit sync updates instead.");
        await Task.CompletedTask;
    }
    async void HandleOnTurnEndEvent(int peerId)
    {
        if (peerId == userPlayer.MultiplayerId)
        {
            GD.PushError("[HandleOnTurnEndEvent] Local peer cannot end its own turn via network event.");
            return;
        }
        if (currentTurnPeerId != 0 && currentTurnPeerId != peerId)
        {
            GD.PushError("[HandleOnTurnEndEvent] Another peer is trying to end their turn");
            return;
        }
        GD.Print($"[HandleOnTurnEndEvent] To {userPlayer.MultiplayerId}: From {peerId}: Finishes its turn");
        AdvanceTurnOwner();
        StartLocalTurnIfNeeded();
        await Task.CompletedTask;
    }

    bool ShouldSkipAutoPhasesForTest()
    {
        return IsTestRunRequested();
    }

    // Match
    async void StartMatchForPlayer()
    {
        await AssignDeckSet();
        if (ShouldSkipAutoPhasesForTest())
        {
            userPlayer.Phase.SetSkipAutoPhases(true);
        }
        await userPlayer.StartGameForPlayer(userPlayer.GetDeckSet());
        if (Multiplayer.IsServer())
        {
            if (ShouldSkipAutoPhasesForTest())
            {
                await userPlayer.Phase.ForceMainPhase(true);
            }
            else
            {
                StartLocalTurnIfNeeded();
            }
        }
        // if (!GetNextPlayer().GetIsControllerPlayer()) Callable.From(GetNextPlayer().GetPlayerAIController().StartTurn).CallDeferred();
    }

    void TryStartGameplayTest()
    {
        string testPath = GetTestFilter();
        if (string.IsNullOrWhiteSpace(testPath))
        {
            return;
        }
        string className = GetGameplayTestClassName(testPath);
        if (string.IsNullOrWhiteSpace(className))
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] Invalid test path: {testPath}");
        }
        System.Type testType = FindGameplayTestType(className);
        if (testType is null)
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] No gameplay test type found for {className}.");
        }
        var ctor = testType.GetConstructor(new[] { typeof(ALGameMatchManager), typeof(float) });
        if (ctor is null)
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] Missing required constructor on {className}.");
        }
        object instance = ctor.Invoke(new object[] { this, debug.GetSelectionSyncStepSeconds() });
        if (instance is not ISelectionSyncTest test)
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] {className} does not implement ISelectionSyncTest.");
        }
        _ = test.Run();
    }

    static string GetGameplayTestClassName(string testPath)
    {
        string normalized = testPath.Replace('\\', '/').Trim();
        string fileName = System.IO.Path.GetFileNameWithoutExtension(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "";
        }
        if (fileName.StartsWith("Test_", System.StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[5..];
        }
        if (fileName.EndsWith("Test", System.StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }
        return $"{fileName}Test";
    }

    static System.Type FindGameplayTestType(string className)
    {
        var assembly = typeof(ALGameMatchManager).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type is null || type.IsAbstract)
            {
                continue;
            }
            if (!typeof(ISelectionSyncTest).IsAssignableFrom(type))
            {
                continue;
            }
            if (string.Equals(type.Name, className, System.StringComparison.Ordinal))
            {
                return type;
            }
        }
        return null;
    }

    bool IsTestRunRequested()
    {
        return IsSingleTestRunRequested();
    }

    static bool IsSingleTestRunRequested()
    {
        string value = GetTestFilter();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return !IsFalseValue(value);
        }
        return HasCommandLineFlag("--test") || HasCommandLineFlag("-test");
    }

    static bool IsFalseValue(string value)
    {
        return string.Equals(value, "0", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "false", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool HasCommandLineFlag(string key)
    {
        return HasCommandLineFlagInArgs(OS.GetCmdlineUserArgs(), key)
            || HasCommandLineFlagInArgs(OS.GetCmdlineArgs(), key);
    }

    static bool HasCommandLineFlagInArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0) return false;
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null) continue;
            if (arg == key || arg.StartsWith(key + "=", System.StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    static string GetCommandLineValue(string key)
    {
        string value = GetCommandLineValueFromArgs(OS.GetCmdlineUserArgs(), key);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return GetCommandLineValueFromArgs(OS.GetCmdlineArgs(), key);
    }

    static string GetTestFilter()
    {
        string value = GetCommandLineValue("--test");
        if (string.IsNullOrWhiteSpace(value))
        {
            value = GetCommandLineValue("-test");
        }
        if (value is null)
        {
            return "";
        }
        return value;
    }

    static string GetCommandLineValueFromArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0) return "";
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null) continue;
            if (arg.StartsWith(key + "=", System.StringComparison.Ordinal))
            {
                return arg[(key.Length + 1)..];
            }
            if (arg == key && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }
        return "";
    }

    async Task OnGameOverHandler(Player losingPlayer)
    {
        bool isVictory = !losingPlayer.GetIsControllerPlayer();
        await playerUI.ShowGameOverUI(isVictory);
        await ExitMatch();
    }
    async Task ExitMatch()
    {
        this.ChangeScene($"{ALMain.ALSceneRootPath}/main.tscn");
        await Task.CompletedTask;
    }

    async Task OnAttackStartHandler(Player attackingPlayer, Card card)
    {
        attackerCard = card.CastToALCard();
        await GetAttackerCard().TryToTriggerCardEffect(ALCardEffectTrigger.StartsAttack);
        GD.Print($"[OnAttackStartHandler] {GetAttackerCard().Name} starts an attack!");
        await attackingPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectAttackTarget);
    }

    async Task OnAttackTargetAdquiredHandler(Player guardingPlayer, Card card)
    {
        GetAttackerCard().SetIsInActiveState(false);
        attackedCard = card.CastToALCard();
        GD.Print($"[OnAttackTargetAdquiredHandler] {GetAttackerCard().Name} attacks {GetAttackedCard().Name}!");
        await Task.CompletedTask;
    }

    async Task OnAttackGuardStartHandler(Player attackerPlayer, Player attackedPlayer)
    {
        if (GetAttackedCard() is null)
        {
            throw new System.InvalidOperationException("[OnAttackGuardStartHandler] Attacked card is missing.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        await attackerPlayer.SetPlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        if (attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnAttackGuardStartHandler");
            ALNetwork.Instance.SyncGuardPhaseStart(GetAttackerCard().GetAttributes<ALCardDTO>().id, GetAttackedCard().GetAttributes<ALCardDTO>().id);
            UpdateRemotePlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
            return;
        }
        await userPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        GD.Print($"[OnAttackGuardStartHandler]");
    }

    async Task OnAttackGuardEndHandler(Player guardingPlayer)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[OnAttackGuardEndHandler] Guard phase ended without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        await guardingPlayer.SetPlayState(EPlayState.Wait);
        if (!attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnAttackGuardEndHandler");
            UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
            ALNetwork.Instance.SyncGuardPhaseEnd();
            GD.Print($"[OnAttackGuardEndHandler] Sent guard end to attacker.");
            return;
        }
        await ResolveGuardPhaseLocally();
    }

    async Task OnRetaliationHandler(Player damagedPlayer, Card retaliatingCard)
    {
        await damagedPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectRetaliationUnit);
        ALHand hand = damagedPlayer.GetPlayerHand<ALHand>();
        if (hand.GetSelectedCard<Card>(damagedPlayer) is null)
        {
            throw new System.InvalidOperationException($"[OnRetaliationHandler] No selected card on board {hand.Name} for player {damagedPlayer.Name}.");
        }
        damagedPlayer.SelectBoard(damagedPlayer, hand);
        await retaliatingCard.TryToTriggerCardEffect(ALCardEffectTrigger.Retaliation);
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }
    async Task OnRetaliationCancel(Player damagedPlayer)
    {
        await damagedPlayer.GoBackInHistoryState();
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }

    async Task OnGuardProvidedHandler(Player guardingPlayer, Card card)
    {
        GetAttackedCard().AddModifier(new AttributeModifier()
        {
            Id = "Guard",
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = card.GetAttributes<ALCardDTO>().supportValue,
            StackableModifier = true
        });
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsBattleSupported);
        GD.Print($"[OnGuardProvidedHandler] Add Guard Modifier for {GetAttackedCard().GetAttributes<ALCardDTO>().name}");
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        if (!attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnGuardProvidedHandler");
            ALNetwork.Instance.SyncGuardProvided(card.GetAttributes<ALCardDTO>().id);
        }
    }

    async Task OnAttackEndHandler(Player guardingPlayer)
    {
        await FinishBattleResolution();
        GD.Print($"[OnAttackEndHandler]");
    }

    void OnPhaseChangeHandler(EALTurnPhase phase)
    {
        matchCurrentPhase = phase;
    }

    async void OnTurnEndHandler()
    {
        await this.Wait(1f);
        if (!IsLocalTurn())
        {
            GD.PushError("[OnTurnEndHandler] Local player cannot end a remote turn.");
            return;
        }
        GD.Print($"[OnTurnEndHandler] {userPlayer.Name} Turn ended!");

        await userPlayer.TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        await this.Wait(1f);
        ALNetwork.Instance.SendTurnEnd();
        AdvanceTurnOwner();
        // if (!GetPlayerPlayingTurn().GetIsControllerPlayer()) GetPlayerPlayingTurn().GetPlayerAIController().StartTurn();
    }

    async Task AssignDeckSet()
    {
        var userPlayerDeckSetId = Multiplayer.IsServer() ? "SD03" : "SD02";
        userPlayer.AssignDeck(BuildDeckSet(userPlayerDeckSetId));
        ALNetwork.Instance.SendDeckSet(userPlayerDeckSetId);
        await userPlayer.GetAsyncHandler().AwaitForCheck(null, userPlayer.HasValidDeck, -1);
        await userPlayer.GetAsyncHandler().AwaitForCheck(null, userPlayer.HasValidEnemyDeck, -1);
    }

    void EnsureRemotePlayer(int peerId)
    {
        if (peerId <= 0)
        {
            throw new System.InvalidOperationException($"[EnsureRemotePlayer] Invalid peer id {peerId}.");
        }
        if (remotePlayer is not null)
        {
            if (remotePlayer.MultiplayerId != peerId)
            {
                throw new System.InvalidOperationException($"[EnsureRemotePlayer] Remote player id mismatch. Current={remotePlayer.MultiplayerId} New={peerId}.");
            }
            return;
        }
        remotePlayer = new ALRemotePlayer();
        remotePlayer.Initialize(peerId, "Enemy", RemotePlayerColor);
    }

    void UpdateRemotePlayState(EPlayState state, string interactionState)
    {
        if (enemyPeerId == 0)
        {
            return;
        }
        if (remotePlayer is null)
        {
            throw new System.InvalidOperationException("[UpdateRemotePlayState] Remote player is not registered.");
        }
        remotePlayer.SetRemotePlayState(state, interactionState);
    }

    void AdvanceTurnOwner()
    {
        if (enemyPeerId == 0)
        {
            throw new System.InvalidOperationException("[AdvanceTurnOwner] Enemy peer id is not registered.");
        }
        if (currentTurnPeerId == userPlayer.MultiplayerId)
        {
            currentTurnPeerId = enemyPeerId;
            return;
        }
        currentTurnPeerId = userPlayer.MultiplayerId;
    }

    void StartLocalTurnIfNeeded()
    {
        if (!IsLocalTurn()) return;
        userPlayer.StartTurn();
    }

    public void RegisterEnemyPeer(int peerId)
    {
        if (peerId <= 0)
        {
            throw new System.InvalidOperationException($"[RegisterEnemyPeer] Invalid peer id {peerId}.");
        }
        if (enemyPeerId != 0 && enemyPeerId != peerId)
        {
            throw new System.InvalidOperationException($"[RegisterEnemyPeer] Enemy peer already set to {enemyPeerId}, got {peerId}.");
        }
        enemyPeerId = peerId;
        EnsureRemotePlayer(peerId);
        if (currentTurnPeerId < 0)
        {
            currentTurnPeerId = enemyPeerId;
        }
        GD.Print($"[RegisterEnemyPeer] enemyPeerId={enemyPeerId} currentTurnPeerId={currentTurnPeerId}");
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

    void EnsureEnemyPeerId(string context)
    {
        if (enemyPeerId != 0)
        {
            return;
        }
        throw new System.InvalidOperationException($"[{context}] Enemy peer id is not registered.");
    }

    void SetAttackContextFromRemote(string attackerCardId, string attackedCardId)
    {
        attackerCard = FindBoardCardById(attackerCardId);
        attackedCard = FindBoardCardById(attackedCardId);
    }

    ALCard FindBoardCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[FindBoardCardById] Card id is required.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>() ?? throw new System.InvalidOperationException("[FindBoardCardById] Player board is missing.");
        List<Card> cards = board.GetCardsInTree();
        foreach (Card card in cards)
        {
            if (card is not ALCard alCard)
            {
                continue;
            }
            if (alCard.GetAttributes<ALCardDTO>().id == cardId)
            {
                return alCard;
            }
        }
        throw new System.InvalidOperationException($"[FindBoardCardById] Card id not found: {cardId}");
    }

    ALCard FindCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[FindCardById] Card id is required.");
        }
        ALCard boardCard = TryFindBoardCardById(cardId);
        if (boardCard is not null) return boardCard;
        ALHand hand = userPlayer.GetPlayerHand<ALHand>();
        if (hand is not null)
        {
            List<Card> handCards = hand.GetCardsInTree();
            foreach (Card card in handCards)
            {
                if (card is ALCard alCard && alCard.GetAttributes<ALCardDTO>().id == cardId)
                {
                    return alCard;
                }
            }
        }
        ALHand enemyHand = userPlayer.GetEnemyPlayerHand<ALHand>();
        if (enemyHand is not null)
        {
            List<Card> enemyHandCards = enemyHand.GetCardsInTree();
            foreach (Card card in enemyHandCards)
            {
                if (card is ALCard alCard && alCard.GetAttributes<ALCardDTO>().id == cardId)
                {
                    return alCard;
                }
            }
        }
        throw new System.InvalidOperationException($"[FindCardById] Card id not found: {cardId}");
    }

    ALCard TryFindBoardCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[TryFindBoardCardById] Card id is required.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        if (board is null)
        {
            throw new System.InvalidOperationException("[TryFindBoardCardById] Player board is missing.");
        }
        List<Card> cards = board.GetCardsInTree();
        foreach (Card card in cards)
        {
            if (card is not ALCard alCard)
            {
                continue;
            }
            if (alCard.GetAttributes<ALCardDTO>().id == cardId)
            {
                return alCard;
            }
        }
        return null;
    }

    async Task ApplyRemoteGuard(string guardCardId)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ApplyRemoteGuard] No attack is in progress.");
        }
        if (!database.cards.TryGetValue(guardCardId, out ALCardDTO guardCard))
        {
            throw new System.InvalidOperationException($"[ApplyRemoteGuard] Guard card id not found: {guardCardId}");
        }
        GetAttackedCard().AddModifier(new AttributeModifier()
        {
            Id = "Guard",
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = guardCard.supportValue,
            StackableModifier = true
        });
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsBattleSupported);
        GD.Print($"[ApplyRemoteGuard] Add Guard Modifier for {GetAttackedCard().GetAttributes<ALCardDTO>().name}");
    }

    async Task ResolveGuardPhaseFromRemote()
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveGuardPhaseFromRemote] Guard phase ended without an active attack.");
        }
        await ResolveGuardPhaseLocally();
    }

    async Task ResolveGuardPhaseLocally()
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveGuardPhaseLocally] Guard phase ended without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        bool isAttackSuccessful = GetBattleOutcome(GetAttackerCard(), GetAttackedCard());
        if (attackedIsEnemy)
        {
            EnsureEnemyPeerId("ResolveGuardPhaseLocally");
            ALNetwork.Instance.SyncBattleResolution(GetAttackerCard().GetAttributes<ALCardDTO>().id, GetAttackedCard().GetAttributes<ALCardDTO>().id, isAttackSuccessful);
        }
        ALPlayer attackerPlayer = GetAttackerCard().GetOwnerPlayer<ALPlayer>();
        await attackerPlayer.SetPlayState(EPlayState.Wait);
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsAttacked);
        await attackerPlayer.SettleBattle(playerUI);
        GD.Print($"[ResolveGuardPhaseLocally]");
    }

    async Task ResolveBattleFromRemote(bool isAttackSuccessful)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveBattleFromRemote] Battle resolution without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        System.Func<ALCard, Task> applyFlagshipDamage = _ => Task.CompletedTask;
        System.Func<ALCard, Task> destroyUnit = _ => Task.CompletedTask;
        if (!attackedIsEnemy)
        {
            applyFlagshipDamage = card =>
            {
                GD.Print($"[ResolveBattleFromRemote] {card.Name} Takes durability damage!");
                card.TakeDurabilityDamage();
                return Task.CompletedTask;
            };
            destroyUnit = async card =>
            {
                GD.Print($"[ResolveBattleFromRemote] {card.Name} destroyed!");
                await ALPlayer.DestroyUnitCard(card);
            };
        }
        await ApplyBattleResolution(GetAttackerCard(), GetAttackedCard(), isAttackSuccessful, applyFlagshipDamage, destroyUnit);
        await FinishBattleResolution();
    }

    static bool GetBattleOutcome(ALCard attacker, ALCard attacked)
    {
        float attackerPower = attacker.GetAttributeWithModifiers<ALCardDTO>("Power");
        float attackedPower = attacked.GetAttributeWithModifiers<ALCardDTO>("Power");
        return attackerPower >= attackedPower;
    }

    public async Task ApplyBattleResolution(ALCard attacker, ALCard attacked, bool isAttackSuccessful, System.Func<ALCard, Task> applyFlagshipDamage, System.Func<ALCard, Task> destroyUnit)
    {
        if (attacker is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Attacker is required.");
        }
        if (attacked is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Attacked card is required.");
        }
        if (applyFlagshipDamage is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Flagship damage handler is required.");
        }
        if (destroyUnit is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Destroy unit handler is required.");
        }
        await playerUI.OnSettleBattleUI(attacker, attacked, isAttackSuccessful);
        if (!isAttackSuccessful)
        {
            return;
        }
        if (attacked.GetIsAFlagship())
        {
            await applyFlagshipDamage(attacked);
            return;
        }
        await destroyUnit(attacked);
    }

    async Task FinishBattleResolution()
    {
        await userPlayer.TryToExpireCardsModifierDuration(ALCardEffectDuration.CurrentBattle);
        attackerCard = null;
        attackedCard = null;
    }

    public ALDeckSet BuildDeckSet(string deckId)
    {
        ALDeckDTO deckDefinition = database.decks[deckId];
        ALDeckSet deckToUse = new()
        {
            name = deckDefinition.name,
            flagship = database.cards[deckDefinition.flagship],
            deck = TransformCardDictToList(deckDefinition.cards, database.cards).Shuffle(),
            cubeDeck = TransformCardDictToList(deckDefinition.cubes, database.cards).Shuffle()
        };
        return deckToUse;
    }

    public static List<ALCardDTO> TransformCardDictToList(Dictionary<string, int> deckDict, Dictionary<string, ALCardDTO> cardsDatabase)
    {
        List<ALCardDTO> cardList = [];
        foreach (var card in deckDict)
        {
            for (int i = 0; i < card.Value; i++)
            {
                cardList.Add(cardsDatabase[card.Key]);
            }
        }
        return cardList;
    }

    public void OnEnemyDeckSetProvided(string enemyDeckId)
    {
        GD.Print($"[OnEnemyDeckSetProvided] {enemyDeckId}");
        userPlayer.AssignEnemyDeck(BuildDeckSet(enemyDeckId));
    }
    public ALDebug GetDebug() => debug;
}
