Param(
  [string]$ClientA = "ClientA",
  [string]$ClientB = "ClientB"
)

$ErrorActionPreference = "Stop"

$rootDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $rootDir "logs"

$envFile = Join-Path $rootDir ".env"
if (Test-Path $envFile) {
  Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0) { return }
    if ($line.StartsWith("#")) { return }
    $parts = $line.Split("=", 2)
    if ($parts.Count -ne 2) { return }
    $name = $parts[0].Trim()
    $value = $parts[1].Trim()
    if ($name.Length -eq 0) { return }
    [Environment]::SetEnvironmentVariable($name, $value)
  }
}

$dotnetBin = if ($env:DOTNET_BIN) { $env:DOTNET_BIN } else { "dotnet" }
$godotBin = if ($env:GODOT_BIN_WINDOWS) { $env:GODOT_BIN_WINDOWS } elseif ($env:GODOT_BIN) { $env:GODOT_BIN } else { "godot" }
$renderingDriver = if ($env:RENDERING_DRIVER) { $env:RENDERING_DRIVER } else { "vulkan" }
$quitAfter = $env:QUIT_AFTER
$scriptTimeout = $env:SCRIPT_TIMEOUT

if (-not (Get-Command $godotBin -ErrorAction SilentlyContinue)) {
  throw "godot binary not found at GODOT_BIN_WINDOWS/GODOT_BIN or in PATH."
}

if (-not (Get-Command $dotnetBin -ErrorAction SilentlyContinue)) {
  throw "dotnet binary not found at DOTNET_BIN or in PATH."
}

# Kill existing clients launched with player-name args.
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -match "--player-name" } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

& $dotnetBin build (Join-Path $rootDir "Cards.sln")

$quitArg = if ($quitAfter) { "--quit-after=$quitAfter" } else { "" }
$renderArgs = @("--rendering-driver", $renderingDriver)

$commonArgs = @("--path", $rootDir) + $renderArgs + @("--", "--player-name")

$clientALog = Join-Path $logDir "$ClientA.log"
$clientAErr = Join-Path $logDir "$ClientA.error.log"
$clientBLog = Join-Path $logDir "$ClientB.log"
$clientBErr = Join-Path $logDir "$ClientB.error.log"

$clientAArgs = $commonArgs + @("$ClientA")
if ($quitArg) { $clientAArgs += $quitArg }

$clientBArgs = $commonArgs + @("$ClientB")
if ($quitArg) { $clientBArgs += $quitArg }

Start-Process -FilePath $godotBin -ArgumentList $clientAArgs -WorkingDirectory $rootDir `
  -RedirectStandardOutput $clientALog -RedirectStandardError $clientAErr

Start-Process -FilePath $godotBin -ArgumentList $clientBArgs -WorkingDirectory $rootDir `
  -RedirectStandardOutput $clientBLog -RedirectStandardError $clientBErr

if ($scriptTimeout) {
  $timeoutSeconds = 0
  $parsedTimeout = [int]::TryParse($scriptTimeout, [ref]$timeoutSeconds)
  if (-not $parsedTimeout -or $timeoutSeconds -le 0) {
    throw "SCRIPT_TIMEOUT must be a positive integer in seconds."
  }
  Start-Sleep -Seconds $timeoutSeconds
  Get-CimInstance Win32_Process |
    Where-Object { $_.CommandLine -match "--player-name" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
