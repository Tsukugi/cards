
using System.Collections.Generic;
using Godot;

public partial class ALPlayer : Player
{
    public List<ALCardDTO> Deck = [];
    public ALCardDTO Flagship = new();

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    public override void _Ready()
    {
        base._Ready();
        Callable.From(StartGameForPlayer).CallDeferred();
    }

    void StartGameForPlayer()
    {
        SelectBoard(hand);
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.SelectCardField(Vector2I.Zero);

        board.SelectCardField(Vector2I.One); // Flagship position
        board.ReplaceCardDTO(Flagship);
        SetPlayState(EPlayState.Select);
    }

    void StartTurn()
    {

    }

    void PlayResetPhase()
    {
        // Reset all Units into active state
        GD.Print($"[PlayResetPhase]");
    }
    void PlayPreparationPhase()
    {
        // Draw 1 card
        // Place 1 face up cube
        GD.Print($"[PlayPreparationPhase]");
    }
    void PlayMainPhase()
    {

        GD.Print($"[PlayMainPhase]");
    }
    void PlayBattlePhase()
    {

        GD.Print($"[PlayBattlePhase]");
    }
    void PlayEndPhase()
    {

        GD.Print($"[PlayEndPhase]");
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