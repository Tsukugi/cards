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
    ALPlayerUI playerUI;
    ALDebug debug;

    public override void _Ready()
    {
        base._Ready();

        debug = new(this);

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

        Callable.From(StartMatchForPlayer).CallDeferred();
    }

    void StartMatchForPlayer()
    {
        userPlayer.StartGameForPlayer();
        enemyPlayer.StartGameForPlayer();
        Callable.From(GetPlayerPlayingTurn().StartTurn).CallDeferred();
    }

    void OnSkipInteractionHandler(Player player, Board triggeringBoard)
    {
        player.SetPlayState(EPlayState.Wait);
        ALPlayer playingPlayer = GetPlayerPlayingTurn();
        if (playingPlayer.IsAwaitingBattleGuard()) _ = playingPlayer.SettleBattle(playerUI);
    }

    void OnAttackStartHandler(Player guardingPlayer, Card card)
    {
        attackerCard = card.CastToALCard();
        GetAttackerCard().TryToTriggerCardEffect(ALCardEffectTrigger.StartsAttack);
        GD.Print($"[OnAttackStartHandler] {GetAttackerCard().Name} starts an attack!");
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
        GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsAttacked);
        _ = attackerPlayer.SettleBattle(playerUI);
        GD.Print($"[OnAttackGuardEndHandler]");
    }

    void OnGuardProvidedHandler(Player guardingPlayer, Card card)
    {
        GetAttackedCard().AddModifier(new AttributeModifier()
        {
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = card.GetAttributes<ALCardDTO>().supportValue,
        });
        GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsBattleSupported);
        GD.Print($"[OnGuardProvidedHandler] Add Guard Modifier for {GetAttackedCard().GetAttributes<ALCardDTO>().name}");
    }

    void OnAttackEndHandler(Player guardingPlayer)
    {
        GetAttackerCard().TryToExpireEffectOrModifier(ALCardEffectDuration.CurrentBattle);
        attackedCard?.TryToExpireEffectOrModifier(ALCardEffectDuration.CurrentBattle); // May be destroyed at this point
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