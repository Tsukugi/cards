using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class EffectManager(Card _card, List<CardEffectDTO> _activeStatusEffects, Player _ownerPlayer)
{
    protected Card card = _card;
    protected List<CardEffectDTO> activeStatusEffects = _activeStatusEffects;
    protected Player ownerPlayer = _ownerPlayer;
    protected AsyncHandler asyncHandler = new(_card);

    protected bool CheckCondition(CardEffectConditionDTO condition)
    {
        bool conditionResult = (bool)ClassUtils.CallMethod(this, condition.conditionId, [condition]);
        return conditionResult;
    }

    protected async Task ApplyEffect(CardEffectDTO effect)
    {
        GD.Print($"[ApplyEffect] {effect.effectId}");
        await ClassUtils.CallMethodAsync(this, effect.effectId, [effect]);
    }

    public virtual async Task TryToApplyEffects(string triggerEvent)
    {
        // GD.Print($"[TryToApplyEffects] {triggerEvent}");
        await Task.CompletedTask;
    }
    public bool HasActiveEffect(string name)
    {
        bool itHasEffect = TryGetStatusEffect(name) is not null;
        GD.Print($"[HasActiveEffect] {card.GetAttributes<CardDTO>().name}'s effects: {activeStatusEffects.Count} - Does it have {name}: {itHasEffect}");
        return itHasEffect;
    }
    public CardEffectDTO? TryGetStatusEffect(string name) => activeStatusEffects.Find(effect => effect.effectValue[0] == name);

    // -- Condition -- 

    public bool EmptyHand(CardEffectConditionDTO conditionDTO)
    {
        bool fulfillsCondition = ownerPlayer.GetPlayerHand<PlayerHand>().GetCardsInTree().Count == 0;
        GD.Print($"[Counter] {fulfillsCondition}");
        return fulfillsCondition;
    }
}