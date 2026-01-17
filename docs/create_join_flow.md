# Create/Join Game Flow (Current)

This document captures how the Create Game and Join Game flows work in the
current Cards (Godot) project.

## Entry Points (UI)

- Scene: `AzurLane/scenes/main.tscn`
- Script: `AzurLane/scripts/MainScene/ALMain.cs`

Buttons and handlers:
- `Create Game` calls `OnCreateGamePressed()`
- `Join Game` calls `OnJoinPressed()`
- `Cancel` in the lobby panel calls `ALLobbyUI.OnCancelHandler()`

## Create Game Flow

1) `OnCreateGamePressed()` in `AzurLane/scripts/MainScene/ALMain.cs`:
   - Disables Create/Join buttons.
   - Shows the lobby panel (`LobbyManager.Visible = true`).
   - Sets `isGameCreated = true`.
   - Calls `Network.Instance.CreateGame()`.

2) `Network.CreateGame()` in `scripts/card/Network.cs`:
   - Creates an ENet server on port 7000 with max 20 connections.
   - Sets `Multiplayer.MultiplayerPeer` to the server peer.
   - Seeds `_players[1] = _playerInfo` for the host.
   - Emits `PlayerConnected` for the host peer.

3) `ALLobbyUI.OnPlayerConnected()` in `AzurLane/scripts/ALLobbyUI.cs`:
   - Adds the player entry to the lobby list.

## Join Game Flow

1) `OnJoinPressed()` in `AzurLane/scripts/MainScene/ALMain.cs`:
   - Disables Start/Create/Join buttons.
   - Shows the lobby panel (`LobbyManager.Visible = true`).
   - Calls `Network.Instance.JoinGame()` using `Network.DefaultServerIP`
     (default is `127.0.0.1`).

2) `Network.JoinGame()` in `scripts/card/Network.cs`:
   - Creates an ENet client to the given address.
   - Sets `Multiplayer.MultiplayerPeer` to the client peer.

3) On successful connection:
   - `Network.OnConnectOk()` runs and adds local player info to `_players`.
   - Emits `PlayerConnected` for the local peer.
   - `ALLobbyUI.OnPlayerConnected()` adds the local entry to the lobby list.

## Lobby UI Behavior

- Script: `AzurLane/scripts/ALLobbyUI.cs`
- Node: `LobbyManager` in `AzurLane/scenes/main.tscn`

Key behavior:
- The lobby list updates on `PlayerConnected` and `PlayerDisconnected`.
- `Cancel` clears the list, closes the network connection, and triggers
  `ALMain.OnExitLobbyHandler()` to re-enable menu buttons.

## Related Notes

- `ALMainDebug.AutoSyncStart()` in
  `AzurLane/scripts/MainScene/ALMainDebug.cs` can auto-join after a delay if a
  game is not created.
- Network constants live in `scripts/card/Network.cs`:
  - Port: 7000
  - Default IP: 127.0.0.1
  - Max connections: 20

