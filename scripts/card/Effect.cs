using System;
using System.Reflection;
using Godot;

public class Effect
{
    protected Card card;
    protected Player ownerPlayer;
    public Effect(Card _card, Player _ownerPlayer)
    {
        card = _card;
        ownerPlayer = _ownerPlayer;
    }

    protected bool CheckCondition(string conditionId)
    {
        GD.Print($"[CheckCondition] {conditionId}");
        bool conditionResult = (bool)ClassUtils.CallMethod(this, conditionId, []);
        return conditionResult;
    }

    public virtual void TryToApplyEffects(string triggerEvent)
    {
        GD.Print($"[TryToApplyEffects]");
    }
}