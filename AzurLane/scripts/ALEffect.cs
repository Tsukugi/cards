using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALEffect(ALCard _card, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : Effect(_card, _ownerPlayer)
{
    readonly ALGameMatchManager matchManager = _matchManager;

    protected bool CheckCondition(CardEffectConditionDTO condition)
    {
        bool conditionResult = (bool)ClassUtils.CallMethod(this, condition.conditionId, [condition]);
        return conditionResult;
    }

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
            GD.PushWarning($"[TryToApplyEffects] Apply effect {effect.effectId}: {effect.effectLabel}");
            await matchManager.GetPlayerUI().OnEffectTriggerUI(card.CastToALCard());

        }
    }

    // --- Condition ---

    public bool IsSpecificCardOnField(object?[]? args)
    {
        CardEffectConditionDTO conditionDTO = (CardEffectConditionDTO)args[0];
        List<ALCard> units = ((ALPlayer)ownerPlayer).GetUnitsInBoard();
        ALCardDTO attrs = card.GetAttributes<ALCardDTO>();

        ALCard? findResult = units.Find(unit =>
            {
                if (unit.IsEmptyField) return false;
                return unit.GetAttributes<ALCardDTO>().id == conditionDTO.conditionCard;
            });

        return findResult is ALCard foundCard;
    }

}