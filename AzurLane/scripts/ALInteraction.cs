
using System.Threading.Tasks;
using Godot;

public class ALInteraction
{
    ALGameMatchManager manager;
    public ALInteraction(ALGameMatchManager matchManager)
    {
        manager = matchManager;
    }

    public async Task OnBoardInputActionHandler(Player triggeringPlayer, Board triggeringBoard, InputAction action)
    {
        ALPlayer player = (ALPlayer)triggeringPlayer;
        EALTurnPhase currentPhase = manager.GetMatchPhase();
        ALBoard board = (ALBoard)triggeringBoard;
        string state = player.GetInteractionState();

        Card card = board.GetSelectedCard<Card>(player);
        if (card is ALPhaseButton phaseButton)
        {
            await OnPhaseBtnActionHandler(player, phaseButton, action);
            return;
        }
        if (card is not ALCard selectedCard)
        {
            GD.PrintErr($"[OnBoardInputActionHandler] No valid card selected");
            return;
        }
        GD.Print($"[OnBoardInputActionHandler] Phase:{currentPhase} InteractionState:{state} Player:{triggeringPlayer.Name} Board:{triggeringBoard.Name} Action:{action} Card:{card.Name}");
        switch (currentPhase)
        {
            case EALTurnPhase.Main:

                if (player.GetInteractionState() == ALInteractionState.SelectBoardFieldToPlaceCard)
                {
                    if (action == InputAction.Ok) await player.OnALPlaceCardStartHandler(board.CardToPlace);
                    if (action == InputAction.Cancel) await player.OnCostPlaceCardCancelHandler(board.CardToPlace);
                }
                if (player.GetPlayState() == EPlayState.SelectCardToPlay && player.GetInteractionState() == ALInteractionState.None)
                {
                    if (action == InputAction.Ok) await board.TriggerCardEffectOnTargetSelected(selectedCard);
                }
                if (player.GetInteractionState() == ALInteractionState.SelectEffectTarget)
                {
                    if (action == InputAction.Ok) await board.TriggerCardEffectOnTargetSelected(selectedCard);
                    if (action == InputAction.Cancel) await player.CancelSelectEffectState(player);
                }
                break;
            case EALTurnPhase.Battle:
                if (player.GetInteractionState() == ALInteractionState.SelectAttackerUnit)
                {
                    if (action == InputAction.Ok) await player.StartBattle(selectedCard);
                }
                if (player.GetInteractionState() == ALInteractionState.SelectAttackTarget)
                {
                    if (action == InputAction.Ok) player.AttackCard(manager.GetAttackerCard(), selectedCard);
                    if (action == InputAction.Cancel) await player.CancelAttack(player);
                }
                if (player.GetInteractionState() == ALInteractionState.SelectGuardingUnit)
                {
                    if (action == InputAction.Ok) await player.PlayCardAsGuard(selectedCard);
                    if (action == InputAction.Cancel) player.EndGuardPhase();
                }
                if (player.GetInteractionState() == ALInteractionState.SelectEffectTarget)
                {
                    if (action == InputAction.Ok) await board.TriggerCardEffectOnTargetSelected(selectedCard);
                    if (action == InputAction.Cancel) await player.CancelSelectEffectState(player);
                }
                if (player.GetInteractionState() == ALInteractionState.SelectRetaliationUnit)
                {
                    if (action == InputAction.Cancel) await player.CancelRetaliation(player);
                }
                break;
        }
        await Task.CompletedTask;
    }
    public async Task OnHandInputActionHandler(Player triggeringPlayer, Board triggeringBoard, InputAction action)
    {
        ALPlayer player = (ALPlayer)triggeringPlayer;
        EALTurnPhase currentPhase = manager.GetMatchPhase();
        ALHand hand = (ALHand)triggeringBoard;
        string state = player.GetInteractionState();

        Card card = hand.GetSelectedCard<Card>(player);
        if (card is not ALCard selectedCard)
        {
            GD.PrintErr($"[OnBoardInputActionHandler] No valid card selected");
            return;
        }
        GD.Print($"[OnBoardInputActionHandler] Phase:{currentPhase} InteractionState:{state} Player:{triggeringPlayer.Name} Board:{triggeringBoard.Name} Action:{action} Card:{card.Name}");
        switch (currentPhase)
        {
            case EALTurnPhase.Main:
                if (player.GetPlayState() == EPlayState.SelectCardToPlay && state == ALInteractionState.None)
                {
                    if (action == InputAction.Ok) await player.OnCostPlayCardStartHandler(selectedCard);
                    if (action == InputAction.Cancel) await player.OnCostPlaceCardCancelHandler(selectedCard);
                }
                break;
            case EALTurnPhase.Battle:
                if (state == ALInteractionState.SelectGuardingUnit)
                {
                    if (action == InputAction.Ok) await player.PlayCardAsGuard(selectedCard);
                    if (action == InputAction.Cancel) player.EndGuardPhase();
                }
                if (state == ALInteractionState.SelectEffectTarget)
                {
                    if (action == InputAction.Cancel) await player.CancelSelectEffectState(player);
                }
                if (state == ALInteractionState.SelectRetaliationUnit)
                {
                    if (action == InputAction.Ok) await hand.TriggerCardEffectOnTargetSelected(selectedCard);
                    if (action == InputAction.Cancel) await player.CancelRetaliation(player);
                }
                break;
        }

        await Task.CompletedTask;
    }

    public async Task OnPhaseBtnActionHandler(ALPlayer player, ALPhaseButton phaseButton, InputAction action)
    {
        GD.Print($"[OnPhaseBtnActionHandler]");
        EALTurnPhase currentPhase = player.Phase.GetCurrentPhase();
        EALTurnPhase matchPhase = manager.GetMatchPhase(); // I want the synched match phase so both player can interact
        string state = player.GetInteractionState();

        if (currentPhase == EALTurnPhase.Main) player.Phase.PlayNextPhase();
        if (matchPhase == EALTurnPhase.Battle)
        {
            if (player.GetPlayState() == EPlayState.SelectTarget && player.GetInteractionState() == ALInteractionState.SelectAttackerUnit) player.Phase.PlayNextPhase();
            if (state == ALInteractionState.SelectGuardingUnit) player.EndGuardPhase(); 
            if (state == ALInteractionState.SelectRetaliationUnit) await player.CancelRetaliation(player);
        }
        await Task.CompletedTask;
    }
}

public static class ALInteractionState
{
    public static readonly string None = "None";
    public static readonly string SelectEffectTarget = "SelectEffectTarget";
    public static readonly string SelectBoardFieldToPlaceCard = "SelectBoardFieldToPlaceCard";
    public static readonly string SelectAttackTarget = "SelectAttackTarget";
    public static readonly string SelectAttackerUnit = "SelectAttackerUnit";
    public static readonly string SelectGuardingUnit = "SelectGuardingUnit";
    public static readonly string SelectRetaliationUnit = "SelectRetaliationUnit";
    public static readonly string AwaitOtherPlayerInteraction = "AwaitOtherPlayerInteraction";
}