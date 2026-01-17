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
/mnt/g/dev/godot/cards/
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
/mnt/g/dev/godot/cards/AzurLane/
├── scenes/
├── scripts/
├── database/
├── assets/
└── tests/
```

### Shared Content
```
/mnt/g/dev/godot/cards/scenes/
/mnt/g/dev/godot/cards/scripts/
/mnt/g/dev/godot/cards/shaders/
```

## Notes
- Godot main scene: `res://AzurLane/scenes/main.tscn`
- Test scenes: `res://AzurLane/tests/Tests.tscn`, `res://tests/shader.tscn`
- Default networking port: 7000 (see `docs/network_api.md`)

## Important Rules
- Do not commit automatically; always ask before using git.
- Avoid destructive git operations without explicit instruction.
