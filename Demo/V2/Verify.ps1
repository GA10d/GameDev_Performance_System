param([int]$Width=1600,[int]$Height=900)
$ErrorActionPreference='Stop'
$qaPath=Join-Path $PSScriptRoot 'QA\runtime'
New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
$exe=Join-Path $PSScriptRoot 'Build\ASTRA Relay V2.exe'
# The visible player is intentional: GPU frame capture requires a drawable window.
& $exe --archive-qa $qaPath -screen-fullscreen 0 -screen-width $Width -screen-height $Height -logFile (Join-Path $qaPath 'player.log') | Out-Null
if($LASTEXITCODE -ne 0){throw 'Runtime verification failed; see QA/runtime'}
