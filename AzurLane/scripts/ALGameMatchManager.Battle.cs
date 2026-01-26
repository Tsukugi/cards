using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALGameMatchManager
{
    async Task OnAttackStartHandler(Player attackingPlayer, Card card)
    {
        attackerCard = card.CastToALCard();
        await GetAttackerCard().TryToTriggerCardEffect(ALCardEffectTrigger.StartsAttack);
        GD.Print($"[OnAttackStartHandler] {GetAttackerCard().Name} starts an attack!");
        await attackingPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectAttackTarget);
    }

    async Task OnAttackTargetAdquiredHandler(Player guardingPlayer, Card card)
    {
        GetAttackerCard().SetIsInActiveState(false);
        attackedCard = card.CastToALCard();
        GD.Print($"[OnAttackTargetAdquiredHandler] {GetAttackerCard().Name} attacks {GetAttackedCard().Name}!");
        await Task.CompletedTask;
    }

    async Task OnAttackGuardStartHandler(Player attackerPlayer, Player attackedPlayer)
    {
        if (GetAttackedCard() is null)
        {
            throw new System.InvalidOperationException("[OnAttackGuardStartHandler] Attacked card is missing.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        await attackerPlayer.SetPlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        if (attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnAttackGuardStartHandler");
            ALNetwork.Instance.SyncGuardPhaseStart(GetAttackerCard().GetAttributes<ALCardDTO>().id, GetAttackedCard().GetAttributes<ALCardDTO>().id);
            UpdateRemotePlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
            return;
        }
        await userPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        GD.Print($"[OnAttackGuardStartHandler]");
    }

    async Task OnAttackGuardEndHandler(Player guardingPlayer)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[OnAttackGuardEndHandler] Guard phase ended without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        await guardingPlayer.SetPlayState(EPlayState.Wait);
        if (!attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnAttackGuardEndHandler");
            UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
            ALNetwork.Instance.SyncGuardPhaseEnd();
            GD.Print($"[OnAttackGuardEndHandler] Sent guard end to attacker.");
            return;
        }
        await ResolveGuardPhaseLocally();
    }

    async Task OnRetaliationHandler(Player damagedPlayer, Card retaliatingCard)
    {
        await damagedPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectRetaliationUnit);
        ALHand hand = damagedPlayer.GetPlayerHand<ALHand>();
        if (hand.GetSelectedCard<Card>(damagedPlayer) is null)
        {
            throw new System.InvalidOperationException($"[OnRetaliationHandler] No selected card on board {hand.Name} for player {damagedPlayer.Name}.");
        }
        damagedPlayer.SelectBoard(damagedPlayer, hand);
        await retaliatingCard.TryToTriggerCardEffect(ALCardEffectTrigger.Retaliation);
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }

    async Task OnRetaliationCancel(Player damagedPlayer)
    {
        await damagedPlayer.GoBackInHistoryState();
        UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
    }

    async Task OnGuardProvidedHandler(Player guardingPlayer, Card card)
    {
        GetAttackedCard().AddModifier(new AttributeModifier()
        {
            Id = "Guard",
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = card.GetAttributes<ALCardDTO>().supportValue,
            StackableModifier = true
        });
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsBattleSupported);
        GD.Print($"[OnGuardProvidedHandler] Add Guard Modifier for {GetAttackedCard().GetAttributes<ALCardDTO>().name}");
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        if (!attackedIsEnemy)
        {
            EnsureEnemyPeerId("OnGuardProvidedHandler");
            ALNetwork.Instance.SyncGuardProvided(card.GetAttributes<ALCardDTO>().id);
        }
    }

    async Task OnAttackEndHandler(Player guardingPlayer)
    {
        await FinishBattleResolution();
        GD.Print($"[OnAttackEndHandler]");
    }

    void EnsureEnemyPeerId(string context)
    {
        if (enemyPeerId != 0)
        {
            return;
        }
        throw new System.InvalidOperationException($"[{context}] Enemy peer id is not registered.");
    }

    void SetAttackContextFromRemote(string attackerCardId, string attackedCardId)
    {
        attackerCard = FindBoardCardById(attackerCardId);
        attackedCard = FindBoardCardById(attackedCardId);
    }

    ALCard FindBoardCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[FindBoardCardById] Card id is required.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>() ?? throw new System.InvalidOperationException("[FindBoardCardById] Player board is missing.");
        List<Card> cards = board.GetCardsInTree();
        foreach (Card card in cards)
        {
            if (card is not ALCard alCard)
            {
                continue;
            }
            if (alCard.GetAttributes<ALCardDTO>().id == cardId)
            {
                return alCard;
            }
        }
        throw new System.InvalidOperationException($"[FindBoardCardById] Card id not found: {cardId}");
    }

    ALCard FindCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[FindCardById] Card id is required.");
        }
        ALCard boardCard = TryFindBoardCardById(cardId);
        if (boardCard is not null) return boardCard;
        ALHand hand = userPlayer.GetPlayerHand<ALHand>();
        if (hand is not null)
        {
            List<Card> handCards = hand.GetCardsInTree();
            foreach (Card card in handCards)
            {
                if (card is ALCard alCard && alCard.GetAttributes<ALCardDTO>().id == cardId)
                {
                    return alCard;
                }
            }
        }
        ALHand enemyHand = userPlayer.GetEnemyPlayerHand<ALHand>();
        if (enemyHand is not null)
        {
            List<Card> enemyHandCards = enemyHand.GetCardsInTree();
            foreach (Card card in enemyHandCards)
            {
                if (card is ALCard alCard && alCard.GetAttributes<ALCardDTO>().id == cardId)
                {
                    return alCard;
                }
            }
        }
        throw new System.InvalidOperationException($"[FindCardById] Card id not found: {cardId}");
    }

    ALCard TryFindBoardCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new System.InvalidOperationException("[TryFindBoardCardById] Card id is required.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        if (board is null)
        {
            throw new System.InvalidOperationException("[TryFindBoardCardById] Player board is missing.");
        }
        List<Card> cards = board.GetCardsInTree();
        foreach (Card card in cards)
        {
            if (card is not ALCard alCard)
            {
                continue;
            }
            if (alCard.GetAttributes<ALCardDTO>().id == cardId)
            {
                return alCard;
            }
        }
        return null;
    }

    async Task ApplyRemoteGuard(string guardCardId)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ApplyRemoteGuard] No attack is in progress.");
        }
        if (!database.cards.TryGetValue(guardCardId, out ALCardDTO guardCard))
        {
            throw new System.InvalidOperationException($"[ApplyRemoteGuard] Guard card id not found: {guardCardId}");
        }
        GetAttackedCard().AddModifier(new AttributeModifier()
        {
            Id = "Guard",
            AttributeName = "Power",
            Duration = ALCardEffectDuration.CurrentBattle,
            Amount = guardCard.supportValue,
            StackableModifier = true
        });
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsBattleSupported);
        GD.Print($"[ApplyRemoteGuard] Add Guard Modifier for {GetAttackedCard().GetAttributes<ALCardDTO>().name}");
    }

    async Task ResolveGuardPhaseFromRemote()
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveGuardPhaseFromRemote] Guard phase ended without an active attack.");
        }
        await ResolveGuardPhaseLocally();
    }

    async Task ResolveGuardPhaseLocally()
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveGuardPhaseLocally] Guard phase ended without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        bool isAttackSuccessful = GetBattleOutcome(GetAttackerCard(), GetAttackedCard());
        if (attackedIsEnemy)
        {
            EnsureEnemyPeerId("ResolveGuardPhaseLocally");
            ALNetwork.Instance.SyncBattleResolution(GetAttackerCard().GetAttributes<ALCardDTO>().id, GetAttackedCard().GetAttributes<ALCardDTO>().id, isAttackSuccessful);
        }
        ALPlayer attackerPlayer = GetAttackerCard().GetOwnerPlayer<ALPlayer>();
        await attackerPlayer.SetPlayState(EPlayState.Wait);
        await GetAttackedCard().TryToTriggerCardEffect(ALCardEffectTrigger.IsAttacked);
        await attackerPlayer.SettleBattle(playerUI);
        GD.Print($"[ResolveGuardPhaseLocally]");
    }

    async Task ResolveBattleFromRemote(bool isAttackSuccessful)
    {
        if (!IsAttackInProgress())
        {
            throw new System.InvalidOperationException("[ResolveBattleFromRemote] Battle resolution without an active attack.");
        }
        ALBoard board = userPlayer.GetPlayerBoard<ALBoard>();
        bool attackedIsEnemy = board.IsEnemyCard(GetAttackedCard());
        System.Func<ALCard, Task> applyFlagshipDamage = _ => Task.CompletedTask;
        System.Func<ALCard, Task> destroyUnit = _ => Task.CompletedTask;
        if (!attackedIsEnemy)
        {
            applyFlagshipDamage = card =>
            {
                GD.Print($"[ResolveBattleFromRemote] {card.Name} Takes durability damage!");
                card.TakeDurabilityDamage();
                return Task.CompletedTask;
            };
            destroyUnit = async card =>
            {
                GD.Print($"[ResolveBattleFromRemote] {card.Name} destroyed!");
                await ALPlayer.DestroyUnitCard(card);
            };
        }
        await ApplyBattleResolution(GetAttackerCard(), GetAttackedCard(), isAttackSuccessful, applyFlagshipDamage, destroyUnit);
        await FinishBattleResolution();
    }

    static bool GetBattleOutcome(ALCard attacker, ALCard attacked)
    {
        float attackerPower = attacker.GetAttributeWithModifiers<ALCardDTO>("Power");
        float attackedPower = attacked.GetAttributeWithModifiers<ALCardDTO>("Power");
        return attackerPower >= attackedPower;
    }

    public async Task ApplyBattleResolution(ALCard attacker, ALCard attacked, bool isAttackSuccessful, System.Func<ALCard, Task> applyFlagshipDamage, System.Func<ALCard, Task> destroyUnit)
    {
        if (attacker is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Attacker is required.");
        }
        if (attacked is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Attacked card is required.");
        }
        if (applyFlagshipDamage is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Flagship damage handler is required.");
        }
        if (destroyUnit is null)
        {
            throw new System.InvalidOperationException("[ApplyBattleResolution] Destroy unit handler is required.");
        }
        await playerUI.OnSettleBattleUI(attacker, attacked, isAttackSuccessful);
        if (!isAttackSuccessful)
        {
            return;
        }
        if (attacked.GetIsAFlagship())
        {
            await applyFlagshipDamage(attacked);
            return;
        }
        await destroyUnit(attacked);
    }

    async Task FinishBattleResolution()
    {
        await userPlayer.TryToExpireCardsModifierDuration(ALCardEffectDuration.CurrentBattle);
        attackerCard = null;
        attackedCard = null;
    }
}
