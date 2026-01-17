#!/usr/bin/env sh
set -eu

GODOT_BIN="${GODOT_BIN:-godot}"
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

if [ -f "$PROJECT_ROOT/.env" ]; then
  set -a
  . "$PROJECT_ROOT/.env"
  set +a
fi

if ! command -v "$GODOT_BIN" >/dev/null 2>&1; then
  echo "Error: GODOT_BIN not found: $GODOT_BIN" >&2
  echo "Set GODOT_BIN to your Godot 4.x .NET binary path." >&2
  exit 1
fi

if [ "${SKIP_DOTNET_BUILD:-}" != "1" ]; then
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: dotnet not found. Install the .NET SDK or add it to PATH." >&2
    exit 1
  fi

  GODOT_NUGET_SOURCE="${GODOT_NUGET_SOURCE:-/mnt/g/Apps/GodotLinux/GodotSharp/Tools/nupkgs}"
  DOTNET_CLI_HOME="$PROJECT_ROOT/.dotnet"
  mkdir -p "$DOTNET_CLI_HOME"

  DOTNET_CLI_HOME="$DOTNET_CLI_HOME" \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
  dotnet restore "$PROJECT_ROOT/Cards.csproj" \
    --source "https://api.nuget.org/v3/index.json" \
    ${GODOT_NUGET_SOURCE:+--source "$GODOT_NUGET_SOURCE"}

  DOTNET_CLI_HOME="$DOTNET_CLI_HOME" \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
  dotnet build "$PROJECT_ROOT/Cards.csproj" --no-restore
fi

USER_DATA_DIR="$PROJECT_ROOT/.godot_user"
mkdir -p "$USER_DATA_DIR"

GODOT_LOG_FILE="$PROJECT_ROOT/.godot_test.log"
GODOT_STDOUT_FILE="$PROJECT_ROOT/.godot_test.stdout"

XDG_DATA_HOME="$USER_DATA_DIR" \
"$GODOT_BIN" --headless --path "$PROJECT_ROOT" --scene res://AzurLane/tests/Tests.tscn \
  --log-file "$GODOT_LOG_FILE" > "$GODOT_STDOUT_FILE" 2>&1

exit_code=$?

if [ "${FILTER_EXPECTED_ERRORS:-1}" = "1" ]; then
  awk '
    /ERROR: \[CanBeAttacked\] A non attacker card is trying to start an attack/ {skip=1; next}
    skip {
      if ($0 ~ /^[[:space:]]/) next
      if ($0 ~ /^C# backtrace/) next
      if ($0 ~ /^at:/) next
      skip=0
    }
    !skip {print}
  ' "$GODOT_STDOUT_FILE"

  if [ -f "$GODOT_LOG_FILE" ]; then
    awk '
      /ERROR: \[CanBeAttacked\] A non attacker card is trying to start an attack/ {skip=1; next}
      skip {
        if ($0 ~ /^[[:space:]]/) next
        if ($0 ~ /^C# backtrace/) next
        if ($0 ~ /^at:/) next
        skip=0
      }
      !skip {print}
    ' "$GODOT_LOG_FILE" > "$GODOT_LOG_FILE.tmp" && mv "$GODOT_LOG_FILE.tmp" "$GODOT_LOG_FILE"
  fi
else
  cat "$GODOT_STDOUT_FILE"
fi

exit $exit_code
