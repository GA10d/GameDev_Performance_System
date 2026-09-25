param([int]$Width=1440,[int]$Height=900)
$ErrorActionPreference='Stop'
$qaPath=Join-Path $PSScriptRoot 'QA\species'
New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
$exe=Join-Path $PSScriptRoot 'Build\ASTRA Performance.exe'
& $exe -screen-fullscreen 0 -screen-width $Width -screen-height $Height --species-qa $qaPath -logFile (Join-Path $qaPath 'player.log') | Out-Null
if($LASTEXITCODE -ne 0){throw 'Species verification failed; see QA/species/checks.txt'}
Get-Content -LiteralPath (Join-Path $qaPath 'checks.txt') -Encoding UTF8
