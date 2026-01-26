using System.Threading.Tasks;
using Godot;

public partial class ALGameMatchManager
{
    async void HandleOnSendMatchPhaseEvent(int peerId, int phase)
    {
        GD.Print($"[HandleOnSendMatchPhaseEvent] To {userPlayer.MultiplayerId}: From {peerId}: {phase}");
        matchCurrentPhase = (EALTurnPhase)phase;
        await Task.CompletedTask;
    }

    async void HandleOnSendPlayStateEvent(int peerId, EPlayState state, string interactionState)
    {
        GD.Print($"[HandleOnSendPlayStateEvent] To {userPlayer.MultiplayerId}: Update {peerId} - {state} - {interactionState}");
        await RunForRemotePeer(peerId, nameof(HandleOnSendPlayStateEvent), () =>
        {
            remotePlayer.SetRemotePlayState(state, interactionState);
            return Task.CompletedTask;
        });
    }

    async void HandleOnSyncFlagship(int peerId, string cardId)
    {
        await RunForRemotePeer(peerId, nameof(HandleOnSyncFlagship), () =>
        {
            ALCardDTO synchedCard = database.cards[cardId];
            GD.Print($"[HandleOnSyncFlagship] To {userPlayer.MultiplayerId}: From {peerId}: {synchedCard.name}");
            userPlayer.UpdateEnemyFlagship(synchedCard);
            return Task.CompletedTask;
        });
    }

    async void HandleOnSyncDurabilityDamage(int peerId, string cardId)
    {
        await RunForRemotePeer(peerId, nameof(HandleOnSyncDurabilityDamage), () =>
        {
            ALCardDTO synchedCard = database.cards[cardId];
            GD.Print($"[HandleOnSyncDurabilityDamage] To {userPlayer.MultiplayerId}: From {peerId}: {synchedCard.name}");
            userPlayer.ApplyEnemyDurabilityDamage(synchedCard);
            return Task.CompletedTask;
        });
    }

    async void HandleOnDrawCardEvent(int peerId, string cardId, ALDrawType drawType)
    {
        await RunForRemotePeer(peerId, nameof(HandleOnDrawCardEvent), async () =>
        {
            ALCardDTO synchedCard = database.cards[cardId];
            GD.Print($"[HandleOnDrawCardEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {synchedCard.name} - {drawType}");
            switch (drawType)
            {
                case ALDrawType.Deck:
                    userPlayer.DrawFromEnemyDeck();
                    userPlayer.AddEnemyCardToHand(synchedCard);
                    break;
                case ALDrawType.Cube:
                    userPlayer.DrawFromEnemyCubeDeck();
                    await userPlayer.PlaceEnemyCubeToBoard(synchedCard);
                    break;
                case ALDrawType.Durability:
                    userPlayer.DrawFromEnemyDeck();
                    await userPlayer.PlaceEnemyDurabilityCard(synchedCard);
                    break;
            }
        });
    }

    async void HandleOnSyncPlaceCard(int peerId, string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[HandleOnSyncPlaceCard] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} {boardName} {fieldPath}");
        await RunForRemotePeer(peerId, nameof(HandleOnSyncPlaceCard), async () =>
        {
            if (!database.cards.TryGetValue(cardId, out ALCardDTO synchedCard))
            {
                throw new System.InvalidOperationException($"[HandleOnSyncPlaceCard] Card id not found: {cardId}");
            }
            await userPlayer.PlaceEnemyCardToBoard(synchedCard, boardName, fieldPath);
        });
    }

    async void HandleOnSyncPlaceCardGuard(int peerId, string cardId, string boardName, string fieldPath)
    {
        GD.Print($"[HandleOnSyncPlaceCardGuard] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} {boardName} {fieldPath}");
        await RunForRemotePeer(peerId, nameof(HandleOnSyncPlaceCardGuard), async () =>
        {
            if (!database.cards.TryGetValue(cardId, out ALCardDTO synchedCard))
            {
                throw new System.InvalidOperationException($"[HandleOnSyncPlaceCardGuard] Card id not found: {cardId}");
            }
            await userPlayer.PlaceEnemyCardToBoard(synchedCard, boardName, fieldPath);
        });
    }

    async void HandleOnGuardPhaseStartEvent(int peerId, string attackerCardId, string attackedCardId)
    {
        GD.Print($"[HandleOnGuardPhaseStartEvent] To {userPlayer.MultiplayerId}: From {peerId}: attacker={attackerCardId} attacked={attackedCardId}");
        await RunForRemotePeer(peerId, nameof(HandleOnGuardPhaseStartEvent), async () =>
        {
            SetAttackContextFromRemote(attackerCardId, attackedCardId);
            await userPlayer.SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectGuardingUnit);
            UpdateRemotePlayState(EPlayState.Wait, ALInteractionState.AwaitOtherPlayerInteraction);
        });
    }

    async void HandleOnGuardPhaseEndEvent(int peerId)
    {
        GD.Print($"[HandleOnGuardPhaseEndEvent] To {userPlayer.MultiplayerId}: From {peerId}");
        await RunForRemotePeer(peerId, nameof(HandleOnGuardPhaseEndEvent), ResolveGuardPhaseFromRemote);
    }

    async void HandleOnGuardProvidedEvent(int peerId, string guardCardId)
    {
        GD.Print($"[HandleOnGuardProvidedEvent] To {userPlayer.MultiplayerId}: From {peerId}: guard={guardCardId}");
        await RunForRemotePeer(peerId, nameof(HandleOnGuardProvidedEvent), () => ApplyRemoteGuard(guardCardId));
    }

    async void HandleOnBattleResolutionEvent(int peerId, string attackerCardId, string attackedCardId, bool isAttackSuccessful)
    {
        GD.Print($"[HandleOnBattleResolutionEvent] To {userPlayer.MultiplayerId}: From {peerId}: attacker={attackerCardId} attacked={attackedCardId} success={isAttackSuccessful}");
        await RunForRemotePeer(peerId, nameof(HandleOnBattleResolutionEvent), async () =>
        {
            SetAttackContextFromRemote(attackerCardId, attackedCardId);
            await ResolveBattleFromRemote(isAttackSuccessful);
        });
    }

    async void HandleOnCardActiveStateEvent(int peerId, string cardId, bool isActive)
    {
        GD.Print($"[HandleOnCardActiveStateEvent] To {userPlayer.MultiplayerId}: From {peerId}: {cardId} active={isActive}");
        await RunForRemotePeer(peerId, nameof(HandleOnCardActiveStateEvent), () =>
        {
            ALCard targetCard = FindCardById(cardId);
            targetCard.SetIsInActiveState(isActive, false);
            return Task.CompletedTask;
        });
    }

    async void HandleOnCardSelectEvent(int peerId, int targetOwnerPeerId, string boardName, Vector2I position)
    {
        GD.Print($"[HandleOnCardSelectEvent] To {userPlayer.MultiplayerId}: From {peerId}: owner={targetOwnerPeerId} -> {boardName} - {position}");
        await RunForRemotePeer(peerId, nameof(HandleOnCardSelectEvent), () =>
        {
            Board board = ResolveSelectionBoard(boardName, targetOwnerPeerId);
            if (board is null)
            {
                return Task.CompletedTask;
            }
            if (remoteSelectedBoard is not null && remoteSelectedBoard != board)
            {
                remoteSelectedBoard.ClearSelectionForPlayer(remotePlayer);
            }
            Vector2I mappedPosition = position;
            if (board is ALBoard alBoard)
            {
                mappedPosition = alBoard.MapToOppositeSidePosition(position);
            }
            GD.Print($"[HandleOnCardSelectEvent.Resolve] selector={remotePlayer.Name}({remotePlayer.MultiplayerId}) board={board.Name} pos={position} mapped={mappedPosition}");
            board.SelectCardField(remotePlayer, mappedPosition, false);
            remoteSelectedBoard = board;
            return Task.CompletedTask;
        }, true);
    }

    Board ResolveSelectionBoard(string boardName, int targetOwnerPeerId)
    {
        if (boardName == "Board")
        {
            Board board = userPlayer.GetPlayerBoard<PlayerBoard>() ?? throw new System.InvalidOperationException($"[ResolveSelectionBoard] Board '{boardName}' not found for player {userPlayer.Name}.");
            return board;
        }
        if (boardName == "Hand")
        {
            if (targetOwnerPeerId != userPlayer.MultiplayerId)
            {
                Board enemyHand = userPlayer.GetEnemyPlayerHand<PlayerHand>() ?? throw new System.InvalidOperationException($"[ResolveSelectionBoard] Enemy hand not found for remote selection. owner={targetOwnerPeerId} local={userPlayer.MultiplayerId}");
                return enemyHand;
            }
            Board board = userPlayer.GetPlayerHand<PlayerHand>() ?? throw new System.InvalidOperationException($"[ResolveSelectionBoard] Hand '{boardName}' not found for player {userPlayer.Name}.");
            return board;
        }
        throw new System.InvalidOperationException($"[ResolveSelectionBoard] Unknown board name '{boardName}'.");
    }

    async void HandleOnInputActionEvent(int peerId, InputAction inputAction)
    {
        GD.Print($"[HandleOnInputActionEvent] To {userPlayer.MultiplayerId}: From {peerId}: -> {inputAction}");
        await RunForRemotePeer(peerId, nameof(HandleOnInputActionEvent), () =>
        {
            GD.Print($"[HandleOnInputActionEvent] Remote input ignored; apply explicit sync updates instead.");
            return Task.CompletedTask;
        });
    }

    async void HandleOnTurnEndEvent(int peerId)
    {
        if (peerId == userPlayer.MultiplayerId)
        {
            GD.PushError("[HandleOnTurnEndEvent] Local peer cannot end its own turn via network event.");
            return;
        }
        if (currentTurnPeerId != 0 && currentTurnPeerId != peerId)
        {
            GD.PushError("[HandleOnTurnEndEvent] Another peer is trying to end their turn");
            return;
        }
        GD.Print($"[HandleOnTurnEndEvent] To {userPlayer.MultiplayerId}: From {peerId}: Finishes its turn");
        AdvanceTurnOwner();
        StartLocalTurnIfNeeded();
        await Task.CompletedTask;
    }

    void EnsureRemotePlayer(int peerId)
    {
        if (peerId <= 0)
        {
            throw new System.InvalidOperationException($"[EnsureRemotePlayer] Invalid peer id {peerId}.");
        }
        if (remotePlayer is not null)
        {
            if (remotePlayer.MultiplayerId != peerId)
            {
                throw new System.InvalidOperationException($"[EnsureRemotePlayer] Remote player id mismatch. Current={remotePlayer.MultiplayerId} New={peerId}.");
            }
            return;
        }
        remotePlayer = new ALRemotePlayer();
        remotePlayer.Initialize(peerId, "Enemy", RemotePlayerColor);
    }

    void UpdateRemotePlayState(EPlayState state, string interactionState)
    {
        if (enemyPeerId == 0)
        {
            return;
        }
        if (remotePlayer is null)
        {
            throw new System.InvalidOperationException("[UpdateRemotePlayState] Remote player is not registered.");
        }
        remotePlayer.SetRemotePlayState(state, interactionState);
    }

    void AdvanceTurnOwner()
    {
        if (enemyPeerId == 0)
        {
            throw new System.InvalidOperationException("[AdvanceTurnOwner] Enemy peer id is not registered.");
        }
        if (currentTurnPeerId == userPlayer.MultiplayerId)
        {
            currentTurnPeerId = enemyPeerId;
            return;
        }
        currentTurnPeerId = userPlayer.MultiplayerId;
    }

    void StartLocalTurnIfNeeded()
    {
        if (!IsLocalTurn()) return;
        userPlayer.StartTurn();
    }

    public void RegisterEnemyPeer(int peerId)
    {
        if (peerId <= 0)
        {
            throw new System.InvalidOperationException($"[RegisterEnemyPeer] Invalid peer id {peerId}.");
        }
        if (enemyPeerId != 0 && enemyPeerId != peerId)
        {
            throw new System.InvalidOperationException($"[RegisterEnemyPeer] Enemy peer already set to {enemyPeerId}, got {peerId}.");
        }
        enemyPeerId = peerId;
        EnsureRemotePlayer(peerId);
        if (currentTurnPeerId < 0)
        {
            currentTurnPeerId = enemyPeerId;
        }
        GD.Print($"[RegisterEnemyPeer] enemyPeerId={enemyPeerId} currentTurnPeerId={currentTurnPeerId}");
    }

    async Task RunForRemotePeer(int peerId, string context, System.Func<Task> action, bool logSelfIgnore = false)
    {
        if (peerId == userPlayer.MultiplayerId)
        {
            if (logSelfIgnore)
            {
                GD.Print($"[{context}] Ignore self event from {peerId}.");
            }
            await Task.CompletedTask;
            return;
        }
        if (action is null)
        {
            throw new System.InvalidOperationException($"[{context}] Remote action is required.");
        }
        RegisterEnemyPeer(peerId);
        await action();
    }
}
