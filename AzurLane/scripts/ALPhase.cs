

// TODO: Think about i18ns
using System.Collections.Generic;
using Godot;

public class ALPhase(ALPlayer _player)
{
    public delegate void PhaseEvent(EALTurnPhase phase);
    public event PhaseEvent OnPhaseChange;
    public string Reset = "Reset Phase";
    public string Preparation = "Preparation Phase";
    public string Main = "Main Phase";
    public string Battle = "Battle Phase";
    public string End = "End Phase";

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    readonly ALPlayer player = _player;
    readonly AsyncHandler asyncPhase = new(_player);

    public string GetPhaseByIndex(int index)
    {
        List<string> phases = [Reset, Preparation, Main, Battle, End];
        if (!phases.Count.IsInsideBounds(index)) { GD.PrintErr("[getPhaseByIndex] Index can't be linked to any known phase"); return ""; }
        return phases[index];
    }

    public void StartTurn()
    {
        PlayResetPhase();
    }

    void PlayResetPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Reset all Units into active state
        GD.Print($"[{player.Name}.PlayResetPhase]");
        player.SetBoardCardsAsActive();
        UpdatePhase(EALTurnPhase.Reset);
        _ = asyncPhase.AwaitBefore(PlayNextPhase);
    }
    void PlayPreparationPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Draw 1 card
        // Place 1 face up cube if possible
        GD.Print($"[{player.Name}.PlayPreparationPhase]");
        player.TryDrawCubeToBoard();
        player.DrawCardToHand();
        UpdatePhase(EALTurnPhase.Preparation);
        PlayNextPhase();
    }
    void PlayMainPhase()
    {
        // Player can play cards
        GD.Print($"[{player.Name}.PlayMainPhase]");
        player.SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Main);
    }
    void PlayBattlePhase()
    {
        // Player can declare attacks
        GD.Print($"[{player.Name}.PlayBattlePhase]");
        player.SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Battle);

        EndBattlePhaseIfNoActiveCards();
    }
    void PlayEndPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Clean some things
        GD.Print($"[{player.Name}.PlayEndPhase]");
        UpdatePhase(EALTurnPhase.End);
        _ = asyncPhase.AwaitBefore(player.EndTurn);
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

    public void PlayNextPhase()
    {
        EALTurnPhase nextPhase = currentPhase + 1;
        if (nextPhase > EALTurnPhase.End)
        {
            GD.PrintErr($"[PlayNextPhase] Trying to play next phase on End phase already!");
            return;
        }
        _ = asyncPhase.AwaitBefore(() => ApplyPhase(nextPhase));
    }

    void UpdatePhase(EALTurnPhase phase)
    {
        currentPhase = phase;
        if (OnPhaseChange is not null) OnPhaseChange(phase);
    }

    public void EndBattlePhaseIfNoActiveCards()
    {
        List<ALCard> units = player.GetActiveUnitsInBoard();
        if (units.Count > 0) return;
        _ = asyncPhase.AwaitBefore(PlayNextPhase);
        GD.Print($"[EndBattlePhaseIfNoActiveCards] No active cards, going to next phase");
    }
    public EALTurnPhase GetCurrentPhase() => currentPhase;
}