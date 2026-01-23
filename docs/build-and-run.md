# Build And Run Scripts

This project includes helper scripts to build the C# solution and launch two Godot clients with distinct player names.

## scripts/buildAndRun.ps1 (Windows / PowerShell)

Usage:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/buildAndRun.ps1
powershell -ExecutionPolicy Bypass -File scripts/buildAndRun.ps1 -ClientA "Alice" -ClientB "Bob"
powershell -ExecutionPolicy Bypass -File scripts/buildAndRun.ps1 --unit
powershell -ExecutionPolicy Bypass -File scripts/buildAndRun.ps1 --test=res://AzurLane/tests/Test_ALSelectionSync.cs
```

Behavior:
- Loads `.env` from the repo root (if present) and exports values into the process environment.
- Validates `GODOT_BIN_WINDOWS`/`GODOT_BIN` and `DOTNET_BIN` (or falls back to `godot`/`dotnet` in PATH).
- Builds `Cards.sln`.
- Launches two clients with `--player-name` set to `ClientA` and `ClientB`.
- `--unit` runs the unit test scene (`res://AzurLane/tests/Tests.tscn`) and executes all tests in that scene.
- `--test=path` runs a gameplay test by path:
  - Launches two clients.
  - Auto-hosts on `ClientA` and auto-joins on `ClientB`.
  - Runs the test once the match reaches main phase.
  - The path should be the script file for the test (for example, `res://AzurLane/tests/Test_ALSelectionSync.cs`).
- Optionally quits after a duration using `QUIT_AFTER` (passed to Godot as `--quit-after=<seconds>`).
- Optionally enforces a hard stop with `SCRIPT_TIMEOUT` (seconds).

Important logs:
- Writes stdout and stderr separately per client:
  - `logs/<ClientA>.log`
  - `logs/<ClientA>.error.log`
  - `logs/<ClientB>.log`
  - `logs/<ClientB>.error.log`

Environment variables:
- `GODOT_BIN_WINDOWS`: Absolute path to Godot .exe on Windows.
- `GODOT_BIN`: Fallback Godot path if `GODOT_BIN_WINDOWS` is unset.
- `DOTNET_BIN`: Dotnet CLI path (defaults to `dotnet`).
- `RENDERING_DRIVER`: Godot rendering driver (defaults to `vulkan`).
- `QUIT_AFTER`: If set, adds `--quit-after=<seconds>` to Godot.
- `SCRIPT_TIMEOUT`: If set, kills client processes after this many seconds.

## scripts/buildAndRun.sh (Linux / WSL / macOS)

Usage:
```bash
./scripts/buildAndRun.sh
./scripts/buildAndRun.sh Alice Bob
```

Behavior:
- Loads `.env` from the repo root (if present) and exports values into the shell.
- Validates `GODOT_BIN` and `DOTNET_BIN` (or falls back to `godot`/`dotnet` in PATH).
- Builds `Cards.sln`.
- Launches two clients with `--player-name` set to `ClientA` and `ClientB`.
- Optionally quits after a duration using `QUIT_AFTER` (passed to Godot as `--quit-after=<seconds>`).
- Optionally enforces a hard stop with `SCRIPT_TIMEOUT` (seconds).

Logs:
- Writes combined stdout/stderr per client to:
  - `logs/<ClientA>.log`
  - `logs/<ClientB>.log`

Environment variables:
- `GODOT_BIN`: Absolute path to Godot binary.
- `DOTNET_BIN`: Dotnet CLI path (defaults to `dotnet`).
- `RENDERING_DRIVER`: Godot rendering driver (defaults to `vulkan`).
- `QUIT_AFTER`: If set, adds `--quit-after=<seconds>` to Godot.
- `SCRIPT_TIMEOUT`: If set, kills client processes after this many seconds.
