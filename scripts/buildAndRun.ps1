Param(
  [string]$ClientA = "ClientA",
  [string]$ClientB = "ClientB"
)

$ErrorActionPreference = "Stop"

$allTests = $false
$testPath = ""
$headless = $false
$unitTests = $false

function Get-EnvBool([string]$value) {
  if ([string]::IsNullOrWhiteSpace($value)) { return $false }
  switch ($value.Trim().ToLower()) {
    "1" { return $true }
    "true" { return $true }
    "yes" { return $true }
    "y" { return $true }
    default { return $false }
  }
}

function Stop-ClientProcesses($processes) {
  foreach ($process in $processes) {
    if (-not $process) { continue }
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
  }
}

function Get-ProjectName([string]$projectFile) {
  if (-not (Test-Path $projectFile)) { return "Cards" }
  $line = Get-Content $projectFile | Where-Object { $_ -match '^\s*config/name=' } | Select-Object -First 1
  if ($line -match 'config/name="(.+)"') { return $Matches[1] }
  return "Cards"
}

function Get-SafeProfileName([string]$profileName) {
  if ([string]::IsNullOrWhiteSpace($profileName)) { throw "Profile name is required." }
  $safe = $profileName -replace '[^A-Za-z0-9_]', '_'
  if ([string]::IsNullOrWhiteSpace($safe)) { throw "Profile name must include alphanumeric characters." }
  return $safe
}

function Write-MatchDebugSettings([string]$projectName, [string]$profileName, [bool]$autoHost, [bool]$autoJoin) {
  $appData = $env:APPDATA
  if ([string]::IsNullOrWhiteSpace($appData)) { throw "APPDATA is required to write user settings." }
  $userDataDir = Join-Path $appData ("Godot\\app_userdata\\{0}" -f $projectName)
  $saveDir = Join-Path $userDataDir "saves"
  New-Item -ItemType Directory -Force -Path $saveDir | Out-Null
  $safeName = Get-SafeProfileName $profileName
  $settingsPath = Join-Path $saveDir ("match_debug_{0}.json" -f $safeName)
  $settings = [ordered]@{
    IgnoreCosts = $true
    EnableAutoHostMatch = $autoHost
    EnableAutoJoinMatch = $autoJoin
    EnableSelectionSyncTest = $false
    SelectionSyncStepSeconds = 1.0
  }
  $settings | ConvertTo-Json -Depth 3 | Set-Content -Path $settingsPath -Encoding UTF8
}

# Parse script flags from $args to keep params minimal.
for ($i = 0; $i -lt $args.Count; $i++) {
  $arg = $args[$i]
  if ([string]::IsNullOrWhiteSpace($arg)) { continue }
  if ($arg -match '^(--|-)(headless)$') {
    $headless = $true
    continue
  }
  if ($arg -match '^(--|-)(unit)$') {
    $unitTests = $true
    continue
  }
  if ($arg -match '^(--|-)(test)=(.+)$') {
    $testPath = $Matches[3]
    continue
  }
  if ($arg -match '^(--|-)(test)$') {
    if ($i + 1 -ge $args.Count) { throw "Missing value for --test." }
    $testPath = $args[$i + 1]
    $i++
    continue
  }
}

$rootDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $rootDir "logs"
$projectName = Get-ProjectName (Join-Path $rootDir "project.godot")

$envFile = Join-Path $rootDir ".env"
if (Test-Path $envFile) {
  # Simple .env loader: KEY=VALUE with optional comments and blank lines.
  foreach ($rawLine in Get-Content $envFile) {
    $line = $rawLine.Trim()
    if ($line.Length -eq 0) { continue }
    if ($line.StartsWith("#")) { continue }
    $parts = $line.Split("=", 2)
    if ($parts.Count -ne 2) { continue }
    $name = $parts[0].Trim()
    $value = $parts[1].Trim()
    if ($name.Length -eq 0) { continue }
    [Environment]::SetEnvironmentVariable($name, $value)
  }
}

$dotnetBin = if ($env:DOTNET_BIN) { $env:DOTNET_BIN } else { "dotnet" }
$godotBin = if ($env:GODOT_BIN_WINDOWS) { $env:GODOT_BIN_WINDOWS } elseif ($env:GODOT_BIN) { $env:GODOT_BIN } else { "godot" }
$renderingDriver = if ($env:RENDERING_DRIVER) { $env:RENDERING_DRIVER } else { "vulkan" }
$quitAfter = $env:QUIT_AFTER
$scriptTimeout = $env:SCRIPT_TIMEOUT
$waitForExit = Get-EnvBool $env:WAIT_FOR_EXIT
$runTests = ($testPath -ne "")
if ($unitTests -and $testPath) {
  throw "Use --test for gameplay tests and --unit for Tests.tscn."
}
if ($unitTests -and -not $runTests) {
  $runTests = $true
}

if (-not (Get-Command $godotBin -ErrorAction SilentlyContinue)) {
  throw "godot binary not found at GODOT_BIN_WINDOWS/GODOT_BIN or in PATH."
}

if (-not (Get-Command $dotnetBin -ErrorAction SilentlyContinue)) {
  throw "dotnet binary not found at DOTNET_BIN or in PATH."
}

$godotPath = (Get-Command $godotBin).Source

# Kill existing clients launched by this script (same Godot binary + player-name).
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -match "--player-name" -and $_.ExecutablePath -eq $godotPath } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

& $dotnetBin build (Join-Path $rootDir "Cards.sln")
if ($LASTEXITCODE -ne 0) {
  throw "dotnet build failed with exit code $LASTEXITCODE."
}

$quitArg = if ($quitAfter) { "--quit-after=$quitAfter" } else { "" }
$renderArgs = @("--rendering-driver", $renderingDriver)

$commonArgs = @("--path", $rootDir) + $renderArgs + @("--")
if ($headless) { $commonArgs = @("--headless") + $commonArgs }

if ($runTests -and $unitTests) {
  $testArgs = @("--path", $rootDir) + $renderArgs
  if ($headless) { $testArgs = @("--headless") + $testArgs }
  $testArgs += @("--scene", "res://AzurLane/tests/Tests.tscn", "--")
  if ($testPath) { $testArgs += "--test=$testPath" }
  & $godotBin @testArgs
  exit $LASTEXITCODE
}

$clientALog = Join-Path $logDir "$ClientA.log"
$clientAErr = Join-Path $logDir "$ClientA.error.log"
$clientBLog = Join-Path $logDir "$ClientB.log"
$clientBErr = Join-Path $logDir "$ClientB.error.log"

if ($testPath -and -not $unitTests) {
  Write-MatchDebugSettings $projectName $ClientA $true $false
  Write-MatchDebugSettings $projectName $ClientB $false $true
}

$clientAArgs = $commonArgs + @("--player-name=$ClientA")
if ($testPath) { $clientAArgs += @("--test=$testPath") }
if ($quitArg) { $clientAArgs += $quitArg }

$clientBArgs = $commonArgs + @("--player-name=$ClientB")
if ($testPath) { $clientBArgs += @("--test=$testPath") }
if ($quitArg) { $clientBArgs += $quitArg }

$clientAProcess = Start-Process -FilePath $godotBin -ArgumentList $clientAArgs -WorkingDirectory $rootDir -PassThru `
  -RedirectStandardOutput $clientALog -RedirectStandardError $clientAErr

$clientBProcess = Start-Process -FilePath $godotBin -ArgumentList $clientBArgs -WorkingDirectory $rootDir -PassThru `
  -RedirectStandardOutput $clientBLog -RedirectStandardError $clientBErr

if ($scriptTimeout) {
  $timeoutSeconds = 0
  $parsedTimeout = [int]::TryParse($scriptTimeout, [ref]$timeoutSeconds)
  if (-not $parsedTimeout -or $timeoutSeconds -le 0) {
    throw "SCRIPT_TIMEOUT must be a positive integer in seconds."
  }
  $elapsedSeconds = 0
  $clientProcesses = @($clientAProcess, $clientBProcess)
  while ($elapsedSeconds -lt $timeoutSeconds) {
    $clientAErrExists = Test-Path $clientAErr
    $clientBErrExists = Test-Path $clientBErr
    $clientAErrHasContent = $clientAErrExists -and (Get-Item $clientAErr).Length -gt 0
    $clientBErrHasContent = $clientBErrExists -and (Get-Item $clientBErr).Length -gt 0
    if ($clientAErrHasContent -or $clientBErrHasContent) {
      Stop-ClientProcesses $clientProcesses
      throw "Client error detected. Check logs for details."
    }
    Start-Sleep -Seconds 1
    $elapsedSeconds++
  }
  Stop-ClientProcesses $clientProcesses
} elseif ($waitForExit) {
  while ($true) {
    $runningClients = @($clientAProcess, $clientBProcess) | Where-Object { -not $_.HasExited }
    if (-not $runningClients) {
      break
    }
    $clientAErrExists = Test-Path $clientAErr
    $clientBErrExists = Test-Path $clientBErr
    $clientAErrHasContent = $clientAErrExists -and (Get-Item $clientAErr).Length -gt 0
    $clientBErrHasContent = $clientBErrExists -and (Get-Item $clientBErr).Length -gt 0
    if ($clientAErrHasContent -or $clientBErrHasContent) {
      Stop-ClientProcesses $runningClients
      throw "Client error detected. Check logs for details."
    }
    Start-Sleep -Seconds 1
  }
}
