
using System;
using System.Collections.Generic;
using Godot;

public partial class ALPlayer : Player
{
    public List<ALCardDTO> Deck = [];
    public List<ALCardDTO> CubeDeck = [];
    public ALCardDTO Flagship = new();
    ALDatabase database = new();

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    public override void _Ready()
    {
        base._Ready();
        database.LoadData();
        Callable.From(StartGameForPlayer).CallDeferred();
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
        SetBoardCardsAsActive();
        PlayPreparationPhase();
    }
    void PlayPreparationPhase()
    {
        // Draw 1 card
        // Place 1 face up cube
        GD.Print($"[PlayPreparationPhase]");
        DrawCubeToBoard();
        PlayMainPhase();
    }
    void PlayMainPhase()
    {
        // Player can play cards
        GD.Print($"[PlayMainPhase]");
        SelectBoard(hand);
        hand.SelectCardField(Vector2I.Zero);
        SetPlayState(EPlayState.Select);
    }
    void PlayBattlePhase()
    {
        // Player can declare attacks
        GD.Print($"[PlayBattlePhase]");
    }
    void PlayEndPhase()
    {
        // Clean some things
        GD.Print($"[PlayEndPhase]");
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
        board.GetNode("CostArea").TryGetAllChildOfType<Card>().ForEach(card => card.SetIsSideWays(false));
        board.GetNode("Units").TryGetAllChildOfType<Card>().ForEach(card => card.SetIsSideWays(false));
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