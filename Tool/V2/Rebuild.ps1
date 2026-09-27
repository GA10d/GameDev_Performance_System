param(
    [switch]$Models,
    [string]$EditorPath = 'F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe',
    [string]$BlenderPath = 'F:\Blender\blender.exe'
)
$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$qaDir = Join-Path $toolRoot 'QA'
New-Item -ItemType Directory -Force -Path $qaDir | Out-Null
if (-not (Test-Path -LiteralPath $EditorPath)) { throw "Unity Editor not found: $EditorPath. Pass -EditorPath." }
if ($Models) {
    if (-not (Test-Path -LiteralPath $BlenderPath)) { throw "Blender not found: $BlenderPath. Pass -BlenderPath." }
    $blenderArgs = '-b --python "' + $toolRoot + '\Tools\build_modular.py"'
    $modelProcess = Start-Process -FilePath $BlenderPath -ArgumentList $blenderArgs -WindowStyle Hidden -PassThru
    $modelProcess.WaitForExit()
    if ($modelProcess.ExitCode -ne 0) { throw 'Blender model build failed.' }
}
$logFile = Join-Path $qaDir 'rebuild.log'
$editorArgs = '-batchmode -nographics -quit -projectPath "' + $toolRoot + '\UnityProject" -executeMethod ToolV2Build.BuildPlayer -logFile "' + $logFile + '"'
$editorProcess = Start-Process -FilePath $EditorPath -ArgumentList $editorArgs -WindowStyle Hidden -PassThru
$editorProcess.WaitForExit()
if ($editorProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $logFile -Pattern 'TOOL_BUILD_OK' -Quiet)) { throw "Unity build failed. See $logFile" }
Write-Output 'Built Build/ASTRA Performance Tool V2.exe and Export/ASTRA_Performance_Tool_V2.unitypackage'
