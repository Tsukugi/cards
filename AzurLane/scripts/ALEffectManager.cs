using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALEffectManager(ALCard _card, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : EffectManager(_card, _ownerPlayer)
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
        GD.Print($"[IsSpecificCardOnField] {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool OnBackRow(CardEffectConditionDTO conditionDTO)
    {
        bool fulfillsCondition = card.CastToALCard().GetAttackFieldType() == EAttackFieldType.BackRow;
        GD.Print($"[OnBackRow] {fulfillsCondition} - {card.CastToALCard().GetAttackFieldType()}");
        return fulfillsCondition;
    }

    public bool Counter(CardEffectConditionDTO conditionDTO)
    {
        bool isBattlePhase = matchManager.GetMatchPhase() == EALTurnPhase.Battle;
        bool isDefenceStep = ownerPlayer.GetPlayState() == EPlayState.EnemyInteraction;
        bool fulfillsCondition = isBattlePhase && isDefenceStep;
        GD.Print($"[Counter] {fulfillsCondition}");
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
        var previousState = ownerPlayer.GetPlayState();
        ownerPlayer.SetPlayState(EPlayState.SelectEffectTarget);

        var board = ownerPlayer.GetPlayerBoard<ALBoard>();
        void OnAfterSelectAndGivePower(Card selectedTarget)
        {
            GD.Print($"[Effect - OnAfterSelectAndGivePower]");
            selectedTarget.AddModifier(new AttributeModifier()
            {
                AttributeName = "Power",
                Duration = effectDTO.duration,
                Amount = effectDTO.effectValue[0].ToInt(),
            });
            board.OnCardTrigger -= OnAfterSelectAndGivePower;
            ownerPlayer.SetPlayState(previousState);
        }
        board.OnCardTrigger -= OnAfterSelectAndGivePower;
        board.OnCardTrigger += OnAfterSelectAndGivePower;

        await asyncHandler.AwaitForCheck(
            null,
            () => ownerPlayer.GetPlayState() == previousState,
            -1,
            50);

        await Task.CompletedTask;
    }


    public async Task AddStatusEffect(CardEffectDTO effectDTO)
    {
        activeEffects.Add(effectDTO);
        GD.Print($"[Effect - AddEffect] {effectDTO.effectValue[0]} to {card.GetAttributes<ALCardDTO>().name}, ActiveEffects: {activeEffects.Count}");
        await Task.CompletedTask;
    }

    public async Task ReturnToHand(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - ReturnToHand] {effectDTO.effectValue}");
        var times = effectDTO.effectValue[0];
        var attributeToCompare = effectDTO.effectValue[1];
        var comparator = effectDTO.effectValue[2];
        var value = effectDTO.effectValue[3];
        await Task.CompletedTask;
    }

}
