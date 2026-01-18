#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLIENT_A="${1:-ClientA}"
CLIENT_B="${2:-ClientB}"
LOG_DIR="$ROOT_DIR/logs"
SCRIPT_TIMEOUT="${SCRIPT_TIMEOUT:-}" # When set, how long to keep clients alive before killing them.
DOTNET_BIN="${DOTNET_BIN:-dotnet}"
QUIT_AFTER="${QUIT_AFTER:-}"         # When set, pass --quit-after=<seconds> to clients.

if [[ -f "$ROOT_DIR/.env" ]]; then
  # Load per-repo environment overrides (e.g. GODOT_BIN).
  set -a
  # shellcheck disable=SC1090
  source "$ROOT_DIR/.env"
  set +a
fi

GODOT_BIN="${GODOT_BIN:-godot}"
if ! command -v "$GODOT_BIN" >/dev/null 2>&1; then
  echo "godot binary not found at GODOT_BIN or in PATH."
  echo "Set GODOT_BIN or update PATH."
  exit 1
fi

# Kill existing clients launched with player-name args.
pkill -f -- "--player-name" >/dev/null 2>&1 || true

# Ensure log directory exists.
mkdir -p "$LOG_DIR"

# Build C# solution (guard with timeout to avoid hangs).
if ! command -v "$DOTNET_BIN" >/dev/null 2>&1; then
  echo "dotnet binary not found at DOTNET_BIN or in PATH."
  echo "Set DOTNET_BIN or update PATH."
  exit 1
fi

"$DOTNET_BIN" build "$ROOT_DIR/Cards.sln"

# Run two clients and write logs per instance.
QUIT_ARG=""
if [[ -n "${QUIT_AFTER}" ]]; then
  QUIT_ARG="--quit-after=${QUIT_AFTER}"
fi

RENDERING_DRIVER="${RENDERING_DRIVER:-vulkan}"
RENDERING_ARGS=(--rendering-driver "$RENDERING_DRIVER")

LIBGL_ALWAYS_SOFTWARE=0 "$GODOT_BIN" --path "$ROOT_DIR" "${RENDERING_ARGS[@]}" -- --player-name="$CLIENT_A" $QUIT_ARG >"$LOG_DIR/${CLIENT_A}.log" 2>&1 &
LIBGL_ALWAYS_SOFTWARE=0 "$GODOT_BIN" --path "$ROOT_DIR" "${RENDERING_ARGS[@]}" -- --player-name="$CLIENT_B" $QUIT_ARG >"$LOG_DIR/${CLIENT_B}.log" 2>&1 &

# Give the clients time to boot, then stop them to avoid hanging CI.
if [[ -n "${SCRIPT_TIMEOUT}" ]]; then
  sleep "$SCRIPT_TIMEOUT"
  pkill -f -- "--player-name" >/dev/null 2>&1 || true
fi
