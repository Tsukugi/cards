using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALGameMatchManager : Node
{
    readonly ALDatabase database = new();
    EALTurnPhase matchCurrentPhase = EALTurnPhase.Reset;
    [Export]
    ALPlayer userPlayer, enemyPlayer;
    List<ALPlayer> orderedPlayers = [];
    int playerIndexPlayingTurn = 1; // First to start

    // --- State ---
    ALCard attackerCard, attackedCard;
    ALPlayerUI playerUI;
    ALDebug debug;
    ALInteraction interaction;
    ISelectionSyncTest selectionSyncTest;

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
        orderedPlayers = [userPlayer, enemyPlayer]; // TODO add some shuffling, with a minigame  also online
        playerIndexPlayingTurn = Multiplayer.IsServer() ? 0 : 1;
        // ! Needs 

        ALHand userHand = userPlayer.GetPlayerHand<ALHand>();
        ALHand enemyHand = enemyPlayer.GetPlayerHand<ALHand>();
        ALBoard userBoard = userPlayer.GetPlayerBoard<ALBoard>();
        ALBoard enemyBoard = enemyPlayer.GetPlayerBoard<ALBoard>();

        // ! NOTE: This only works thinking that always the player is down and enemy is up
        // IsEnemyBoard is needed for thinks like flipping the Input axis
        enemyHand.SetIsEnemyBoard(true);
        enemyBoard.SetIsEnemyBoard(true);

        // Assign Enemy boards is needed to handle onBoardEdges
        userPlayer.AssignEnemyBoards(enemyHand, enemyBoard);
        enemyPlayer.AssignEnemyBoards(userHand, userBoard);

        orderedPlayers.ForEach(player =>
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

        });
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
        ALNetwork.Instance.OnSendSelectCardEvent -= HandleOnCardSelectEvent;
        ALNetwork.Instance.OnSendSelectCardEvent += HandleOnCardSelectEvent;
        ALNetwork.Instance.OnSendInputActionEvent -= HandleOnInputActionEvent;
        ALNetwork.Instance.OnSendInputActionEvent += HandleOnInputActionEvent;

        Callable.From(StartMatchForPlayer).CallDeferred();
        Callable.From(TryStartSelectionSyncTest).CallDeferred();
    }

    async void HandleOnSendMatchPhaseEvent(int peerId, int phase)
    {
        GD.Print($"[HandleOnSendMatchPhaseEvent] To {userPlayer.MultiplayerId}: From {peerId}: {phase}");
        matchCurrentPhase = (EALTurnPhase)phase;
        await Task.CompletedTask;
    }
    async void HandleOnSendPlayStateEvent(int peerId, EPlayState state, string interactionState)
    {
        ALPlayer affectedPlayer = userPlayer.MultiplayerId == peerId ? userPlayer : enemyPlayer;
        // ALPlayer affectedPlayer = GetPlayerPlayingTurn();
        GD.Print($"[HandleOnSendPlayStateEvent] To {userPlayer.MultiplayerId}: Update {peerId} - {state} - {interactionState}");
        await affectedPlayer.SetPlayState(state, interactionState, false);
    }
    async void HandleOnSyncFlagship(int peerId, string cardId)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnSyncFlagship] To {userPlayer.MultiplayerId}: From {peerId}: {synchedCard.name}");
        enemyPlayer.UpdateFlagship(synchedCard);
        await Task.CompletedTask;
    }
    async void HandleOnDrawCardEvent(int peerId, string cardId, ALDrawType drawType)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnDrawCardEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {synchedCard.name} - {drawType}");
        switch (drawType)
        {
            case ALDrawType.Deck:
                enemyPlayer.DrawFromDeck();
                await enemyPlayer.AddCardToHand(synchedCard);
                break;
            case ALDrawType.Cube:
                enemyPlayer.DrawFromCubeDeck();
                await enemyPlayer.PlaceCubeToBoard(synchedCard);
                break;
            case ALDrawType.Durability:
                enemyPlayer.DrawFromDeck();
                await enemyPlayer.PlaceDurabilityCard(synchedCard);
                break;
        }
    }
    async void HandleOnCardSelectEvent(int peerId, int targetOwnerPeerId, string boardName, Vector2I position)
    {
        GD.Print($"[HandleOnCardSelectEvent] To {userPlayer.MultiplayerId}: From {peerId}: owner={targetOwnerPeerId} -> {boardName} - {position}");
        if (peerId == userPlayer.MultiplayerId)
        {
            GD.Print($"[HandleOnCardSelectEvent] Ignore self selection from {peerId}.");
            return;
        }

        ALPlayer selectingPlayer = ResolveSelectingPlayer(peerId);
        if (selectingPlayer is null)
        {
            GD.PrintErr($"[HandleOnCardSelectEvent] Unknown peerId {peerId}. User={userPlayer.MultiplayerId} Enemy={enemyPlayer.MultiplayerId}");
            return;
        }
        ALPlayer targetOwner = ResolveSelectingPlayer(targetOwnerPeerId);
        if (targetOwner is null)
        {
            GD.PrintErr($"[HandleOnCardSelectEvent] Unknown targetOwnerPeerId {targetOwnerPeerId}. User={userPlayer.MultiplayerId} Enemy={enemyPlayer.MultiplayerId}");
            return;
        }
        Board board = ResolveOwnerBoard(targetOwner, boardName);
        GD.Print($"[HandleOnCardSelectEvent.Resolve] selector={selectingPlayer.Name}({selectingPlayer.MultiplayerId}) owner={targetOwner.Name}({targetOwner.MultiplayerId}) board={board.Name} localEnemy={board.GetIsEnemyBoard()} pos={position}");
        board.SelectCardField(selectingPlayer, position, false);
        selectingPlayer.SelectBoard(selectingPlayer, board);
        await Task.CompletedTask;
    }

    Board ResolveOwnerBoard(ALPlayer ownerPlayer, string boardName)
    {
        if (boardName == "Board")
        {
            Board board = ownerPlayer.GetPlayerBoard<PlayerBoard>();
            if (board is null)
            {
                throw new System.InvalidOperationException($"[ResolveOwnerBoard] Board '{boardName}' not found for player {ownerPlayer.Name}.");
            }
            return board;
        }
        if (boardName == "Hand")
        {
            Board board = ownerPlayer.GetPlayerHand<PlayerHand>();
            if (board is null)
            {
                throw new System.InvalidOperationException($"[ResolveOwnerBoard] Board '{boardName}' not found for player {ownerPlayer.Name}.");
            }
            return board;
        }
        throw new System.InvalidOperationException($"[ResolveOwnerBoard] Unknown board name '{boardName}'.");
    }

    ALPlayer ResolveSelectingPlayer(int peerId)
    {
        if (userPlayer.MultiplayerId == peerId) return userPlayer;
        if (enemyPlayer.MultiplayerId == peerId) return enemyPlayer;
        return null;
    }

    async void HandleOnInputActionEvent(int peerId, InputAction inputAction)
    {
        GD.Print($"[HandleOnInputActionEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {inputAction}");
        enemyPlayer.TriggerAction(enemyPlayer, inputAction, false);
        await Task.CompletedTask;
    }
    async void HandleOnTurnEndEvent(int peerId)
    {
        if (GetPlayerPlayingTurn() == userPlayer || peerId == Network.Instance.Multiplayer.GetUniqueId())
        {
            GD.PushError("[HandleOnTurnEndEvent] Another peer is trying to end their turn");
            return;
        }
        GD.Print($"[HandleOnTurnEndEvent] To {userPlayer.MultiplayerId}: From {peerId}: Finishes its turn");
        PickNextPlayerToPlayTurn().StartTurn();
        await Task.CompletedTask;
    }

    bool ShouldSkipAutoPhasesForTest()
    {
        return debug.GetSelectionSyncTestEnabled();
    }

    // Match
    async void StartMatchForPlayer()
    {
        await AssignDeckSet();
        if (ShouldSkipAutoPhasesForTest())
        {
            foreach (var player in orderedPlayers)
            {
                player.Phase.SetSkipAutoPhases(true);
            }
        }
        await userPlayer.StartGameForPlayer(userPlayer.GetDeckSet());
        //await enemyPlayer.StartGameForPlayer(enemyPlayer.GetDeckSet());
        if (Multiplayer.IsServer())
        {
            if (ShouldSkipAutoPhasesForTest())
            {
                await GetPlayerPlayingTurn().Phase.ForceMainPhase(true);
                foreach (var player in orderedPlayers)
                {
                    if (player == GetPlayerPlayingTurn())
                    {
                        continue;
                    }
                    await player.Phase.ForceMainPhase(false);
                }
            }
            else
            {
                GetPlayerPlayingTurn().StartTurn();
            }
        }
        // if (!GetNextPlayer().GetIsControllerPlayer()) Callable.From(GetNextPlayer().GetPlayerAIController().StartTurn).CallDeferred();
    }

    void TryStartSelectionSyncTest()
    {
        if (!debug.GetSelectionSyncTestEnabled())
        {
            return;
        }
        string selectionClass = GetSelectionSyncTestClass();
        if (string.Equals(selectionClass, "Simul", System.StringComparison.OrdinalIgnoreCase))
        {
            selectionSyncTest ??= new ALSelectionSyncSimulTest(this, debug.GetSelectionSyncStepSeconds());
        }
        else
        {
            selectionSyncTest ??= new ALSelectionSyncTest(this, debug.GetSelectionSyncStepSeconds());
        }
        _ = selectionSyncTest.Run();
    }

    static string GetSelectionSyncTestClass()
    {
        string value = GetCommandLineValue("--selection-sync-test-class");
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Default";
        }
        return value;
    }

    static string GetCommandLineValue(string key)
    {
        string value = GetCommandLineValueFromArgs(OS.GetCmdlineUserArgs(), key);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return GetCommandLineValueFromArgs(OS.GetCmdlineArgs(), key);
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
        if (attackedPlayer is not ALPlayer)
        {
            GD.PrintErr($"[OnAttackGuardStartHandler] {attackedPlayer.GetType()} needs to be an ALPlayer instance.");
            return;
        }
        await attackerPlayer.SetPlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        await attackedPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
        GD.Print($"[OnAttackGuardStartHandler]");
    }

    async Task OnAttackGuardEndHandler(Player guardingPlayer)
    {
        await guardingPlayer.SetPlayState(EPlayState.Wait);
        ALPlayer attackerPlayer = GetAttackerCard().GetOwnerPlayer<ALPlayer>();
        await attackerPlayer.SetPlayState(EPlayState.Wait);
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsAttacked);
        await attackerPlayer.SettleBattle(playerUI);
        GD.Print($"[OnAttackGuardEndHandler]");
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

        await GetNextPlayer((ALPlayer)damagedPlayer).SetPlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }
    async Task OnRetaliationCancel(Player damagedPlayer)
    {
        await damagedPlayer.GoBackInHistoryState();
        await GetNextPlayer((ALPlayer)damagedPlayer).GoBackInHistoryState();
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
    }

    async Task OnAttackEndHandler(Player guardingPlayer)
    {
        foreach (var player in orderedPlayers)
        {
            await player.TryToExpireCardsModifierDuration(ALCardEffectDuration.CurrentBattle);
        }
        attackerCard = null;
        attackedCard = null;
        GD.Print($"[OnAttackEndHandler]");
    }

    void OnPhaseChangeHandler(EALTurnPhase phase)
    {
        matchCurrentPhase = phase;
    }

    async void OnTurnEndHandler()
    {
        await this.Wait(1f);
        ALPlayer playingPlayer = GetPlayerPlayingTurn();
        GD.Print($"[OnTurnEndHandler] {playingPlayer.Name} Turn ended!");

        await playingPlayer.TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        await GetNextPlayer().TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        await this.Wait(1f);
        ALNetwork.Instance.SendTurnEnd();
        PickNextPlayerToPlayTurn();
        // if (!GetPlayerPlayingTurn().GetIsControllerPlayer()) GetPlayerPlayingTurn().GetPlayerAIController().StartTurn();
    }

    ALPlayer PickNextPlayerToPlayTurn()
    {
        playerIndexPlayingTurn = GetNextPlayerIndex(playerIndexPlayingTurn);
        return GetPlayerPlayingTurn();
    }

    int GetNextPlayerIndex(int fromPlayerIndex) => orderedPlayers.Count.ApplyCircularBounds(fromPlayerIndex + 1);
    int FindIndexForPlayer(ALPlayer player) => orderedPlayers.FindIndex(orderedPlayer => orderedPlayer == player);
    ALPlayer GetNextPlayer(ALPlayer fromPlayer = null)
    {
        var currentPlayer = fromPlayer is null ? GetPlayerPlayingTurn() : fromPlayer;
        return orderedPlayers[GetNextPlayerIndex(FindIndexForPlayer(currentPlayer))];
    }

    async Task AssignDeckSet()
    {
        var userPlayerDeckSetId = Multiplayer.IsServer() ? "SD03" : "SD02";
        userPlayer.AssignDeck(BuildDeckSet(userPlayerDeckSetId));
        ALNetwork.Instance.SendDeckSet(userPlayerDeckSetId);
        foreach (var player in orderedPlayers)
        {
            await player.GetAsyncHandler().AwaitForCheck(null, player.HasValidDeck, -1);
        }
    }

    // ----- API -----
    public ALPlayerUI GetPlayerUI() => playerUI;
    public List<ALPlayer> GetOrderedPlayers() => orderedPlayers;
    public ALPlayer GetPlayerPlayingTurn() => orderedPlayers[playerIndexPlayingTurn];
    public ALPlayer GetControlledPlayer() => orderedPlayers.Find(player => player.GetIsControllerPlayer());
    public EALTurnPhase GetMatchPhase() => matchCurrentPhase;
    public ALDatabase GetDatabase() => database;
    public bool IsAttackInProgress() => attackedCard is not null && attackerCard is not null;
    public ALPlayer GetUserPlayer() => userPlayer;
    public ALPlayer GetEnemyPlayer() => enemyPlayer;
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
        enemyPlayer.AssignDeck(BuildDeckSet(enemyDeckId));
    }
    public ALDebug GetDebug() => debug;
}
