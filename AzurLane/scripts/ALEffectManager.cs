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
        CardEffectDTO[] matchingEffects = Array.FindAll(attrs.effects, skill => skill.triggerEvent == triggerEvent);
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
        List<ALCard> units = ((ALPlayer)ownerPlayer).GetUnitsInBoard();
        ALCardDTO attrs = card.GetAttributes<ALCardDTO>();

        ALCard? findResult = units.Find(unit =>
            {
                if (unit.GetIsEmptyField()) return false;
                return unit.GetAttributes<ALCardDTO>().id == conditionDTO.conditionCard;
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
        var previousState = ownerPlayer.GetPlayState();
        ownerPlayer.SetPlayState(EPlayState.SelectEffectTarget);

        var board = ownerPlayer.GetPlayerBoard<ALBoard>();
        void OnAfterSelectAndGivePower(Card selectedTarget)
        {
            var board = ownerPlayer.GetPlayerBoard<ALBoard>();
            GD.Print($"[Effect - OnAfterSelectAndGivePower]");
            selectedTarget.AddModifier(new AttributeModifier()
            {
                AttributeName = "Power",
                Duration = effectDTO.duration,
                Amount = effectDTO.effectValue[0].ToInt(),
            });
            board.OnCardTrigger -= OnAfterSelectAndGivePower;
            ownerPlayer.SetPlayState(previousState);
            isFinished = true;
        }
        board.OnCardTrigger -= OnAfterSelectAndGivePower;
        board.OnCardTrigger += OnAfterSelectAndGivePower;

        await asyncHandler.AwaitForCheck(
            null,
            () => isFinished && ownerPlayer.GetPlayState() == previousState, // Playstate has delay, that's why i need to check for it too
            -1);
    }


    public async Task AddStatusEffect(CardEffectDTO effectDTO)
    {
        activeStatusEffects.Add(effectDTO);
        GD.Print($"[Effect - AddEffect] {effectDTO.effectValue[0]} to {card.GetAttributes<ALCardDTO>().name}, ActiveEffects: {activeStatusEffects.Count}");
        await Task.CompletedTask;
    }

    public async Task ReturnToHand(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - ReturnToHand] {effectDTO.effectValue}");
        string times = effectDTO.effectValue[0];
        string attributeToCompare = effectDTO.effectValue[1];
        string comparator = effectDTO.effectValue[2];
        string value = effectDTO.effectValue[3];
        // TODO Add me
        await Task.CompletedTask;
    }

    public async Task Awakening(CardEffectDTO effectDTO)
    {
        string newPower = effectDTO.effectValue[0];
        string awakeningEffect = effectDTO.effectValue[1];
        GD.Print($"[Effect - Awakening] {effectDTO.effectValue}");
        card.SetIsFaceDown(true);
        card.AddModifier(new AttributeModifier()
        {
            AttributeName = "Power",
            Duration = CardEffectDuration.WhileFaceDown,
            Amount = newPower.ToInt() - card.GetAttributes<ALCardDTO>().power,
            // If face up power is 400 and face down (awakened) is 500, i want to give a 100 boost (500 - 400)
        });

        await ClassUtils.CallMethodAsync(card, awakeningEffect, effectDTO.effectValue);
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
    }
    public async Task Reactivate(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - Reactivate]");
        card.CastToALCard().SetIsInActiveState(true);
    }
}
