using System.Threading.Tasks;
using Godot;

public class Effect(Card _card, Player _ownerPlayer)
{
    protected Card card = _card;
    protected Player ownerPlayer = _ownerPlayer;

    protected bool CheckCondition(string conditionId)
    {
        GD.Print($"[CheckCondition] {conditionId}");
        bool conditionResult = (bool)ClassUtils.CallMethod(this, conditionId, []);
        return conditionResult;
    }

    public virtual async Task TryToApplyEffects(string triggerEvent)
    {
        GD.Print($"[TryToApplyEffects] {triggerEvent}");
        await Task.CompletedTask;
    }
}