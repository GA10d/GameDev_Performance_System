param(
    [string]$UnityEditor='F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe',
    [string]$Blender='F:\Blender\blender.exe',
    [switch]$RegenerateCharacters
)
$ErrorActionPreference='Stop'
if($RegenerateCharacters){
    & $Blender -b --python (Join-Path $PSScriptRoot 'Tools\build_species.py')
    if($LASTEXITCODE -ne 0){throw 'Character generation failed'}
}
$demoProject=Join-Path $PSScriptRoot 'UnityProject'
$buildLog=Join-Path $PSScriptRoot 'QA\unity-build.log'
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'QA') -Force | Out-Null
$arguments='-batchmode -nographics -quit -projectPath "'+$demoProject+'" -executeMethod PerformanceBuild.Build -logFile "'+$buildLog+'"'
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw ('Build failed. See '+$buildLog)}
Write-Output (Join-Path $PSScriptRoot 'Build\ASTRA Performance.exe')
