# Network API Documentation

## Overview
The network architecture consists of a base `Network` class that handles general multiplayer functionality, and an extended `ALNetwork` class that handles Azur Lane-specific game logic.

## Configuration Constants
- **Port**: 7000
- **Default Server IP**: "127.0.0.1" (localhost)
- **Max Connections**: 20

## Connection Management API

### Connection Methods
- `JoinGame(string address = "")` - Connects to a server at the specified address (defaults to DefaultServerIP)
- `CreateGame()` - Creates a local server to host a game
- `CheckConnection()` - Checks if the connection is active
- `GetPlayerCount()` - Gets the number of connected players

### Disconnection Methods
- `CloseConnection()` - Closes the current connection
- `RemoveMultiplayerPeer()` - Internal method that closes connection and clears player data

## Communication API

### Base Network Communication Methods
- `SendInputAction(InputAction action)` - Sends an input action to other players
- `RequestStartMatch(string path)` - Requests to start a match with a specific scene path
- `SendPlayOrder(int order)` - Sends play order information
- `DrawCard(string cardId)` - Sends a card draw action
- `SendPlayState(int peerId, int state, string interactionState)` - Sends player state information
- `SendSelectCardField(int peerId, Board board, Vector2I position)` - Sends card selection on a board
- `SendTurnEnd()` - Sends turn end notification
- `PollPing()` - Continuously sends pings (async method)

### ALNetwork Extended Communication Methods
- `RegisterMatchPlayer()` - Registers a player in the match
- `SendDeckSet(string userPlayerDeckSetId)` - Sends the selected deck for the match
- `ALDrawCard(string cardId, ALDrawType drawType)` - Draws a card with a specific draw type
- `SyncFlagship(string cardId)` - Synchronizes flagship card information
- `SyncDurabilityDamage(string cardId)` - Synchronizes a flagship durability damage draw to hand
- `SendMatchPhase(int matchPhase)` - Sends the current match phase
- `SyncPlaceCard(string cardId, string boardName, string fieldPath)` - Synchronizes a card placement using a board-relative field path
- `SyncPlaceCardGuard(string cardId, string boardName, string fieldPath)` - Synchronizes a guard card placement
- `SyncGuardPhaseStart(string attackerCardId, string attackedCardId)` - Starts guard phase on the defending client
- `SyncGuardPhaseEnd()` - Ends guard phase on the defending client
- `SyncGuardProvided(string guardCardId)` - Sends the selected guard card for battle support
- `SyncBattleResolution(string attackerCardId, string attackedCardId, bool isAttackSuccessful)` - Syncs battle outcome and animation
- `SyncCardActiveState(string cardId, bool isActive)` - Syncs active/inactive state for a card
- `OnMatchStart()` - Initializes match start procedures

## Signal Definitions
- `PlayerConnectedEventHandler(int peerId, Godot.Collections.Dictionary<string, string> playerInfo)` - Triggered when a player connects
- `PlayerDisconnectedEventHandler(int peerId)` - Triggered when a player disconnects
- `ServerDisconnectedEventHandler()` - Triggered when disconnected from server

## Event Delegates (Callbacks)

### Base Network Events
- `PlayerInputActionEvent(int peerId, InputAction inputAction)` - Event for player input actions
- `PlayerCardEvent(int peerId, string cardId)` - Event for player card actions
- `PlayerSelectCardEvent(int peerId, string boardType, bool isEnemyBoard, Vector2I position)` - Event for player card selection
- `PlayerOrderEvent(int peerId, int order)` - Event for player order changes
- `PlayerPlayStateEvent(int peerId, EPlayState state, string interactionState)` - Event for player state changes
- `ALPlayerEvent(int peerId)` - Event for Azur Lane player actions

### ALNetwork Events
- `ALPlayerMatchPhaseEvent(int peerId, int matchPhase)` - Event for match phase changes
- `ALPlayerSyncCardEvent(int peerId, string cardId)` - Event for card synchronization
- `ALPlayerDrawEvent(int peerId, string cardId, ALDrawType drawType)` - Event for card draw actions
- `ALPlayerSyncCardEvent(int peerId, string cardId)` - Event for durability damage sync (emitted as OnSyncDurabilityDamageEvent)
- `ALPlayerSyncPlaceCardEvent(int peerId, string cardId, string boardName, string fieldPath)` - Event for placing cards using a board-relative field path
- `ALPlayerGuardPhaseStartEvent(int peerId, string attackerCardId, string attackedCardId)` - Event for starting guard phase
- `ALPlayerGuardPhaseEndEvent(int peerId)` - Event for ending guard phase
- `ALPlayerGuardProvidedEvent(int peerId, string guardCardId)` - Event for guard card selection
- `ALPlayerBattleResolutionEvent(int peerId, string attackerCardId, string attackedCardId, bool isAttackSuccessful)` - Event for battle outcome sync
- `ALPlayerCardActiveStateEvent(int peerId, string cardId, bool isActive)` - Event for active/inactive card state changes

## Internal RPC Callbacks

### Base Network RPC Callbacks
- `OnSendPlayOrder(int order)` - Handles play order from other peers
- `OnSendPlayState(int peerId, int state, string interactionState)` - Handles play state from other peers
- `OnDrawCard(string data)` - Handles card draw from other peers
- `OnSendSelectCardField(int peerId, string boardName, bool isEnemyBoard, Vector2I position)` - Handles card selection from other peers
- `OnSendInput(int inputAction)` - Handles input actions from other peers
- `OnSendTurnEnd()` - Handles turn end notifications from other peers
- `StartMatch(string gameScenePath)` - Handles match start requests
- `PlayerLoaded()` - Handles player load completion
- `RegisterPlayer(Godot.Collections.Dictionary<string, string> newPlayerInfo)` - Handles player registration
- `Ping()` - Handles ping requests

### ALNetwork RPC Callbacks
- `OnRegisterMatchPlayer()` - Handles player registration from other peers
- `OnSendDeckSet(string deckSetId)` - Handles deck set information from other peers
- `OnALDrawCard(string data, ALDrawType drawType)` - Handles card draw events from other peers
- `OnSyncFlagship(string cardId)` - Handles flagship sync from other peers
- `OnSyncDurabilityDamage(string cardId)` - Handles durability damage sync
- `OnSendMatchPhase(int matchPhase)` - Handles match phase updates from other peers
- `OnSyncPlaceCardRpc(string cardId, string boardName, string fieldPath)` - Handles card placement sync using a board-relative field path
- `OnSyncPlaceCardGuardRpc(string cardId, string boardName, string fieldPath)` - Handles guard placement sync
- `OnSyncGuardPhaseStartRpc(string attackerCardId, string attackedCardId)` - Handles guard phase start sync
- `OnSyncGuardPhaseEndRpc()` - Handles guard phase end sync
- `OnSyncGuardProvidedRpc(string guardCardId)` - Handles guard selection sync
- `OnSyncBattleResolutionRpc(string attackerCardId, string attackedCardId, bool isAttackSuccessful)` - Handles battle resolution sync
- `OnSyncCardActiveStateRpc(string cardId, bool isActive)` - Handles card active state sync

## Placement Sync Notes
- Card placement sync uses `fieldPath` from `Board.GetPathTo(selectedField)` (e.g., `Player/Units/FrontRow2`).
- Receivers map the path to the enemy board by swapping `Player/` with `EnemyPlayer/` to place the card on the mirrored field.

## Durability Damage Sync Notes
- Durability damage sync sends the revealed card id and removes the last facedown durability card from the enemy board.
- The receiving client adds the revealed card to the enemy hand as a facedown card.

## Guard + Battle Resolution Notes
- Guard phase is started on the defending client via `SyncGuardPhaseStart`.
- Defenders can cancel (guard end) or provide a guard card, which syncs back to the attacker.
- Battle resolution is synced to play attack animations on both clients and apply defender-side durability/unit cleanup.

## Card Active State Sync Notes
- `SetIsInActiveState` now syncs active/inactive changes for non-empty fields from the controlling player.
- Remote clients apply the state without re-sending it.

## Network Architecture Notes
- Uses Godot's built-in multiplayer system with ENet
- Supports both client (joining) and server (hosting) modes
- Uses RPC (Remote Procedure Calls) for communication between peers
- Uses reliable transfers for all gameplay-critical RPCs
- Maintains player information in a dictionary with peer IDs as keys

This network API enables a peer-to-peer or client-server multiplayer card game where players can connect, exchange game state information, and synchronize gameplay actions in real-time.
