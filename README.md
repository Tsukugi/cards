# Cards (Godot 4.5, C#)

Card game playground built in Godot with C# scripts. The main menu boots the
Azur Lane prototype and exposes local match, create game, and join game flows.

## Requirements
- Godot 4.5 .NET (C# build)
- .NET SDK (as required by your Godot install)

## Run
1. Open the project in Godot 4.4 .NET.
2. Press Play. The configured main scene is `res://AzurLane/scenes/main.tscn`.

## Controls (default input map)
- Accept: Enter, Space, E
- Select: Space, W, gamepad button 3
- Cancel: Esc, Q

## Testing
- Unit tests (run `AzurLane/tests/Tests.tscn`): `scripts/buildAndRun.ps1 --unit`
  - Uses the test scene runner and executes all unit tests in `Tests.tscn`.
  - Optional: add `--headless` to run Godot without a window.
- Gameplay tests (two clients, auto host/join): `scripts/buildAndRun.ps1 --test=res://AzurLane/tests/Test_ALSelectionSync.cs`
  - `--test` is for gameplay-only tests and must be the script path to the test file.
  - The script launches two clients, auto-hosts/auto-joins a match, then the test runs once the match reaches main phase.
  - Optional: add `--headless` to run Godot without a window.
- Without test flags, `scripts/buildAndRun.ps1` just launches two match clients.
- Phases are auto-skipped when `--unit` or `--test` is set.

## Project layout
- `AzurLane/`: Azur Lane prototype (scenes, scripts, database, assets, tests)
- `Vanguard/`: Vanguard board prototype
- `scenes/`: Shared scenes (card UI, phase button)
- `scripts/`: Shared C# gameplay and utility code
- `shaders/`: Custom shaders
- `Material/`, `fonts/`: UI theme materials and fonts

## Tests and debug scenes
- `AzurLane/tests/Tests.tscn`: test runner scene for Azur Lane scripts
- `tests/shader.tscn`: shader test scene

## Networking notes
Networking is autoloaded via `res://AzurLane/scripts/ALNetwork.cs` and the main
menu exposes Create/Join flows. The default server IP is defined in code.

## AI match (planned)
See `docs/match_against_ai.md` for the planned “Match against AI” design that
keeps the two-client model by spawning a headless AI client.
