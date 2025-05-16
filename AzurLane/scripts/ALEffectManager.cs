using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public class ALEffectManager(ALCard _card, List<CardEffectDTO> _activeStatusEffects, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : EffectManager(_card, _activeStatusEffects, _ownerPlayer)
{
    readonly ALGameMatchManager matchManager = _matchManager;
    public bool CheckCanTriggerEffect(CardEffectDTO effectDTO)
    {
        List<CardEffectConditionDTO> conditionsFulfilled = [];
        foreach (CardEffectConditionDTO conditionDTO in effectDTO.condition)
        {

            bool fulfillsCondition = CheckCondition(conditionDTO);
            if (fulfillsCondition) conditionsFulfilled.Add(conditionDTO);
        }
        return conditionsFulfilled.Count == effectDTO.condition.Length;
    }

    public CardEffectDTO[] GetEffectsByTrigger(string triggerEvent)
    {
        ALCardDTO attrs = card.GetAttributes<ALCardDTO>();
        // Filter only the ones with the matching trigger event
        CardEffectDTO[] matchingEffects = Array.FindAll(attrs.effects, skill => skill.triggerEvent.Contains(triggerEvent));
        return matchingEffects;
    }

    public override async Task TryToApplyEffects(string triggerEvent)
    {
        await base.TryToApplyEffects(triggerEvent);
        foreach (CardEffectDTO effect in GetEffectsByTrigger(triggerEvent))
        {
            bool canTrigger = CheckCanTriggerEffect(effect);
            if (!canTrigger) return;

            GD.Print($"[TryToApplyEffects] Apply effect {effect.effectId}: {effect.effectLabel}");

            await matchManager.GetPlayerUI().OnEffectTriggerUI(card.CastToALCard());
            await ApplyEffect(effect);
        }
    }

    // --- Condition ---
    public bool CheckCardsInBoard(CardEffectConditionDTO conditionDTO)
    {
        string scope = conditionDTO.conditionArgs[0] ?? PlayerType.Self; // Enemy, Self
        string attributeToCompare = conditionDTO.conditionArgs[1] ?? "Cost";
        string comparator = conditionDTO.conditionArgs[2] ?? "LessThan";
        int value = (conditionDTO.conditionArgs[3] ?? "0").ToInt();

        Board targetBoard = GetBoardBasedOnScope(scope);

        bool fulfillsCondition = targetBoard.GetCardsInTree().FindAll((field) =>
        {
            if (field is not ALCard card) return false;
            if (!card.IsCardUnit()) return false;
            int attr = card.GetAttributeWithModifiers<ALCardDTO>(attributeToCompare);
            return LogicUtils.ApplyComparison(attr, comparator, value);
        }).Count > 0;

        GD.Print($"[Condition - CheckCardsInBoard] {scope} {attributeToCompare} {comparator} {value} => {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool CheckCardsInHand(CardEffectConditionDTO conditionDTO)
    {
        string scope = conditionDTO.conditionArgs[0] ?? PlayerType.Self; // Enemy, Self
        string attributeToCompare = conditionDTO.conditionArgs[1] ?? "Cost";
        string comparator = conditionDTO.conditionArgs[2] ?? "LessThan";
        int value = (conditionDTO.conditionArgs[3] ?? "0").ToInt();

        Board targetBoard = GetBoardBasedOnScope(scope);

        bool fulfillsCondition = targetBoard.GetCardsInTree().FindAll((field) =>
        {
            if (field is not ALCard card) return false;
            if (!card.IsCardUnit()) return false;
            int attr = card.GetAttributeWithModifiers<ALCardDTO>(attributeToCompare);
            return LogicUtils.ApplyComparison(attr, comparator, value);
        }).Count > 0;

        GD.Print($"[Condition - CheckCardsInBoard] {scope} {attributeToCompare} {comparator} {value} => {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool IsSpecificCardOnField(CardEffectConditionDTO conditionDTO)
    {
        string cardId = conditionDTO.conditionArgs[0];
        if (cardId is null) return false;

        List<ALCard> units = ((ALPlayer)ownerPlayer).GetUnitsInBoard();
        ALCardDTO attrs = card.GetAttributes<ALCardDTO>();

        ALCard? findResult = units.Find(unit =>
            {
                if (unit.GetIsEmptyField()) return false;
                return unit.GetAttributes<ALCardDTO>().id == cardId;
            });

        bool fulfillsCondition = findResult is ALCard foundCard;
        GD.Print($"[Condition - IsSpecificCardOnField] {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool IsPlacedOnBackRow(CardEffectConditionDTO conditionDTO)
    {
        bool fulfillsCondition = card.CastToALCard().GetAttackFieldType() == EAttackFieldType.BackRow;
        GD.Print($"[Condition - IsPlacedOnBackRow] {fulfillsCondition} - {card.CastToALCard().GetAttackFieldType()}");
        return fulfillsCondition;
    }

    public bool CheckFlagshipDurability(CardEffectConditionDTO conditionDTO)
    {
        string comparisonOperator = conditionDTO.conditionArgs[0] ?? "LessThan";
        string valueToCompare = conditionDTO.conditionArgs[1] ?? "0";

        int durability = ((ALPlayer)ownerPlayer).GetDurabilityCards().Count;

        bool fulfillsCondition = LogicUtils.ApplyComparison(durability, comparisonOperator, valueToCompare.ToInt());
        GD.Print($"[Condition - CheckFlagshipDurability] {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool HasCubes(CardEffectConditionDTO conditionDTO)
    {
        string comparisonOperator = conditionDTO.conditionArgs[0] ?? "LessThan";
        string valueToCompare = conditionDTO.conditionArgs[1] ?? "0";

        int cubes = ((ALPlayer)ownerPlayer).GetActiveCubesInBoard().Count;

        bool fulfillsCondition = LogicUtils.ApplyComparison(cubes, comparisonOperator, valueToCompare.ToInt());
        GD.Print($"[Condition - HasCubes] {cubes} {comparisonOperator} {valueToCompare} => {fulfillsCondition}");
        return fulfillsCondition;
    }
    public bool CurrentPlayerPlayingTurn(CardEffectConditionDTO conditionDTO)
    {
        string whoShouldBePlaying = conditionDTO.conditionArgs[0] ?? PlayerType.Self;
        bool fulfillsCondition = false;

        if (whoShouldBePlaying == PlayerType.Self) fulfillsCondition = matchManager.GetPlayerPlayingTurn() == ownerPlayer;
        else if (whoShouldBePlaying == PlayerType.Enemy) fulfillsCondition = matchManager.GetPlayerPlayingTurn() != ownerPlayer;

        GD.Print($"[Condition - CurrentPlayerPlayingTurn] {whoShouldBePlaying}: {ownerPlayer.Name} - {matchManager.GetPlayerPlayingTurn().Name} => {fulfillsCondition}");
        return fulfillsCondition;
    }

    // --- Effect ---
    public async Task GetPower(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - GetPower]");
        card.AddModifier(new AttributeModifier()
        {
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = effectDTO.effectValue[0].ToInt(),
        });
        await Task.CompletedTask;
    }
    public async Task SelectAndGivePower(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - SelectAndGivePower]");
        int amount = (effectDTO.effectValue[0] ?? "0").ToInt();
        string scope = effectDTO.effectValue[1] ?? PlayerType.Self;

        bool isFinished = false;
        EPlayState previousState = ownerPlayer.GetInputPlayState();
        string previousInteractionState = ownerPlayer.GetInteractionState();
        async Task OnAfterSelectAndGivePower(Card selectedTarget)
        {
            GD.Print($"[Effect - OnAfterSelectAndGivePower]");
            selectedTarget.AddModifier(new AttributeModifier()
            {
                AttributeName = "Power",
                Duration = effectDTO.duration,
                Amount = amount,
            });
            await ownerPlayer.GoBackInHistoryState();
            isFinished = true;
        }

        Board targetBoard = GetBoardBasedOnScope(scope);
        await ApplySelectEffectTargetAction(
               targetBoard,
               OnAfterSelectAndGivePower,
               () => isFinished
            );
    }

    public async Task AddStatusEffect(CardEffectDTO effectDTO)
    {
        string effectName = effectDTO.effectValue[0];
        if (activeStatusEffects.Contains(effectDTO) && !effectDTO.stackableEffect)
        {
            GD.PrintErr($"[Effect - AddEffect] Effect {effectName} already exists");
            return;
        }
        activeStatusEffects.Add(effectDTO);
        GD.Print($"[Effect - AddEffect] {effectName} to {card.GetAttributes<ALCardDTO>().name}, ActiveEffects: {activeStatusEffects.Count}");
        await Task.CompletedTask;
    }

    public async Task ReturnEnemyToHand(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - ReturnEnemyToHand]");
        string attributeToCompare = effectDTO.effectValue[0] ?? "Cost";
        string comparator = effectDTO.effectValue[1] ?? "LessThan";
        string value = effectDTO.effectValue[2] ?? "0";

        EPlayState previousState = ownerPlayer.GetInputPlayState();
        string previousInteractionState = ownerPlayer.GetInteractionState();
        async Task OnAfterSelectReturningCard(Card selectedTarget)
        {
            var board = ownerPlayer.GetPlayerBoard<ALBoard>();
            GD.Print($"[Effect - OnAfterSelectReturningCard]");
            var attrs = selectedTarget.GetAttributes<ALCardDTO>();
            if (attrs.cardType != ALCardType.Ship)
            {
                GD.PrintErr($"[Effect - OnAfterSelectReturningCard] {attrs.name} is not a Ship card, so it cannot be returned!. It is {attrs.cardType}");
                return;
            }
            int attr = selectedTarget.GetAttributeWithModifiers<ALCardDTO>(attributeToCompare);
            if (LogicUtils.ApplyComparison(attr, comparator, value.ToInt()))
            {
                ALPlayer enemyPlayer = selectedTarget.GetOwnerPlayer<ALPlayer>();
                await enemyPlayer.AddCardToHand(selectedTarget.GetAttributes<ALCardDTO>());
                selectedTarget.DestroyCard();
                await ownerPlayer.GoBackInHistoryState();
                return;
            }
            GD.PrintErr($"[OnAfterSelectReturningCard] Attribute: {attr} - {comparator} - Value: {value}");
        }

        await ApplySelectEffectTargetAction(
                ownerPlayer.GetEnemyPlayerBoard<PlayerBoard>(),
                OnAfterSelectReturningCard
            );

    }

    public async Task Awakening(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - Awakening] {effectDTO.effectValue}");
        //card.SetIsFaceDown(true);
        var id = card.GetAttributes<ALCardDTO>().id;
        card.UpdateAttributes(matchManager.GetDatabase().cards[$"{id}覺醒"]);
        await card.Wait(0.5f);
        await matchManager.GetPlayerUI().OnEffectTriggerUI(card.CastToALCard()); // TODO add a proper animation for Awakening
        await Task.CompletedTask;
    }
    public async Task ReactivateCube(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - ReactivateCube]");
        var cube = ((ALPlayer)ownerPlayer).GetCubesInBoard().Find(cube => !cube.GetIsEmptyField() && !cube.GetIsInActiveState());
        if (cube is not ALCard inactiveCube)
        {
            GD.PrintErr($"[ReactivateCube] No cube found to reactivate");
            return;
        }
        inactiveCube.SetIsInActiveState(true);
        await Task.CompletedTask;
    }
    public async Task Reactivate(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - Reactivate]");
        card.CastToALCard().SetIsInActiveState(true);
        await Task.CompletedTask;
    }

    public async Task Retaliation(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - Retaliation]");
        card.AddModifier(new AttributeModifier()
        {
            AttributeName = "Cost",
            Duration = CardEffectDuration.CurrentInteraction,
            Amount = -card.GetAttributes<ALCardDTO>().cost, // I want it to be 0
        });
        await Task.CompletedTask;
    }

    public async Task DiscardAndDrawCubeDown(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - DiscardAndDrawCubeDown]");

        EPlayState previousState = ownerPlayer.GetInputPlayState();
        string previousInteractionState = ownerPlayer.GetInteractionState();
        async Task OnSelectToDiscardCard(Card selectedTarget)
        {
            ALPlayer player = (ALPlayer)ownerPlayer;
            var hand = player.GetPlayerHand<PlayerHand>();
            GD.Print($"[Effect - OnAfterSelectReturningCard]");
            // Discard card from hand
            var attrs = selectedTarget.GetAttributes<ALCardDTO>();
            player.AddToRetreatAreaOnTop(attrs);
            hand.RemoveCardFromHand(player, selectedTarget);
            // Draw cube
            await player.TryDrawCubeToBoard();

            await ownerPlayer.GoBackInHistoryState();
            GD.Print($"[OnAfterSelectReturningCard] Discarding: {attrs.name}");
        }

        await ApplySelectEffectTargetAction(
                ownerPlayer.GetPlayerHand<PlayerHand>(),
                OnSelectToDiscardCard
            );
    }
    public async Task RestEnemy(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - RestEnemy]");

        EPlayState previousState = ownerPlayer.GetInputPlayState();
        string previousInteractionState = ownerPlayer.GetInteractionState();
        async Task OnSelectToRestCard(Card selectedTarget)
        {
            if (selectedTarget is ALCard target && target.IsCardUnit() && target.GetIsInActiveState())
            {
                target.SetIsInActiveState(false);
                GD.Print($"[OnSelectToRestCard] Resting: {target.GetAttributes<CardDTO>().name}");
                await ownerPlayer.GoBackInHistoryState();
                return;
            }
            GD.PrintErr($"[OnSelectToRestCard] Select a valid target to rest");
        }

        await ApplySelectEffectTargetAction(
                ownerPlayer.GetEnemyPlayerBoard<PlayerBoard>(),
                OnSelectToRestCard
            );
    }
    public async Task RestOrDestroyEnemy(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - RestOrDestroyEnemy]");

        EPlayState previousState = ownerPlayer.GetInputPlayState();
        string previousInteractionState = ownerPlayer.GetInteractionState();
        async Task OnSelectToRestOrDestroyCard(Card selectedTarget)
        {
            if (selectedTarget is ALCard target && target.IsCardUnit())
            {
                // If active, rest it. If resting, destroy it.
                if (target.GetIsInActiveState())
                {
                    target.SetIsInActiveState(false);
                    GD.Print($"[OnSelectToRestCard] Resting: {target.GetAttributes<CardDTO>().name}");
                }
                else
                {
                    target.DestroyCard();
                    GD.Print($"[OnSelectToRestCard] Destroying: {target.GetAttributes<CardDTO>().name}");
                }
                await ownerPlayer.GoBackInHistoryState();
                return;
            }
            GD.PrintErr($"[OnSelectToRestCard] Select a valid target to rest");
        }

        await ApplySelectEffectTargetAction(
                ownerPlayer.GetEnemyPlayerBoard<PlayerBoard>(),
                OnSelectToRestOrDestroyCard
            );
    }

    public async Task Rush(CardEffectDTO effectDTO)
    {
        //TODO: 
    }

    async Task ApplySelectEffectTargetAction(Board target, Board.CardEvent OnAfterSelect, AsyncHandler.SimpleCheck ConclusionCheck = null)
    {
        GD.Print($"[Effect - ApplySelectEffectTargetAction]");
        PlayState oldPlayState = ownerPlayer.GetPlayState();
        await ownerPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectEffectTarget);
        target.OnCardEffectTargetSelected -= OnAfterSelect;
        target.OnCardEffectTargetSelected += OnAfterSelect;

        var safeConclusionCheck = ConclusionCheck is null ? () => true : ConclusionCheck;
        await asyncHandler.AwaitForCheck(
            () =>
            {
                target.OnCardEffectTargetSelected -= OnAfterSelect;
            },
            () =>
            safeConclusionCheck() && ownerPlayer.GetPlayState() == oldPlayState,
            -1);
    }
}
