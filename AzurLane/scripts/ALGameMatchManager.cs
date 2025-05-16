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

        debug = new(this);
        interaction = new(this);

        playerUI = GetNode<ALPlayerUI>("Control");
        playerUI.SetPlayer(userPlayer); // Assign the controlling player

        database.LoadData();

        // --- Players --- 
        orderedPlayers = [enemyPlayer, userPlayer]; // TODO add some shuffling, with a minigame

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

        userPlayer.AssignDeck(BuildDeckSet("SD01"));
        enemyPlayer.AssignDeck(BuildDeckSet("SD02"));

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

        Callable.From(StartMatchForPlayer).CallDeferred();
    }

    async void StartMatchForPlayer()
    {
        await userPlayer.StartGameForPlayer();
        await enemyPlayer.StartGameForPlayer();
        Callable.From(GetPlayerPlayingTurn().StartTurn).CallDeferred();
        if (!GetNextPlayer().GetIsControllerPlayer()) Callable.From(GetNextPlayer().GetPlayerAIController().StartTurn).CallDeferred();
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
        damagedPlayer.SelectBoard(damagedPlayer.GetPlayerHand<ALHand>());
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
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = card.GetAttributes<ALCardDTO>().supportValue,
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

        PickNextPlayer().StartTurn();
        await playingPlayer.TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        await GetNextPlayer().TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        if (!GetPlayerPlayingTurn().GetIsControllerPlayer()) _ = GetPlayerPlayingTurn().GetPlayerAIController().StartTurn();
    }

    ALPlayer PickNextPlayer()
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

    // ----- API -----
    public ALPlayerUI GetPlayerUI() => playerUI;
    public ALPlayer GetPlayerPlayingTurn() => orderedPlayers[playerIndexPlayingTurn];
    public ALPlayer GetControlledPlayer() => orderedPlayers.Find(player => player.GetIsControllerPlayer());
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

    public ALDebug GetDebug() => debug;
}