# Project Context

This file contains project-specific context and information for AI assistants
working with the Cards Godot project.

## Project Overview
- Project Name: Cards (Godot)
- Date: Saturday, January 17, 2026
- Operating System: Linux
- Project Directory: /mnt/g/dev/godot/cards

## Project Summary
- Godot 4.4 .NET (C#) card game playground with an Azur Lane prototype
- Main menu exposes local match, create game, and join game flows
- Networking autoloaded via `res://AzurLane/scripts/ALNetwork.cs`

## Complete Project Structure

### Root Directory
```

├── AGENTS.md
├── Cards.sln
├── Cards.csproj
├── Cards.csproj.old
├── README.md
├── project.godot
├── AzurLane/
├── Vanguard/
├── scenes/
├── scripts/
├── shaders/
├── docs/
├── tests/
├── Material/
├── fonts/
├── back.jpg
├── card.jpg
├── icon.svg
└── playmat.png
```

### AzurLane Prototype
```
AzurLane/
├── scenes/
├── scripts/
├── database/
├── assets/
└── tests/
```

## Notes
- Godot main scene: `res://AzurLane/scenes/main.tscn`
- Test scenes: `res://AzurLane/tests/Tests.tscn`, `res://tests/shader.tscn`
- Default networking port: 7000 (see `docs/network_api.md`)
- Check the docs for more information (/docs)

## Testing (Local Build + Run)
Use the build/run helpers to compile and launch two clients with player names.

- Windows (PowerShell): `powershell -ExecutionPolicy Bypass -File scripts/buildAndRun.ps1`
- Linux/WSL/macOS (Bash): `./scripts/buildAndRun.sh`

Both scripts load `.env` from the repo root and support:
- `GODOT_BIN_WINDOWS` (Windows) or `GODOT_BIN` (Linux/WSL/macOS) for the Godot binary.
- `DOTNET_BIN` for the dotnet CLI.
- `RENDERING_DRIVER` to pick a Godot rendering backend.
- `QUIT_AFTER` to pass `--quit-after=<seconds>` to Godot.
- `SCRIPT_TIMEOUT` to stop clients after a given number of seconds.

Logs are critical for validation:
- Windows script writes stdout/stderr separately to `logs/<Client>.log` and `logs/<Client>.error.log`.
- Bash script writes combined output to `logs/<Client>.log`.

## Two-Client Testing And Auto Start
- Launch two clients with the build/run scripts;
- In each client, use the main menu flows:
  - Start: local match (single client).
  - Create Game: host a match.
  - Join Game: join a host by address/port.
- To auto-start a match with two clients, open the debug panel and enable only one of:
  - "Enable auto host match" on the host client.
  - "Enable auto join match" on the join client.
  The host will create a lobby, wait for 2 players, then start the match automatically.

## User Operations (Default Inputs)
- Accept: Enter, Space, E
- Select: Space, W, gamepad button 3
- Cancel: Esc, Q

## Important Rules
- Do not commit automatically; always ask before using git.
- Avoid destructive git operations without explicit instruction.
