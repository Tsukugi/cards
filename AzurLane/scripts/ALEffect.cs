using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ALEffect(ALCard _card, ALPlayer _ownerPlayer, ALGameMatchManager _matchManager) : Effect(_card, _ownerPlayer)
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
                if (unit.IsEmptyField) return false;
                return unit.GetAttributes<ALCardDTO>().id == conditionDTO.conditionCard;
            });

        bool fulfillsCondition = findResult is ALCard foundCard;
        GD.Print($"[IsSpecificCardOnField] {fulfillsCondition}");
        return fulfillsCondition;
    }

    public bool OnBackRow(CardEffectConditionDTO conditionDTO)
    {
        bool fulfillsCondition = card.CastToALCard().GetAttackFieldType() == EAttackFieldType.BackRow;
        GD.Print($"[OnBackRow] {fulfillsCondition}");
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
        GD.Print($"[Effect - GetPower]");
        card.AddModifier(new AttributeModifier()
        {
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = effectDTO.effectValue[0].ToInt(),
        });
        await Task.CompletedTask;
    }

    public async Task AddEffect(CardEffectDTO effectDTO)
    {
        GD.Print($"[Effect - AddEffect] {effectDTO.effectValue[0]}");
        activeEffects.Add(effectDTO);
        await Task.CompletedTask;
    }

}