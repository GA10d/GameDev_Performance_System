param([switch]$Matrix)
$ErrorActionPreference='Stop'
$folder=if($Matrix){'matrix'}else{'final'}
$reviewPath=Join-Path $PSScriptRoot ('QA\visual-review\'+$folder)
New-Item -ItemType Directory -Path $reviewPath -Force | Out-Null
$exe=Join-Path $PSScriptRoot 'Build\ASTRA Relay V2.exe'
$auditMode=if($Matrix){'--audit-matrix'}else{'--audit-gallery'}
# Keep a drawable player window; a hidden window can produce black screenshots.
& $exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900 --visual-audit $reviewPath $auditMode -logFile (Join-Path $reviewPath 'player.log') | Out-Null
if($LASTEXITCODE -ne 0){throw 'Visual capture failed; inspect player.log'}
Write-Output (Join-Path $reviewPath 'complete.txt')
