# Azur Lane subproject

Azur Lane-themed card game prototype inside the larger Godot Cards project.
This folder contains the scenes, scripts, assets, shaders, and test scenes used
by the Azur Lane flow.

## Game overview (player-facing)
- Two-player match with a flagship and a deck of ship/event cards.
- Spend cube resources to play cards and build a battlefield.
- Take turns through phases, attack opposing units, and protect your flagship.
- Deal durability damage to the enemy flagship to win.

## Game overview (dev-facing)
- Turn-based match loop with phases: Reset, Preparation, Main, Battle, End.
- Resources are "cubes" drawn from a cube deck; spending cubes powers card plays.
- Units have active/inactive states (tapped) and row-based attack rules.
- Combat supports guard and retaliation interactions plus effect triggers.
- Multiplayer flows supported via `ALNetwork` (create/join, sync phases, sync draws).

## Entry scenes
- `AzurLane/scenes/main.tscn`: main menu for the prototype (also the project main scene)
- `AzurLane/scenes/match.tscn`: match scene used when starting a game

## Data
- `AzurLane/database/Cards.json`: card definitions
- `AzurLane/database/Decks.json`: deck lists

## Scripts
- `AzurLane/scripts/`: C# gameplay logic, UI, and networking for Azur Lane
- `AzurLane/scripts/MainScene/ALMain.cs`: main menu controller
- `AzurLane/scripts/ALNetwork.cs`: networking autoload used by the prototype

## Assets and shaders
- `AzurLane/res/`: card art, board textures, and UI images
- `AzurLane/shader/`: Azur Lane-specific shaders and materials

## Tests
- `AzurLane/tests/Tests.tscn`: test runner scene for Azur Lane scripts
