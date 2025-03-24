
using System;
using System.Collections.Generic;
using Godot;

public partial class ALPlayer : Player
{
    public List<ALCardDTO> Deck = [];
    public List<ALCardDTO> CubeDeck = [];
    public ALCardDTO Flagship = new();
    ALDatabase database = new();

    new ALBoard board;
    new ALHand hand;

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    public override void _Ready()
    {
        base._Ready();
        hand = GetNode<ALHand>("Hand");
        board = GetNode<ALBoard>("Board");
        InitializeEvents();

        database.LoadData();
        Callable.From(StartGameForPlayer).CallDeferred();
    }

    protected new void InitializeEvents()
    {
        board.OnCardTrigger -= OnCardTriggerHandler;
        board.OnCardTrigger += OnCardTriggerHandler;
        hand.OnPlayCardStart -= OnPlayCardStartHandler; // Unload default event
        hand.OnPlayCardStart -= OnCostPlayCardStartHandler;
        hand.OnPlayCardStart += OnCostPlayCardStartHandler;
        GD.Print("[InitializeEvents] ALPlayer events initialized");
    }

    void BuildDeck()
    {
        //TODO: Customize deck
        // Flagship
        Flagship = database.cards["SD01-001"];

        // Deck
        List<string> availableDeckKeys = new() {
            "SD01-002",
            "SD01-003",
            "SD01-004",
            "SD01-005",
            "SD01-006",
            "SD01-007",
            "SD01-008"
        };
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
        SelectBoard(hand);
        DrawCardToHand(5);
        hand.SelectCardField(Vector2I.Zero);

        // Deck setup
        board.SelectCardField(new Vector2I(3, 1)); // Deck position
        board.SelectedCard.CardStack = Deck.Count; // Set it as deck size
        board.UpdateSelectedCardDTO(Deck[0]);
        // CubeDeck setup
        board.SelectCardField(new Vector2I(-1, 2)); // Cube deck position
        board.SelectedCard.CardStack = CubeDeck.Count; // Set it as deck size
        board.UpdateSelectedCardDTO(CubeDeck[0]);
        // Flagship setup
        board.SelectCardField(Vector2I.One); // Flagship position
        board.UpdateSelectedCardDTO(Flagship);

        StartTurn();
    }

    void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");
        PlayResetPhase();
    }
    void PlayResetPhase()
    {
        // Reset all Units into active state
        GD.Print($"[PlayResetPhase]");
        currentPhase = EALTurnPhase.Reset;
        SetBoardCardsAsActive();
        PlayNextPhase(currentPhase);
    }
    void PlayPreparationPhase()
    {
        // Draw 1 card
        // Place 1 face up cube
        GD.Print($"[PlayPreparationPhase]");
        currentPhase = EALTurnPhase.Preparation;
        DrawCubeToBoard();
        DrawCardToHand();
        PlayNextPhase(currentPhase);
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
        currentPhase = EALTurnPhase.End;
        EndTurn();
    }

    void EndTurn()
    {
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

    async void PlayNextPhase(EALTurnPhase currentPhase)
    {
        await this.Wait(0.2f);
        EALTurnPhase nextPhase = currentPhase + 1;
        if (nextPhase > EALTurnPhase.End) return;

        ApplyPhase(nextPhase);
    }

    void DrawCardToHand(int num = 1)
    {
        for (int i = 0; i < num; i++)
        {
            ALCardDTO cardToDraw = DrawCard(Deck);
            hand.AddCardToHand(cardToDraw);
        }
    }

    void DrawCubeToBoard()
    {
        ALCardDTO cardToDraw = DrawCard(CubeDeck);
        Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
            board.GetNode("CostArea").TryGetAllChildOfType<Card>());
        board.SelectCardField(cubeField.PositionInBoard);
        board.UpdateSelectedCardDTO(cardToDraw);
    }

    void SetBoardCardsAsActive()
    {
        // We wanna only reset units and cost area cards in AzurLane TCG
        board.GetNode("CostArea").TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
    }

    List<ALCard> GetActiveCubesInBoard() => board.GetNode("CostArea").TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());

    // Events
    void OnCostPlayCardStartHandler(Card cardToPlay)
    {
        if (cardToPlay is not ALCard card)
        {
            GD.PrintErr($"[OnPlayCardHandler] Cannot play a card not belonging to AzurLane TCG, {cardToPlay.Name} is {cardToPlay.GetType()} ");
            return;
        }

        ALCardDTO attributes = card.GetAttributes();
        GD.Print(attributes.name);

        var activeCubes = GetActiveCubesInBoard();

        GD.Print($"[OnPlayCardHandler] Cost {attributes.cost} - Active cubes {activeCubes.Count}");

        if (attributes.cost > activeCubes.Count)
        {
            GD.PrintErr("[OnPlayCardHandler] Player doesn't have enough cubes to play this card");
            return;
        }

        for (int i = 0; i < attributes.cost; i++)
        {
            activeCubes[activeCubes.Count - 1 - i].SetIsInActiveState(false); // Last match
        }

        OnPlayCardStartHandler(card);
    }

    void OnCardTriggerHandler(Card card)
    {
        if (card is ALPhaseButton phaseButton)
        {
            if (currentPhase == EALTurnPhase.Main) PlayNextPhase(currentPhase);
            if (currentPhase == EALTurnPhase.Battle) PlayNextPhase(currentPhase);
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