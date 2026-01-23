
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
        EALTurnPhase matchPhase = manager.GetMatchPhase();
        ALBoard board = (ALBoard)triggeringBoard;
        string interactionState = player.GetInteractionState();

        Card card = board.GetSelectedCard<Card>(player);
        if (card is ALPhaseButton phaseButton)
        {
            await OnPhaseBtnActionHandler(player, phaseButton, action);
            return;
        }
        if (card is not ALCard selectedCardInBoard)
        {
            GD.PrintErr($"[OnBoardInputActionHandler] No valid card selected");
            return;
        }
        //GD.Print($"[OnBoardInputActionHandler] Phase:{currentPhase} InteractionState:{state} Player:{triggeringPlayer.Name} Board:{triggeringBoard.Name} Action:{action} Card:{card.Name}");

        // Main
        if (matchPhase == EALTurnPhase.Main && interactionState == ALInteractionState.SelectBoardFieldToPlaceCard)
        {
            if (action == InputAction.Ok) await player.OnALPlaceCardStartHandler(board.CardToPlace);
            if (action == InputAction.Cancel) await player.OnCostPlaceCardCancelHandler(board.CardToPlace);
        }
        else if (matchPhase == EALTurnPhase.Main && player.GetInputPlayState() == EPlayState.SelectCardToPlay && interactionState == ALInteractionState.None)
        {
            if (action == InputAction.Ok) await board.TriggerCardEffectOnTargetSelected(selectedCardInBoard);
        }

        // Battle

        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectAttackerUnit)
        {
            if (action == InputAction.Ok) await player.StartBattle(selectedCardInBoard);
        }
        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectAttackTarget)
        {
            if (action == InputAction.Ok) player.AttackCard(manager.GetAttackerCard(), selectedCardInBoard);
            if (action == InputAction.Cancel) await player.CancelAttack(player);
        }
        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectGuardingUnit)
        {
            if (action == InputAction.Ok) await player.PlayCardAsGuard(selectedCardInBoard);
            if (action == InputAction.Cancel) player.EndGuardPhase();
        }
        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectRetaliationUnit)
        {
            if (action == InputAction.Cancel) await player.CancelRetaliation(player);
        }

        // Generic

        else if (interactionState == ALInteractionState.SelectEffectTarget)
        {
            if (action == InputAction.Ok) await board.TriggerCardEffectOnTargetSelected(selectedCardInBoard);
            if (action == InputAction.Cancel) await player.CancelSelectEffectState(player);
        }

    }
    public async Task OnHandInputActionHandler(Player triggeringPlayer, Board triggeringBoard, InputAction action)
    {
        ALPlayer player = (ALPlayer)triggeringPlayer;
        EALTurnPhase matchPhase = manager.GetMatchPhase();
        ALHand hand = (ALHand)triggeringBoard;
        string interactionState = player.GetInteractionState();

        Card card = hand.GetSelectedCard<Card>(player);
        if (card is not ALCard selectedCardInHand)
        {
            GD.PrintErr($"[OnBoardInputActionHandler] No valid card selected");
            return;
        }
        //GD.Print($"[OnBoardInputActionHandler] Phase:{currentPhase} InteractionState:{state} Player:{triggeringPlayer.Name} Board:{triggeringBoard.Name} Action:{action} Card:{card.Name}");

        // Main

        if (matchPhase == EALTurnPhase.Main && player.GetInputPlayState() == EPlayState.SelectCardToPlay && interactionState == ALInteractionState.None)
        {
            if (action == InputAction.Ok) await player.OnCostPlayCardStartHandler(selectedCardInHand);
            if (action == InputAction.Cancel) await player.OnCostPlaceCardCancelHandler(selectedCardInHand);
        }
        else if (matchPhase == EALTurnPhase.Main && interactionState == ALInteractionState.SelectBoardFieldToPlaceCard)
        {
            if (action == InputAction.Cancel) await player.OnCostPlaceCardCancelHandler(player.GetPlayerBoard<ALBoard>().CardToPlace);
        }

        // Battle

        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectGuardingUnit)
        {
            if (action == InputAction.Ok) await player.PlayCardAsGuard(selectedCardInHand);
            if (action == InputAction.Cancel) player.EndGuardPhase();
        }
        else if (matchPhase == EALTurnPhase.Battle && interactionState == ALInteractionState.SelectRetaliationUnit)
        {
            if (action == InputAction.Ok) await player.PlayCardInRetaliationPhase(selectedCardInHand);
            if (action == InputAction.Cancel) await player.CancelRetaliation(player);
        }

        // Generic
        else if (interactionState == ALInteractionState.SelectEffectTarget)
        {
            if (action == InputAction.Ok) await hand.TriggerCardEffectOnTargetSelected(selectedCardInHand);
            if (action == InputAction.Cancel) await player.CancelSelectEffectState(player);
        }
    }

    public async Task OnPhaseBtnActionHandler(ALPlayer player, ALPhaseButton phaseButton, InputAction action)
    {
        GD.Print($"[OnPhaseBtnActionHandler]");
        EALTurnPhase playerPhase = player.Phase.GetCurrentPhase();
        EALTurnPhase matchPhase = manager.GetMatchPhase(); // I want the synched match phase so both player can interact
        string interactionState = player.GetInteractionState();

        if (playerPhase == EALTurnPhase.Main) player.Phase.PlayNextPhase();
        if (matchPhase == EALTurnPhase.Battle)
        {
            if (player.GetInputPlayState() == EPlayState.SelectTarget && player.GetInteractionState() == ALInteractionState.SelectAttackerUnit) player.Phase.PlayNextPhase();
            if (interactionState == ALInteractionState.SelectGuardingUnit) player.EndGuardPhase();
            if (interactionState == ALInteractionState.SelectRetaliationUnit) await player.CancelRetaliation(player);
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
