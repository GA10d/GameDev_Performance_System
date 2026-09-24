$ErrorActionPreference='Stop'
$qaPath=Join-Path $PSScriptRoot 'QA\runtime'
New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
$exe=Join-Path $PSScriptRoot 'Build\ASTRA Performance.exe'
$arguments='-screen-fullscreen 0 -screen-width 1440 -screen-height 900 --performance-qa "'+$qaPath+'" -logFile "'+(Join-Path $qaPath 'player.log')+'"'
# A rendered interactive player must be visible: minimized Unity windows may skip rendering.
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Normal -PassThru
if(-not $process.WaitForExit(90000)){Stop-Process -Id $process.Id;throw 'QA timed out'}
if($process.ExitCode -ne 0){throw ('QA failed: '+$qaPath)}
Get-Content -LiteralPath (Join-Path $qaPath 'runtime-verification.txt') -Encoding UTF8
