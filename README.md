# Cards (Godot 4.4, C#)

Card game playground built in Godot with C# scripts. The main menu boots the
Azur Lane prototype and exposes local match, create game, and join game flows.

## Requirements
- Godot 4.4 .NET (C# build)
- .NET SDK (as required by your Godot install)

## Run
1. Open the project in Godot 4.4 .NET.
2. Press Play. The configured main scene is `res://AzurLane/scenes/main.tscn`.

## Controls (default input map)
- Accept: Enter, Space, E
- Select: Space, W, gamepad button 3
- Cancel: Esc, Q

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
