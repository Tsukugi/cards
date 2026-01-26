# Match Against AI (Design + Implementation Notes)

This document describes the recommended design for a single-click “Match against
AI” flow using a single-process “Local AI peer” mode.

The goal is to avoid spawning a second process and still reuse as much of the
existing match and AI code as possible.

## Overview

### Recommended: Local AI Peer (Single Process)

Instead of launching a second executable, run both players in the same scene:
- The user player remains the controlled player.
- The opponent is a real `ALPlayer` instance driven by `ALBasicAI`.
- Networking sync calls are bypassed in this mode.

This removes editor/export brittleness and works for compiled builds without
special launcher logic.

## User Experience

Add a new button in the main menu:
- Label: `Match against AI`

Expected behavior when clicked:
1. The game starts a special “Local AI” match mode.
2. The AI opponent is created locally.
3. The AI takes actions automatically on its turns.

## Configuration Model

### New debug setting: `PlayAsAI`

Add a boolean field to `ALMatchDebugSettings`:
- Name: `PlayAsAI`
- Type: `bool`
- Default: `false`

File:
- `scripts/utils/ALLocalStorage.cs`

Rationale:
- The project already uses profile-scoped debug settings. `PlayAsAI` can still
  be useful for local debugging and testing AI behavior.

### New match mode: `LocalAI`

Add an explicit match mode (or flag) to the match manager:
- Name: `LocalAI`
- Type: `bool` (or enum-backed mode)
- Default: `false`

Suggested location:
- `AzurLane/scripts/ALGameMatchManager.cs`
- or a profile-scoped setting similar to debug settings

Rationale:
- `PlayAsAI` is a per-client control flag.
- `LocalAI` is a match-level topology flag (single-process vs networked).

## Runtime Behavior

### Where AI should be triggered

AI should run only when all of the following are true:
- It is the local player’s turn on that client.
- Either:
  - the local profile’s debug settings have `PlayAsAI == true`, or
  - the match is in `LocalAI` mode and the turn owner is the AI player.

Best hook:
- `ALGameMatchManager.StartLocalTurnIfNeeded()`

File:
- `AzurLane/scripts/ALGameMatchManager.Network.cs`

Why here:
- This method is already the turn boundary where a client decides whether to begin its own turn. It is the narrowest, safest place to attach AI turn automation without changing input or phase logic.

Suggested behavior:
1. Call `userPlayer.StartTurn()` as usual.
2. If `PlayAsAI` is enabled for this client, defer-call the AI controller:
   - `userPlayer.GetPlayerAIController().StartTurn()`

Notes:
- Do not rely on input simulation RPCs; the AI already uses explicit gameplay actions.

## Local AI Peer Mode (Single Process)

This section describes how to implement AI locally without spawning a second
process.

### Core idea

In `LocalAI` mode:
- Use a real `ALPlayer` as the opponent (not `ALRemotePlayer`).
- Run both players in the same scene tree.
- Avoid network RPCs for gameplay sync and call local handlers instead.

### Minimal topology changes

1. Create or reuse an enemy player node
- Instantiate a second `ALPlayer` for the enemy side.
- Ensure it is not a controlled player.

2. Register both players in the match manager
- Maintain an explicit `localEnemyPlayer` reference.
- Treat it as the remote opponent for gameplay purposes.

3. Drive AI on enemy turns
- When the enemy player becomes the turn owner:
  - call `enemyPlayer.StartTurn()`
  - then call `enemyPlayer.GetPlayerAIController().StartTurn()` (deferred)

### Bypassing networking safely

The current codebase mixes “gameplay action” with “network sync.” In `LocalAI`
mode, the sync calls should be bypassed.

Recommended approach:
- Introduce a small “match sync” adapter that abstracts network sync calls.

For example:
- `IMatchSync` interface (send draw, send turn end, send battle resolution, etc.)
- `NetworkMatchSync` implementation that calls `ALNetwork.Instance.*`
- `LocalMatchSync` implementation that calls the corresponding local handlers
  directly in the same process

This isolates networking concerns without rewriting the gameplay logic.

## Main Menu Integration

### New action: “Match against AI”

Files:
- Scene: `AzurLane/scenes/main.tscn`
- Script: `AzurLane/scripts/MainScene/ALMain.cs`

Changes:
1. Add a new `Button` node in `main.tscn` under `Panel/Vert`.
2. Export a reference in `ALMain.cs` (for example, `matchAgainstAIBtn`).
3. Wire it in `_Ready()` with a handler such as `OnMatchAgainstAIPressed()`.

## Suggested Implementation Checklist

This section is a concrete engineering checklist that matches the design above.

### Recommended path: Local AI peer

1. Add a match mode flag
- Add `LocalAI` (bool or enum) in `ALGameMatchManager`.

2. Add a local enemy player reference
- Add `ALPlayer enemyPlayer` (or similar) in `ALGameMatchManager`.

3. Introduce a sync adapter
- Add `IMatchSync` with at least the calls you already send through `ALNetwork`.
- Provide `NetworkMatchSync` and `LocalMatchSync`.
- Use `LocalMatchSync` when `LocalAI` is enabled.

4. Trigger AI on the enemy turn
- In `StartLocalTurnIfNeeded()`:
  - if turn owner is the user, behave normally
  - if turn owner is the AI enemy, start the AI turn locally

5. Wire the main menu button to Local AI mode
- `Match against AI` should set `LocalAI = true` and skip Create/Join flows.

### Minimal implementation (fastest viable)

If you want the smallest diff first:
1. Add `LocalAI` to the match manager.
2. Add an enemy `ALPlayer` in the match scene.
3. In `StartLocalTurnIfNeeded()`:
   - start user turn normally
   - when it is the enemy turn, start the enemy AI locally
4. Guard key network sync calls with:
   - “if LocalAI, do local equivalent; else, call ALNetwork”

This is not as clean as a sync adapter but is usually the fastest way to prove
the approach works.

### Reusable code points

To keep this maintainable and reusable:
- Centralize topology choice in one place (`LocalAI` vs network).
- Centralize sync policy behind an adapter (`IMatchSync`).
- Keep AI trigger logic at turn boundaries (`StartLocalTurnIfNeeded()`).
- Prefer reusing existing handlers (for example, use existing “HandleOn*”
  methods where possible instead of adding parallel code paths).

### Existing items still useful

1. Add `PlayAsAI` to match debug settings
- Update `ALMatchDebugSettings` in `scripts/utils/ALLocalStorage.cs`.

2. Expose `PlayAsAI` in the debug helper
- File: `AzurLane/scripts/ALDebug.cs`
- Add a field for `playAsAI`.
- Load/save it using `ALLocalStorage.LoadMatchDebugSettings()`.
- Provide a getter such as `GetPlayAsAI()`.

3. Trigger AI turns using the debug setting
- File: `AzurLane/scripts/ALGameMatchManager.Network.cs`
- In `StartLocalTurnIfNeeded()`:
  - Call `userPlayer.StartTurn()`.
  - If `debug.GetPlayAsAI()` is true, call the AI controller with a deferred call.

4. Add the main menu button
- File: `AzurLane/scenes/main.tscn`
- Add a `Match against AI` button under `Panel/Vert`.
- Update the root `node_paths` and script exports accordingly.

5. Implement the main menu handler
- File: `AzurLane/scripts/MainScene/ALMain.cs`
- Add `OnMatchAgainstAIPressed()` that:
  - ensures no active connection,
  - enables `LocalAI` match mode,
  - starts the match scene directly.

## Testing Guidance

### Manual verification steps

On Windows:
1. Run the project normally.
2. Click `Match against AI`.
3. Confirm:
   - the match begins immediately,
   - the AI takes actions on its turns,
   - no second process is required.

### Useful diagnostics

- Godot output/logs for both processes.
- The `PeerId` label in `match.tscn` UI.
- Any thrown `InvalidOperationException` messages.

## Risks and Notes

- The main risk in single-process mode is accidental divergence between local and
  networked behavior. Centralizing sync policy helps reduce this.

## Related Files

- Main menu scene: `AzurLane/scenes/main.tscn`
- Main menu script: `AzurLane/scripts/MainScene/ALMain.cs`
- Match manager (turn boundary): `AzurLane/scripts/ALGameMatchManager.Network.cs`
- Debug helper: `AzurLane/scripts/ALDebug.cs`
- Local storage + settings: `scripts/utils/ALLocalStorage.cs`
- AI controller: `AzurLane/scripts/AzurLaneAI/ALBasicAI.cs`
