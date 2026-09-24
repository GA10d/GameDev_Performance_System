param(
    [string]$UnityEditor='F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe',
    [string]$Blender='F:\Blender\blender.exe',
    [switch]$RegenerateCast
)
$ErrorActionPreference='Stop'
if($RegenerateCast){
    & $Blender -b --python (Join-Path $PSScriptRoot 'Tools\build_cast.py')
    if($LASTEXITCODE -ne 0){throw 'Blender cast export failed'}
}
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'QA') -Force | Out-Null
$demoProject=Join-Path $PSScriptRoot 'UnityProject'
$buildLog=Join-Path $PSScriptRoot 'QA\unity-build.log'
$arguments='-batchmode -nographics -quit -projectPath "'+$demoProject+'" -executeMethod ArchiveBuild.Build -logFile "'+$buildLog+'"'
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw ('Build failed: '+$buildLog)}
Write-Output (Join-Path $PSScriptRoot 'Build\ASTRA Relay V2.exe')
