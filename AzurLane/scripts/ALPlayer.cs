
using System;
using System.Collections.Generic;
using Godot;

public partial class ALPlayer : Player
{
    public List<ALCardDTO> Deck = [], CubeDeck = [], Retreat = [];
    public ALCardDTO Flagship = new();
    readonly ALDatabase database = new();

    new ALBoard board;
    new ALHand hand;
    Control control;
    Label phaseLabel;
    Node3D costArea, durabilityArea, unitsArea;
    ALCard deckNode, cubeDeckNode, flagshipNode, retreatNode;

    AsyncHandler asyncPhase;

    readonly ALPhase phase = new();

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    public override void _Ready()
    {
        base._Ready();
        asyncPhase = new(this);
        hand = GetNode<ALHand>("Hand");
        board = GetNode<ALBoard>("Board");
        costArea = board.GetNode<Node3D>("CostArea");
        unitsArea = board.GetNode<Node3D>("Units");
        control = GetNode<Control>("Control");
        phaseLabel = GetNode<Label>("Control/PhaseLabel");
        durabilityArea = board.GetNode<Node3D>("FlagshipDurability");
        InitializeEvents();

        database.LoadData();

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        phaseLabel.Text = phase.GetPhaseByIndex((int)currentPhase);
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
        SetPlayState(EPlayState.Wait);
        BuildDeck();

        // Deck setup
        deckNode = board.GetCardInPosition(new Vector2I(3, 1)); // Deck position
        deckNode.CardStack = Deck.Count; // Set it as deck size
        deckNode.UpdateAttributes(Deck[^1]); // Use last card as template
        // CubeDeck setup
        cubeDeckNode = board.GetCardInPosition(new Vector2I(-1, 2)); // Cube deck position
        cubeDeckNode.CardStack = CubeDeck.Count; // Set it as deck size
        cubeDeckNode.UpdateAttributes(CubeDeck[^1]); // Use last card as template
        // Flagship setup
        flagshipNode = board.GetCardInPosition(Vector2I.One); // Flagship position
        flagshipNode.UpdateAttributes(Flagship);
        // Retreat setup
        retreatNode = board.GetCardInPosition(new Vector2I(10, 2)); // Retreat node position

        // Player preparation
        SelectBoard(hand);
        hand.SelectCardField(Vector2I.Zero);
        DrawCardToHand(5);
        ApplyFlagshipDurability(); // Manual says that this step is after drawing hand cards

        StartTurn();
    }

    // Turn and Phases

    void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");
        PlayResetPhase();
    }
    void PlayResetPhase()
    {
        currentPhase = EALTurnPhase.Reset;

        // Reset all Units into active state
        SetBoardCardsAsActive();
        asyncPhase.AwaitBefore(PlayNextPhase);
    }
    void PlayPreparationPhase()
    {
        // Draw 1 card
        // Place 1 face up cube if possible
        TryDrawCubeToBoard();
        DrawCardToHand();
        PlayNextPhase();
    }
    void PlayMainPhase()
    {
        // Player can play cards
        GD.Print($"[PlayMainPhase]");
        currentPhase = EALTurnPhase.Main;
        SelectBoard(hand);
        hand.SelectCardField(Vector2I.Zero);
        SetPlayState(EPlayState.Select);
    }
    void PlayBattlePhase()
    {
        // Player can declare attacks
        GD.Print($"[PlayBattlePhase]");
        currentPhase = EALTurnPhase.Battle;
        SetPlayState(EPlayState.Select);
    }
    void PlayEndPhase()
    {
        // Clean some things
        GD.Print($"[PlayEndPhase]");
        asyncPhase.AwaitBefore(EndTurn);
    }

    void EndTurn()
    {
        // TODO Switch to the other player
        StartTurn();
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
        currentPhase = nextPhase;
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
        if (card is ALPhaseButton)
        {
            if (currentPhase == EALTurnPhase.Main) asyncPhase.Debounce(PlayNextPhase);
            if (currentPhase == EALTurnPhase.Battle) asyncPhase.Debounce(PlayNextPhase);
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
            ALCardDTO cardToDraw = DrawCard(Deck, deckNode);
            hand.AddCardToHand(cardToDraw);
        }
    }

    void AddToRetreatAreaOnTop(ALCardDTO cardToAdd)
    {
        // We add to the bottom as the deck works flipped down
        AddCardToDeck(cardToAdd, Retreat, retreatNode, false);
    }

    void ApplyFlagshipDurability()
    {
        int durability = flagshipNode.GetAttributes<ALCardDTO>().durability;
        List<ALCard> durabilityList = durabilityArea.TryGetAllChildOfType<ALCard>();
        for (int i = 0; i < durability; i++)
        {
            ALCardDTO cardToDraw = DrawCard(Deck, deckNode);
            durabilityList[i].UpdateAttributes(cardToDraw);
        }

    }

    void TryDrawCubeToBoard()
    {
        try
        {
            ALCardDTO cardToDraw = DrawCard(CubeDeck, cubeDeckNode);
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
}

public enum EALTurnPhase
{
    Reset,
    Preparation,
    Main,
    Battle,
    End
}
