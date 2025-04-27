using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class Effect(Card _card, Player _ownerPlayer)
{
    protected readonly List<CardEffectDTO> activeEffects = []; // EffectIds
    protected Card card = _card;
    protected Player ownerPlayer = _ownerPlayer;

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
        GD.Print($"[TryToApplyEffects] {triggerEvent}");
        await Task.CompletedTask;
    }

    public CardEffectDTO? FindActiveEffect(string effectId) => activeEffects.Find(effect => effect.effectId == effectId);
}