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

    public bool IsSpecificCardOnField(CardEffectConditionDTO conditionDTO)
    {
        string cardId = conditionDTO.conditionArgs[0];

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

    public bool OnBackRow(CardEffectConditionDTO conditionDTO)
    {
        bool fulfillsCondition = card.CastToALCard().GetAttackFieldType() == EAttackFieldType.BackRow;
        GD.Print($"[Condition - OnBackRow] {fulfillsCondition} - {card.CastToALCard().GetAttackFieldType()}");
        return fulfillsCondition;
    }

    public bool CheckFlagshipDurability(CardEffectConditionDTO conditionDTO)
    {
        string comparisonOperator = conditionDTO.conditionArgs[0];
        string valueToCompare = conditionDTO.conditionArgs[1];

        int durability = ((ALPlayer)ownerPlayer).GetDurabilityCards().Count;

        bool fulfillsCondition = LogicUtils.ApplyComparison(durability, comparisonOperator, valueToCompare.ToInt());
        GD.Print($"[Condition - CheckFlagshipDurability] {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool HasCubes(CardEffectConditionDTO conditionDTO)
    {
        string comparisonOperator = conditionDTO.conditionArgs[0];
        string valueToCompare = conditionDTO.conditionArgs[1];

        int cubes = ((ALPlayer)ownerPlayer).GetActiveCubesInBoard().Count;

        bool fulfillsCondition = LogicUtils.ApplyComparison(cubes, comparisonOperator, valueToCompare.ToInt());
        GD.Print($"[Condition - HasCubes] {fulfillsCondition}");
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
        bool isFinished = false;
        EPlayState previousState = ownerPlayer.GetPlayState();
        void OnAfterSelectAndGivePower(Card selectedTarget)
        {
            GD.Print($"[Effect - OnAfterSelectAndGivePower]");
            selectedTarget.AddModifier(new AttributeModifier()
            {
                AttributeName = "Power",
                Duration = effectDTO.duration,
                Amount = effectDTO.effectValue[0].ToInt(),
            });
            ownerPlayer.SetPlayState(previousState);
            isFinished = true;
        }

        await ApplySelectPlayState(
               ownerPlayer.GetPlayerBoard<ALBoard>(),
               OnAfterSelectAndGivePower,
               () => isFinished
            );
    }

    public async Task AddStatusEffect(CardEffectDTO effectDTO)
    {
        activeStatusEffects.Add(effectDTO);
        string effectName = effectDTO.effectValue[0];
        if (activeStatusEffects.Contains(effectDTO) && !effectDTO.effectValue.Contains(ALCardReservedEffectValues.StackableEffect))
        {
            GD.PrintErr($"[Effect - AddEffect] Effect {effectName} already exists");
            return;
        }
        GD.Print($"[Effect - AddEffect] {effectName} to {card.GetAttributes<ALCardDTO>().name}, ActiveEffects: {activeStatusEffects.Count}");
        await Task.CompletedTask;
    }

    public async Task ReturnEnemyToHand(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - ReturnEnemyToHand]");
        string attributeToCompare = effectDTO.effectValue[0];
        string comparator = effectDTO.effectValue[1];
        string value = effectDTO.effectValue[2];

        EPlayState previousState = ownerPlayer.GetPlayState();
        void OnAfterSelectReturningCard(Card selectedTarget)
        {
            var board = ownerPlayer.GetPlayerBoard<ALBoard>();
            GD.Print($"[Effect - OnAfterSelectReturningCard]");
            int attr = selectedTarget.GetAttributeWithModifiers<ALCardDTO>(attributeToCompare);
            if (LogicUtils.ApplyComparison(attr, comparator, value.ToInt()))
            {
                ALPlayer enemyPlayer = selectedTarget.GetOwnerPlayer<ALPlayer>();
                enemyPlayer.AddCardToHand(selectedTarget.GetAttributes<ALCardDTO>());
                selectedTarget.DestroyCard();
                ownerPlayer.SetPlayState(previousState);
                return;
            }
            GD.PrintErr($"[OnAfterSelectReturningCard] Attribute: {attr} - {comparator} - Value: {value}");
        }

        await ApplySelectPlayState(
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
        // TODO implement me
        await Task.CompletedTask;
    }

    async Task ApplySelectPlayState(Board target, Board.CardEvent OnAfterSelect, AsyncHandler.SimpleCheck ConclusionCheck = null)
    {
        GD.Print($"[ApplySelectPlayState]");
        ConclusionCheck ??= () => true;

        EPlayState previousState = ownerPlayer.GetPlayState();
        ownerPlayer.SetPlayState(EPlayState.SelectEffectTarget);
        target.OnCardTrigger -= OnAfterSelect;
        target.OnCardTrigger += OnAfterSelect;

        await asyncHandler.AwaitForCheck(
            () =>
            {
                target.OnCardTrigger -= OnAfterSelect;
            },
            () => ConclusionCheck() && ownerPlayer.GetPlayState() == previousState, // Playstate has delay, that's why i need to check for it too
            -1);
    }


}
