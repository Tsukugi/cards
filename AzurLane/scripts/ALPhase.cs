

// TODO: Think about i18ns
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALPhase
{
    public delegate void PhaseEvent(EALTurnPhase phase);
    public event PhaseEvent OnPhaseChange;
    public string Reset = "Reset Phase";
    public string Preparation = "Preparation Phase";
    public string Main = "Main Phase";
    public string Battle = "Battle Phase";
    public string End = "End Phase";

    EALTurnPhase currentPhase = EALTurnPhase.Reset;
    readonly ALPlayer player;
    readonly AsyncHandler asyncPhase;

    public ALPhase(ALPlayer _player)
    {
        player = _player;
        asyncPhase = _player.GetPlayerAsyncHandler();
    }

    public string GetPhaseByIndex(int index)
    {
        List<string> phases = [Reset, Preparation, Main, Battle, End];
        if (!phases.Count.IsInsideBounds(index)) { GD.PrintErr("[getPhaseByIndex] Index can't be linked to any known phase"); return ""; }
        return phases[index];
    }

    public async void StartTurn()
    {
        GD.Print($"[{player.Name}.StartTurn] --------------- Start of turn ---------------");
        await PlayResetPhase();
    }

    async Task PlayResetPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Reset all Units into active state
        GD.Print($"[{player.Name}.PlayResetPhase]");
        player.SetBoardCardsAsActive();
        UpdatePhase(EALTurnPhase.Reset);
        await asyncPhase.AwaitBefore(PlayNextPhase);
    }
    async Task PlayPreparationPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Draw 1 card
        // Place 1 face up cube if possible
        GD.Print($"[{player.Name}.PlayPreparationPhase]");
        player.TryDrawCubeToBoard();
        player.DrawCardToHand();
        UpdatePhase(EALTurnPhase.Preparation);
        await asyncPhase.AwaitBefore(PlayNextPhase);
    }
    async Task PlayMainPhase()
    {
        // Player can play cards
        GD.Print($"[{player.Name}.PlayMainPhase]");
        player.SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Main);
        await Task.CompletedTask;
    }
    async Task PlayBattlePhase()
    {
        // Player can declare attacks
        GD.Print($"[{player.Name}.PlayBattlePhase]");
        player.SetPlayState(EPlayState.Select);
        UpdatePhase(EALTurnPhase.Battle);
        await asyncPhase.AwaitBefore(EndBattlePhaseIfNoActiveCards);
    }
    async Task PlayEndPhase()
    {
        player.SetPlayState(EPlayState.Wait);
        // Clean some things
        UpdatePhase(EALTurnPhase.End);
        player.EndTurn();
        await Task.CompletedTask;
        GD.Print($"[{player.Name}.PlayEndPhase] --------------- End of turn ---------------");
    }

    async Task ApplyPhase(EALTurnPhase phase)
    {
        switch (phase)
        {
            case EALTurnPhase.Reset: await PlayResetPhase(); return;
            case EALTurnPhase.Preparation: await PlayPreparationPhase(); return;
            case EALTurnPhase.Main: await PlayMainPhase(); return;
            case EALTurnPhase.Battle: await PlayBattlePhase(); return;
            case EALTurnPhase.End: await PlayEndPhase(); return;
        }
    }

    public void PlayNextPhase()
    {
        EALTurnPhase nextPhase = currentPhase + 1;
        if (nextPhase > EALTurnPhase.End)
        {
            GD.PrintErr($"[PlayNextPhase] Trying to play next phase on End phase already!");
            GD.PushError($"[PlayNextPhase] Trying to play next phase on End phase already!");
            return;
        }
        asyncPhase.Debounce(() => _ = ApplyPhase(nextPhase));
    }

    void UpdatePhase(EALTurnPhase phase)
    {
        GD.Print($"[UpdatePhase] {currentPhase} -> {phase}");
        currentPhase = phase;
        if (OnPhaseChange is not null) OnPhaseChange(phase);
    }

    public async Task EndBattlePhaseIfNoActiveCards()
    {
        if (currentPhase != EALTurnPhase.Battle) return;
        List<ALCard> units = player.GetActiveUnitsInBoard();
        if (units.Count > 0) return;
        GD.Print($"[EndBattlePhaseIfNoActiveCards] No active cards, going to next phase");
        await asyncPhase.AwaitBefore(PlayNextPhase);
    }
    public EALTurnPhase GetCurrentPhase() => currentPhase;
}