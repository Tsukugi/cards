using System;
using System.Collections.Generic;
using Godot;

public class ALEffect(ALCard _card, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : Effect(_card, _ownerPlayer)
{
    readonly ALGameMatchManager matchManager = _matchManager;

    protected bool CheckCondition(CardEffectConditionDTO condition)
    {
        bool conditionResult = (bool)ClassUtils.CallMethod(this, condition.conditionId, [condition]);
        return conditionResult;
    }

    public override void TryToApplyEffects(string triggerEvent)
    {
        base.TryToApplyEffects(triggerEvent);
        ALCardDTO attrs = card.GetAttributes<ALCardDTO>();
        // Filter only the ones with the matching trigger event
        var affectedSkills = Array.FindAll(attrs.skills, skill => skill.triggerEvent == triggerEvent);

        foreach (var skill in affectedSkills)
        {
            List<CardEffectConditionDTO> conditionsFulfilled = [];
            foreach (var conditionDTO in skill.condition)
            {

                bool fulfillsCondition = CheckCondition(conditionDTO);
                if (fulfillsCondition) conditionsFulfilled.Add(conditionDTO);
            }
            if (conditionsFulfilled.Count == skill.condition.Length) GD.Print($"[TryToApplyEffects] Apply effect {skill.effectId}: {skill.effectLabel}");
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