using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class ALEffect : Effect
{
    ALGameMatchManager matchManager;
    public ALEffect(ALCard _card, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : base(_card, _ownerPlayer)
    {
        matchManager = _matchManager;
    }
    protected bool CheckCondition(ALCardSkillConditionDTO condition)
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
            List<ALCardSkillConditionDTO> conditionsFulfilled = [];
            foreach (var conditionDTO in skill.condition)
            {

                bool fulfillsCondition = CheckCondition(conditionDTO);
                if (fulfillsCondition) conditionsFulfilled.Add(conditionDTO);
            }
            if (conditionsFulfilled.Count == skill.condition.Length) GD.Print($"[TryToApplyEffects] Apply effect {skill.effectId}: {skill.effectLabel}");
        }
    }

    public bool IsAttacked(object?[]? args)
    {
        return matchManager.GetAttackedCard() == card;
    }

    public bool StartsAttack(object?[]? args)
    {
        return matchManager.GetAttackerCard() == card;
    }

    public bool IsSpecificCardOnField(object?[]? args)
    {
        ALCardSkillConditionDTO conditionDTO = (ALCardSkillConditionDTO)args[0];
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