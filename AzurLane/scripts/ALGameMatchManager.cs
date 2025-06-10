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

    public override void _Ready()
    {
        base._Ready();

        userPlayer.MultiplayerId = Network.Instance.Multiplayer.GetUniqueId();
        ALNetwork.Instance.OnMatchStart();
        debug = new(this);
        interaction = new(this);

        playerUI = GetNode<ALPlayerUI>("Control");
        playerUI.SetPlayer(userPlayer); // Assign the controlling player

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
        // ALNetwork.Instance.OnSyncPlaceCard -= HandleOnPlaceCardEvent;
        // ALNetwork.Instance.OnSyncPlaceCard += HandleOnPlaceCardEvent;
        // ALNetwork.Instance.OnSyncPlaceCardGuard -= HandleOnGuardEvent;
        // ALNetwork.Instance.OnSyncPlaceCardGuard += HandleOnGuardEvent;
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
    }

    async void HandleOnSendMatchPhaseEvent(int peerId, int phase)
    {
        GD.Print($"[HandleOnSendMatchPhaseEvent] {peerId}: {phase}");
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
        GD.Print($"[HandleOnSyncFlagship] {peerId}: {synchedCard.name}");
        enemyPlayer.UpdateFlagship(synchedCard);
        await Task.CompletedTask;
    }
    async void HandleOnDrawCardEvent(int peerId, string cardId, ALDrawType drawType)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnDrawCardEvent] {peerId} -> {synchedCard.name} - {drawType}");
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
    async void HandleOnPlaceCardEvent(int peerId, string cardId, Board board, Vector2I position)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnPlaceCardEvent] {peerId} -> {synchedCard.name}");
        board.SelectCardField(enemyPlayer, position);
        await enemyPlayer.OnALPlaceCardStartHandler(board.GetSelectedCard<ALCard>(enemyPlayer), false);
    }
    async void HandleOnGuardEvent(int peerId, string cardId, Board board, Vector2I position)
    {
        ALCardDTO synchedCard = database.cards[cardId];
        GD.Print($"[HandleOnGuardEvent] {peerId} -> {synchedCard.name}");
        board.SelectCardField(enemyPlayer, position);
        await enemyPlayer.PlayCardAsGuard(board.GetSelectedCard<ALCard>(enemyPlayer), false);
    }
    async void HandleOnCardSelectEvent(int peerId, string boardName, bool isEnemyBoard, Vector2I position)
    {
        GD.Print($"[HandleOnCardSelectEvent] {peerId} -> {boardName} - {isEnemyBoard} - {position}");

        ALPlayer affectedPlayer = isEnemyBoard ? userPlayer : enemyPlayer;
        Board board = affectedPlayer.GetNode<Board>(boardName);
        affectedPlayer.SelectBoard(affectedPlayer, board);
        board.SelectCardField(affectedPlayer, position, false);
        await Task.CompletedTask;
    }
    async void HandleOnInputActionEvent(int peerId, InputAction inputAction)
    {
        GD.Print($"[HandleOnInputActionEvent] {peerId} -> {inputAction}");
        //   enemyPlayer.TriggerAction(enemyPlayer, inputAction, false);
        // TODO This triggers many things that affect play state
        await Task.CompletedTask;
    }
    async void HandleOnTurnEndEvent(int peerId)
    {
        if (GetPlayerPlayingTurn() == userPlayer || peerId == Network.Instance.Multiplayer.GetUniqueId())
        {
            GD.PushError("[HandleOnTurnEndEvent] Another peer is trying to end their turn");
            return;
        }
        GD.Print($"[HandleOnTurnEndEvent] {peerId} Finishes its turn");
        PickNextPlayerToPlayTurn().StartTurn();
        await Task.CompletedTask;
    }

    // Match
    async void StartMatchForPlayer()
    {
        await AssignDeckSet();
        await userPlayer.StartGameForPlayer(userPlayer.GetDeckSet());
        //await enemyPlayer.StartGameForPlayer(enemyPlayer.GetDeckSet());
        if (Multiplayer.IsServer()) GetPlayerPlayingTurn().StartTurn();
        // if (!GetNextPlayer().GetIsControllerPlayer()) Callable.From(GetNextPlayer().GetPlayerAIController().StartTurn).CallDeferred();
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
        damagedPlayer.SelectBoard(damagedPlayer, damagedPlayer.GetPlayerHand<ALHand>());
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