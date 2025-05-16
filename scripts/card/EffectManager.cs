using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class EffectManager(Card _card, List<CardEffectDTO> _activeStatusEffects, Player _ownerPlayer)
{
    protected Card card = _card;
    protected List<CardEffectDTO> activeStatusEffects = _activeStatusEffects;
    protected Player ownerPlayer = _ownerPlayer;
    protected AsyncHandler asyncHandler = new(_ownerPlayer);

    protected bool CheckCondition(CardEffectConditionDTO condition)
    {
        bool conditionResult = (bool)ClassUtils.CallMethod(this, condition.conditionId, [condition]) || false;
        return conditionResult;
    }

    protected async Task ApplyEffect(CardEffectDTO effect)
    {
        GD.PrintErr($"[ApplyEffect] Start Effect: {effect.effectId}");
        await ClassUtils.CallMethodAsync(this, effect.effectId, [effect]);
        GD.PrintErr($"[ApplyEffect] End Effect: {effect.effectId}");
    }

    public virtual async Task TryToApplyEffects(string triggerEvent)
    {
        //GD.Print($"[TryToApplyEffects] {triggerEvent}");
        await Task.CompletedTask;
    }
    public bool HasActiveEffect(string name)
    {
        bool itHasEffect = TryGetStatusEffect(name) is not null;
        GD.Print($"[HasActiveEffect] {card.GetAttributes<CardDTO>().name}'s effects: {activeStatusEffects.Count} - Does it have {name}: {itHasEffect}");
        return itHasEffect;
    }
    public CardEffectDTO? TryGetStatusEffect(string name) => activeStatusEffects.Find(effect => effect.effectValue[0] == name);

    protected Player GetPlayerBasedOnScope(string scope)
    {
        if (scope == PlayerType.Self) return ownerPlayer;
        if (scope == PlayerType.Enemy) return ownerPlayer.GetEnemyPlayerBoard<PlayerBoard>().TryFindParentNodeOfType<Player>();
        return null;
    }
    protected virtual List<Card> GetCardListBasedOnArgs(string nodeType, string scope)
    {
        if (nodeType == BoardType.Hand) return GetPlayerBasedOnScope(scope).GetPlayerHand<PlayerHand>().GetCardsInTree();
        if (nodeType == BoardType.Board) return GetPlayerBasedOnScope(scope).GetPlayerBoard<PlayerBoard>().GetCardsInTree();
        GD.PrintErr("[GetBoardBasedOnType] No valid type provided");
        return null;
    }

    // -- Condition -- 
    public bool CheckCardCount(CardEffectConditionDTO conditionDTO)
    {
        string scope = conditionDTO.conditionArgs[0]; // Enemy, Self
        string boardType = conditionDTO.conditionArgs[1]; // Hand, Board
        string comparator = conditionDTO.conditionArgs[2];
        int value = conditionDTO.conditionArgs[3].ToInt();

        List<Card> cardList = GetCardListBasedOnArgs(boardType, scope);

        bool fulfillsCondition = LogicUtils.ApplyComparison(cardList.Count, comparator, value);
        GD.Print($"[CheckCardCount] {scope} {boardType} {cardList.Count} {comparator} {value} => {fulfillsCondition}");
        return fulfillsCondition;
    }
}