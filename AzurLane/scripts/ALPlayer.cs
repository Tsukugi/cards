
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALPlayer : Player
{
    public event EnemyInteractionRequestEvent OnAttackGuardStart;

    // --- Events ---
    public event Action OnTurnEnd;

    // --- Refs ---
    private readonly List<ALCardDTO> Deck = [];
    private readonly List<ALCardDTO> CubeDeck = [];
    private readonly List<ALCardDTO> Retreat = [];
    ALCardDTO Flagship = new();
    ALGameMatchManager matchManager;
    ALDatabase database;

    // --- AI ---
    ALBasicAI ai;

    // --- Nodes --- 
    Node3D costArea, durabilityArea, unitsArea;
    [Export]
    ALCard deckField, cubeDeckField, flagshipField, retreatField;
    [Export]
    ALPhaseButton phaseButtonField;

    // --- State ---
    ALCard battleAttackerCard, battleAttackedCard;

    // --- Phase ---
    AsyncHandler playerAsyncHandler;
    ALPhase phaseManager;

    public override void _Ready()
    {
        base._Ready(); // Call it at the end as the overrided code can use the refs correctly
        matchManager = this.TryFindParentNodeOfType<ALGameMatchManager>();
        database = matchManager.GetDatabase();

        playerAsyncHandler = new(this);
        ai = new(this);
        phaseManager = new(this);

        ALBoard board = GetPlayerBoard<ALBoard>();
        costArea = board.GetNode<Node3D>("CostArea");
        unitsArea = board.GetNode<Node3D>("Units");
        durabilityArea = board.GetNode<Node3D>("FlagshipDurability");

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    protected override void InitializeEvents()
    {
        base.InitializeEvents();
        ALHand hand = GetPlayerHand<ALHand>();
        ALBoard board = GetPlayerBoard<ALBoard>();
        board.OnCardTrigger -= OnCardTriggerHandler;
        board.OnCardTrigger += OnCardTriggerHandler;
        hand.OnPlayCardStart -= OnPlayCardStartHandler; // Unload default event
        hand.OnPlayCardStart -= OnCostPlayCardStartHandler;
        hand.OnPlayCardStart += OnCostPlayCardStartHandler;
        board.OnPlaceCardCancel -= OnPlaceCardCancelHandler; // Unload default event
        board.OnPlaceCardCancel -= OnCostPlaceCardCancelHandler;
        board.OnPlaceCardCancel += OnCostPlaceCardCancelHandler;
        board.OnPlaceCardStart -= OnPlaceCardStartHandler;
        board.OnPlaceCardStart -= OnALPlaceCardStartHandler;
        board.OnPlaceCardStart += OnALPlaceCardStartHandler;
        List<ALCard> units = unitsArea.TryGetAllChildOfType<ALCard>();
        units.ForEach(unit =>
        {
            unit.OnDurabilityDamage -= OnDurabilityDamageHandler;
            unit.OnDurabilityDamage += OnDurabilityDamageHandler;
        });

        GD.Print("[InitializeEvents] ALPlayer events initialized");
    }

    protected override void UnassignBoardEvents(Board board)
    {
        base.UnassignBoardEvents(board);
        ALBoard enemyBoard = GetEnemyPlayerBoard<ALBoard>();
        if (enemyBoard is not null) enemyBoard.OnCardTrigger -= OnCardTriggerHandler;
    }
    protected override void AssignBoardEvents(Board board)
    {
        UnassignBoardEvents(board);
        base.AssignBoardEvents(board);
        ALBoard enemyBoard = GetEnemyPlayerBoard<ALBoard>();
        if (enemyBoard is not null) enemyBoard.OnCardTrigger += OnCardTriggerHandler;
    }

    void BuildDeck()
    {
        //TODO: Customize deck
        // Flagship
        Flagship = database.cards["SD01-001"];

        // Deck
        List<string> availableDeckKeys = [
            "SD01-002",
            "SD01-003",
            "SD01-004",
            "SD01-005",
            "SD01-006",
            "SD01-007",
            "SD01-008",
            "SD01-009",
            "SD01-010",
            "SD01-011",
            "SD01-012",
            "SD01-013",
            "SD01-014",
            "SD01-015",
            "SD01-016"
        ];
        for (int i = 0; i < 50; i++)
        {
            Deck.Add(database.cards[availableDeckKeys.GetRandKey()]);
        }

        // CubeDeck
        for (int i = 0; i < 10; i++)
        {
            CubeDeck.Add(database.cards["SD01-Cube"]);
        }
    }

    void StartGameForPlayer()
    {
        BuildDeck();

        // Deck setup
        deckField.CardStack = Deck.Count; // Set it as deck size
        deckField.UpdateAttributes(Deck[^1]); // Use last card as template
        // CubeDeck setup
        cubeDeckField.CardStack = CubeDeck.Count; // Set it as deck size
        cubeDeckField.UpdateAttributes(CubeDeck[^1]); // Use last card as template
        // Flagship setup
        flagshipField.UpdateAttributes(Flagship);

        // Player preparation
        ALHand hand = GetPlayerHand<ALHand>();
        DrawCardToHand(5);
        SelectBoard(hand);
        hand.SelectCardField(this, Vector2I.Zero);
        ApplyFlagshipDurability(); // Manual says that this step is after drawing hand cards
    }

    // Turn and Phases
    public void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");
        if (!GetIsControllerPlayer()) _ = ai.StartTurn();
        phaseManager.StartTurn();
    }

    public void EndTurn()
    {
        if (OnTurnEnd is not null) OnTurnEnd();
    }

    // Event handlers

    public void OnCostPlayCardStartHandler(Card cardToPlay)
    {
        var currentPhase = phaseManager.GetCurrentPhase();
        if (currentPhase != EALTurnPhase.Main)
        {
            GD.PrintErr($"[OnCostPlayCardStartHandler] Cannot place cards outside main phase");
            return;
        }

        ALCard card = cardToPlay.CastToALCard();
        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();

        List<ALCard> activeCubes = GetActiveCubesInBoard();

        GD.Print($"[OnCostPlayCardStartHandler] Cost {attributes.cost} - Active cubes {activeCubes.Count}");

        if (attributes.cost > activeCubes.Count)
        {
            GD.PrintErr("[OnCostPlayCardStartHandler] Player doesn't have enough cubes to play this card");
            return;
        }

        for (int i = 0; i < attributes.cost; i++)
        {
            activeCubes[activeCubes.Count - 1 - i].SetIsInActiveState(false); // Last match
        }

        OnPlayCardStartHandler(card);
    }

    protected void OnCostPlaceCardCancelHandler(Card cardToRestore)
    {
        ALCard card = cardToRestore.CastToALCard();

        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();
        List<ALCard> cubes = GetCubesInBoard();
        GD.Print($"[OnCostPlaceCardCancelHandler] Reverting cubes spent for {attributes.name}");

        for (int i = 0; i < attributes.cost; i++)
        {
            cubes[cubes.Count - 1 - i].SetIsInActiveState(true); // Last match
        }

        OnPlaceCardCancelHandler(card);
    }

    protected void OnALPlaceCardStartHandler(Card fieldToPlace)
    {
        ALCard cardToPlace = fieldToPlace.CastToALCard();
        ALBoard board = GetPlayerBoard<ALBoard>();
        ALCard fieldBeingPlaced = board.GetSelectedCard<ALCard>(this);
        if (fieldBeingPlaced.GetIsAFlagship())
        {
            GD.PrintErr($"[OnALPlaceCardStartHandler] You cannot place cards in a flagship field");
            return;
        }
        if (!fieldBeingPlaced.IsEmptyField)
        {
            ALCardDTO existingCard = fieldBeingPlaced.GetAttributes<ALCardDTO>();
            GD.Print($"[OnALPlaceCardStartHandler] Sending {existingCard} to retreat area");
            AddToRetreatAreaOnTop(existingCard);
        }
        OnPlaceCardStartHandler(cardToPlace);
    }

    protected override void OnCardTriggerHandler(Card field)
    {
        playerAsyncHandler.Debounce(() =>
        {
            base.OnCardTriggerHandler(field);
            EALTurnPhase currentPhase = phaseManager.GetCurrentPhase();
            if (field is ALPhaseButton)
            {
                if (currentPhase == EALTurnPhase.Main) phaseManager.PlayNextPhase();
                if (currentPhase == EALTurnPhase.Battle) phaseManager.PlayNextPhase();
            }
            if (field is ALCard card)
            {
                if (currentPhase == EALTurnPhase.Battle && GetPlayState() == EPlayState.Select) StartBattle(card);
                if (currentPhase == EALTurnPhase.Battle && GetPlayState() == EPlayState.SelectTarget) AttackCard(battleAttackerCard, card);
                if (currentPhase == EALTurnPhase.Battle && GetPlayState() == EPlayState.EnemyInteraction) PlayCardAsGuard(card, battleAttackedCard);
            }
        }, 1f);
    }

    static void PlayCardAsGuard(ALCard cardToGuard, ALCard battleAttackedCard)
    {
        if (!cardToGuard.GetIsInActiveState())
        {
            GD.PrintErr($"[OnDurabilityDamageHandler] Select a valid card, selected: {cardToGuard.Name}");
            return;
        };
        ALCardDTO attrs = cardToGuard.GetAttributes<ALCardDTO>();

        GD.Print($"[PlayCardAsGuard] Buff {battleAttackedCard.GetAttributes<ALCardDTO>().name} with {attrs.name}'s {attrs.supportValue}");

        // TODO make a buff for the attacked card

        cardToGuard.DestroyCard();
    }

    void OnDurabilityDamageHandler(Card card)
    {
        List<ALCard> durabilityCards = GetDurabilityCards();
        ALCard durabilityCard = durabilityCards.FindLast(durabilityCard => durabilityCard.GetIsFaceDown());
        if (durabilityCard is null)
        {
            GD.PrintErr($"[OnDurabilityDamageHandler] Game over for {Name}");
            return;
        }
        // "Draw" the card to hand 
        durabilityCard.DestroyCard(); // Destroy card from board
        ALHand hand = GetPlayerHand<ALHand>();
        hand.AddCardToHand(durabilityCard.GetAttributes<ALCardDTO>());
        GD.Print($"[OnDurabilityDamageHandler] {Name} takes damage, durability is {durabilityCards.FindAll(durabilityCard => durabilityCard.GetIsFaceDown())}/{durabilityCards.Count}");
    }

    // Actions 

    void StartBattle(ALCard attacker)
    {
        battleAttackerCard = attacker;
        SetPlayState(EPlayState.SelectTarget);
    }

    // Limitations on attack
    // BackRow ship -> FrontRow Ships
    // FrontRow Ship -> All ships 
    // Flagship -> All Ships
    void AttackCard(ALCard attacker, ALCard target)
    {
        if (attacker.GetBoard().GetIsEnemyBoard() == target.GetBoard().GetIsEnemyBoard())
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name} as they are allies!");
            return;
        }
        //  if Flagship, they can attack everyone
        bool canBeAttacked = attacker.GetIsAFlagship() || target.CanBeAttacked(attacker.GetAttackFieldType());
        if (!canBeAttacked)
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name}");
            return;
        }
        battleAttackedCard = target;

        battleAttackerCard.SetIsInActiveState(false);
        GD.Print($"[AttackCard] {battleAttackerCard.Name} attacks {battleAttackedCard}!");

        // Start guard phase    
        if (OnAttackGuardStart is not null) OnAttackGuardStart(this, target.GetBoard().TryFindParentNodeOfType<ALPlayer>()); // TODO: improve me to avoid node search
    }

    public async Task SettleBattle()
    {
        SetPlayState(EPlayState.Wait);

        ALCardDTO attackerAttrs = battleAttackerCard.GetAttributes<ALCardDTO>();
        ALCardDTO attackedAttrs = battleAttackedCard.GetAttributes<ALCardDTO>();
        bool isAttackSuccessful = attackerAttrs.power >= attackedAttrs.power;

        if (isAttackSuccessful)
        {
            if (battleAttackedCard.GetIsAFlagship())
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} Takes durability damage!");
                battleAttackedCard.TakeDurabilityDamage();
            }
            else
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} destroyed!");
                battleAttackedCard.DestroyCard();
            }
        }
        else
        {
            GD.PrintErr($"[SettleBattle] Attack from {attackerAttrs.name} did not go through");
        }

        // Clean
        SetPlayState(EPlayState.Select);
        battleAttackedCard = null;
        battleAttackerCard = null;
        await phaseManager.EndBattlePhaseIfNoActiveCards();
    }


    // Nodes

    static ALCardDTO DrawCard(List<ALCardDTO> deck, ALCard relatedField)
    {
        var res = Player.DrawCard(deck);
        UpdateDeckStackSize(relatedField, deck.Count);
        return res;
    }

    static void AddCardToDeck(ALCardDTO cardToAdd, List<ALCardDTO> deck, ALCard boardField, bool top = true)
    {
        if (top)
        {
            deck.Insert(0, cardToAdd);
        }
        else
        {
            deck.Add(cardToAdd);
            boardField.UpdateAttributes(cardToAdd); // Update image src to show bottom card 
        }
        UpdateDeckStackSize(boardField, deck.Count);
    }

    static void UpdateDeckStackSize(ALCard deck, int size)
    {
        deck.CardStack = size;
        if (deck.CardStack == 0) deck.IsEmptyField = true;
    }

    void AddToRetreatAreaOnTop(ALCardDTO cardToAdd)
    {
        // We add to the bottom as the deck works flipped down
        AddCardToDeck(cardToAdd, Retreat, retreatField, false);
    }

    void ApplyFlagshipDurability()
    {
        int durability = flagshipField.GetAttributes<ALCardDTO>().durability;
        List<ALCard> durabilityList = durabilityArea.TryGetAllChildOfType<ALCard>();
        for (int i = 0; i < durability; i++)
        {
            ALCardDTO cardToDraw = DrawCard(Deck, deckField);
            durabilityList[i].UpdateAttributes(cardToDraw);
            durabilityList[i].IsInputSelectable = true;
        }
    }

    // Public Player Actions for AI 
    public ALPhase Phase => phaseManager;
    public AsyncHandler GetPlayerAsyncHandler()
    {
        if (playerAsyncHandler is null) GD.PushError("[GetPlayerAsyncHandler] No asyncHandler is set");
        return playerAsyncHandler;
    }
    public ALBasicAI AI { get => ai; }

    public List<ALCard> GetActiveUnitsInBoard() => unitsArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetActiveCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetDurabilityCards() => durabilityArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.IsEmptyField);
    public List<ALCard> GetCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.IsEmptyField);

    public bool IsAwaitingBattleGuard() => phaseManager.GetCurrentPhase() == EALTurnPhase.Battle && GetPlayState() == EPlayState.AwaitEnemyInteraction && battleAttackedCard is not null && battleAttackerCard is not null;
    public ALCard FindAvailableEmptyFieldInRow(bool frontRow = false)
    {
        List<ALCard> fields = unitsArea.TryGetAllChildOfType<ALCard>();
        if (frontRow) return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.FrontRow && field.IsEmptyField);
        else return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.BackRow && field.IsEmptyField);
    }

    public string GetCurrentPhaseText() => phaseManager.GetPhaseByIndex((int)matchManager.GetMatchPhase());
    public EALTurnPhase GetCurrentPhase() => phaseManager.GetCurrentPhase();

    // Play actions
    public void TriggerPhaseButton()
    {
        ALBoard board = GetPlayerBoard<ALBoard>();
        SelectBoard(board);
        // Select phase button
        board.SelectCardField(this, phaseButtonField.PositionInBoard);
        TriggerAction(InputAction.Ok);
    }

    public void DrawCardToHand(int num = 1)
    {
        for (int i = 0; i < num; i++)
        {
            ALCardDTO cardToDraw = DrawCard(Deck, deckField);
            ALHand hand = GetPlayerHand<ALHand>();
            hand.AddCardToHand(cardToDraw);
        }
    }

    public void SetBoardCardsAsActive()
    {
        // We wanna only reset units and cost area cards in AzurLane TCG
        costArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
        unitsArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
    }

    public void TryDrawCubeToBoard()
    {
        try
        {
            ALCardDTO cardToDraw = DrawCard(CubeDeck, cubeDeckField);
            Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
                costArea.TryGetAllChildOfType<Card>()
            );
            cubeField.IsInputSelectable = true;
            GetPlayerBoard<ALBoard>()
                .GetCardInPosition<ALCard>(this, cubeField.PositionInBoard)
                .UpdateAttributes(cardToDraw);
        }
        catch (Exception e)
        {
            GD.Print(e); // Expected when cube deck is empty
            return;
        }
    }

}

public enum EALTurnPhase
{
    Reset,
    Preparation,
    Main,
    Battle,
    End
}
