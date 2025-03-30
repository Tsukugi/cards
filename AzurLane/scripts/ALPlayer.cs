
using System;
using System.Collections.Generic;
using Godot;

public partial class ALPlayer : Player
{

    // --- Events ---
    public delegate void PhaseEvent(EALTurnPhase phase);
    public event Action OnTurnEnd;
    public event PhaseEvent OnPhaseChange;

    // --- Refs ---
    List<ALCardDTO> Deck = [], CubeDeck = [], Retreat = [];
    ALCardDTO Flagship = new();
    readonly ALDatabase database = new();

    // --- AI ---
    ALBasicAI ai;

    // --- Nodes --- 
    new ALBoard board;
    new ALHand hand;
    Node3D costArea, durabilityArea, unitsArea;
    [Export]
    ALCard deckField, cubeDeckField, flagshipField, retreatField;
    [Export]
    ALPhaseButton phaseButtonField;

    // --- State ---
    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    // State updated externally, represents the synched phase running between all players
    EALTurnPhase synchedPhase = EALTurnPhase.Reset;
    ALCard battleAttackerCard, battleAttackedCard;

    // --- UI --- 
    Control control;
    Panel selectedCardInfo;
    Label phaseLabel, selectedCardNameLabel;
    TextureRect selectedCardImage;

    // --- Phase ---
    AsyncHandler asyncPhase;
    readonly ALPhase phase = new();

    public override void _Ready()
    {
        base._Ready();
        ai = new(this);
        asyncPhase = new(this);
        hand = GetNode<ALHand>("Hand");
        board = GetNode<ALBoard>("Board");
        costArea = board.GetNode<Node3D>("CostArea");
        unitsArea = board.GetNode<Node3D>("Units");
        durabilityArea = board.GetNode<Node3D>("FlagshipDurability");
        control = GetNode<Control>("Control");
        phaseLabel = GetNode<Label>("Control/PhaseLabel");
        selectedCardInfo = GetNode<Panel>("Control/SelectedCardInfo");
        selectedCardImage = GetNode<TextureRect>("Control/SelectedCardInfo/SelectedCardImage");
        selectedCardNameLabel = GetNode<Label>("Control/SelectedCardInfo/NamePanel/NameLabel");

        selectedCardInfo.Visible = isControlledPlayer; // Hide it if not the controlled player

        InitializeEvents();

        database.LoadData();

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (!isControlledPlayer) return;
        phaseLabel.Text = phase.GetPhaseByIndex((int)synchedPhase);

        if (selectedBoard.GetSelectedCard<ALCard>() is ALCard selectedCard)
        {
            bool CanShowCardDetailsUI = selectedCard.CanShowCardDetailsUI();
            selectedCardInfo.Visible = CanShowCardDetailsUI;
            if (CanShowCardDetailsUI)
            {
                selectedCardImage.Texture = (Texture2D)selectedCard.GetCardImageResource();
                selectedCardNameLabel.Text = selectedCard.GetAttributes<ALCardDTO>().name;
            }
        }
        else
        {
            selectedCardInfo.Visible = false;
        }
    }

    protected new void InitializeEvents()
    {
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
        GD.Print("[InitializeEvents] ALPlayer events initialized");
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
            "SD01-008"
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
        SelectBoard(hand);
        hand.SelectCardField(Vector2I.Zero);
        DrawCardToHand(5);
        ApplyFlagshipDurability(); // Manual says that this step is after drawing hand cards

    }

    // Turn and Phases
    public void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");

        if (!isControlledPlayer) _ = ai.SkipTurn(); // TODO: Make a proper handler for proper AI
        IsPlayingTurn = true;
        PlayResetPhase();
    }
    void PlayResetPhase()
    {
        SetPlayState(EPlayState.Wait);
        // Reset all Units into active state
        GD.Print($"[{Name}.PlayResetPhase]");
        SetBoardCardsAsActive();
        UpdatePhase(EALTurnPhase.Reset);
        asyncPhase.AwaitBefore(PlayNextPhase);
    }
    void PlayPreparationPhase()
    {
        SetPlayState(EPlayState.Wait);
        // Draw 1 card
        // Place 1 face up cube if possible
        GD.Print($"[{Name}.PlayPreparationPhase]");
        TryDrawCubeToBoard();
        DrawCardToHand();
        UpdatePhase(EALTurnPhase.Preparation);
        PlayNextPhase();
    }
    void PlayMainPhase()
    {
        // Player can play cards
        GD.Print($"[{Name}.PlayMainPhase]");
        SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Main);
    }
    void PlayBattlePhase()
    {
        // Player can declare attacks
        GD.Print($"[{Name}.PlayBattlePhase]");
        SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Battle);
    }
    void PlayEndPhase()
    {
        SetPlayState(EPlayState.Wait);
        // Clean some things
        GD.Print($"[{Name}.PlayEndPhase]");
        UpdatePhase(EALTurnPhase.End);
        asyncPhase.AwaitBefore(EndTurn);
    }

    void EndTurn()
    {
        IsPlayingTurn = false;
        if (OnTurnEnd is not null) OnTurnEnd();
    }

    void ApplyPhase(EALTurnPhase phase)
    {
        switch (phase)
        {
            case EALTurnPhase.Reset: PlayResetPhase(); return;
            case EALTurnPhase.Preparation: PlayPreparationPhase(); return;
            case EALTurnPhase.Main: PlayMainPhase(); return;
            case EALTurnPhase.Battle: PlayBattlePhase(); return;
            case EALTurnPhase.End: PlayEndPhase(); return;
        }
    }

    void PlayNextPhase()
    {
        EALTurnPhase nextPhase = currentPhase + 1;
        if (nextPhase > EALTurnPhase.End)
        {
            GD.PrintErr($"[PlayNextPhase] Trying to play next phase on End phase already!");
            return;
        }
        asyncPhase.AwaitBefore(() => ApplyPhase(nextPhase));
    }


    // Event handlers
    protected void OnCostPlayCardStartHandler(Card cardToPlay)
    {
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
        ALCard fieldBeingPlaced = board.GetSelectedCard<ALCard>();

        if (!fieldBeingPlaced.IsEmptyField)
        {
            ALCardDTO existingCard = fieldBeingPlaced.GetAttributes<ALCardDTO>();
            GD.Print($"[OnALPlaceCardStartHandler] Sending {existingCard} to retreat area");
            AddToRetreatAreaOnTop(existingCard);
        }
        OnPlaceCardStartHandler(cardToPlace);
    }

    protected void OnCardTriggerHandler(Card card)
    {
        // Ignore repeated triggers 
        asyncPhase.Debounce(() =>
        {
            if (card is ALPhaseButton)
            {
                if (currentPhase == EALTurnPhase.Main) PlayNextPhase();
                if (currentPhase == EALTurnPhase.Battle) PlayNextPhase();
            }
            if (card is ALCard alCard)
            {
                if (currentPhase == EALTurnPhase.Battle && GetPlayState() == EPlayState.Select) StartBattle(alCard);
                if (currentPhase == EALTurnPhase.Battle && GetPlayState() == EPlayState.SelectTarget) AttackCard(battleAttackerCard, alCard);
            }
        }, 1f);
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
        bool canBeAttacked = target.CanBeAttacked(attacker.GetAttackFieldType(), attacker.GetIsAFlagship());
        if (!canBeAttacked)
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name}");
            return;
        }
        battleAttackedCard = target;

        // TODO: Add support
    }

    void SettleBattle()
    {
        bool isAttackSuccessful = battleAttackerCard.GetAttributes<ALCardDTO>().power >= battleAttackedCard.GetAttributes<ALCardDTO>().power;

        if (isAttackSuccessful)
        {
            if (battleAttackedCard.GetIsAFlagship())
            {
                battleAttackedCard.GetBoard().GetPlayer();
            }
        }
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

    void DrawCardToHand(int num = 1)
    {
        for (int i = 0; i < num; i++)
        {
            ALCardDTO cardToDraw = DrawCard(Deck, deckField);
            hand.AddCardToHand(cardToDraw);
        }
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
        }

    }

    void TryDrawCubeToBoard()
    {
        try
        {
            ALCardDTO cardToDraw = DrawCard(CubeDeck, cubeDeckField);
            Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
                costArea.TryGetAllChildOfType<Card>()
            );
            board
                .GetCardInPosition(cubeField.PositionInBoard)
                .UpdateAttributes(cardToDraw);
        }
        catch (Exception e)
        {
            GD.Print(e); // Expected when cube deck is empty
            return;
        }
    }

    void SetBoardCardsAsActive()
    {
        // We wanna only reset units and cost area cards in AzurLane TCG
        costArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
        unitsArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
    }

    List<ALCard> GetActiveCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    List<ALCard> GetCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.IsEmptyField);

    void UpdatePhase(EALTurnPhase phase)
    {
        currentPhase = phase;
        if (OnPhaseChange is not null) OnPhaseChange(phase);
    }

    // Public Player Actions for AI 
    public void TriggerPhaseButton()
    {
        SelectBoard(board);
        // Select phase button
        board.SelectCardField(phaseButtonField.PositionInBoard);
        // Execute trigger handler directly
        OnCardTriggerHandler(phaseButtonField);
    }
    public EALTurnPhase GetCurrentPhase() => currentPhase;
    public EALTurnPhase SyncPhase(EALTurnPhase phase) => synchedPhase = phase;
}

public enum EALTurnPhase
{
    Reset,
    Preparation,
    Main,
    Battle,
    End
}
