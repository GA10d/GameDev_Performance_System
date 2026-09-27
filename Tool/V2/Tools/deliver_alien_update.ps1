$ErrorActionPreference = 'Stop'
$sourceRoot = 'F:\Documents\ChatGPT\Project_ Astra\PerformanceToolV2'
$targetRoot = 'F:\Documents\GitHub\GameDev_Performance_System\Tool\V2'
if (!(Test-Path -LiteralPath "$targetRoot\UnityProject\Assets\AstraToolV2")) { throw 'Expected V2 target project missing' }
$assetRoot = 'UnityProject\Assets\AstraToolV2'
$ubc = 'Source\Downloads\UniversalBaseCharacters\Universal Base Characters[Standard]'
$files = @(
 'README.md','THIRD_PARTY_NOTICES.md',
 'Tools\build_modular.py','Tools\import_ubc_hair.py','Tools\alien_design.py','Tools\alien_sheets.py','Tools\deliver_alien_update.ps1',
 'Source\Quaternius_Astra_Modular.blend','Source\modular_manifest.json','Source\ubc_hair_manifest.json','Source\alien_design_manifest.json',
 'Source\Downloads\UniversalBaseCharacters_Standard.zip',"$ubc\License_Standard.txt",
 'Docs\技术架构.md','Docs\捏人与演出设计.md','Docs\验收记录.md','Docs\外星人重设计与原包发型.md',
 'QA\hair-scalp-verification.json','QA\tool-v2-verification.txt','QA\build-summary.txt','QA\alien-build.log','QA\alien-player.log','QA\alien-blender.log',
 "$assetRoot\Models\ModularCast.fbx",
 "$assetRoot\Runtime\ToolCreatorOptions.cs","$assetRoot\Runtime\ToolCharacterView.cs","$assetRoot\Runtime\ToolWorkbench.cs",
 "$assetRoot\Runtime\ToolPortraitFraming.cs","$assetRoot\Runtime\ToolStage.cs","$assetRoot\Runtime\ToolAlienRuntimeQA.cs",
 "$assetRoot\Editor\ToolV2CharacterCreatorWindow.cs","$assetRoot\Editor\ToolV2CreatorBuild.cs","$assetRoot\Editor\ToolV2QA.cs"
)
foreach ($relative in $files) {
 $source = Join-Path $sourceRoot $relative; $destination = Join-Path $targetRoot $relative
 if (!(Test-Path -LiteralPath $source)) { throw "Missing source: $relative" }
 [System.IO.Directory]::CreateDirectory((Split-Path $destination)) | Out-Null
 Copy-Item -LiteralPath $source -Destination $destination -Force
 if ($relative.StartsWith('UnityProject\') -and (Test-Path -LiteralPath ($source+'.meta'))) {
  Copy-Item -LiteralPath ($source+'.meta') -Destination ($destination+'.meta') -Force
 }
 if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw "Hash mismatch: $relative" }
}
foreach ($relative in @('Build','Export',"$assetRoot\Characters\Prefabs",'QA\AlienSheets',"$ubc\Hairstyles\Rigged to Head Bone\FBX (Unity)")) {
 & robocopy (Join-Path $sourceRoot $relative) (Join-Path $targetRoot $relative) /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NP /NFL /NDL /NJH /NJS
 if ($LASTEXITCODE -ge 8) { throw "Copy failed: $relative" }
}
# Hidden-window screen capture is black on this host. Deliver actual camera images only.
& robocopy (Join-Path $sourceRoot 'QA\AlienFinal') (Join-Path $targetRoot 'QA\AlienFinal') /E /XF 'ui_*.png' /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NP /NFL /NDL /NJH /NJS
if ($LASTEXITCODE -ge 8) { throw 'Copy failed: AlienFinal' }
foreach ($relative in @('Build\ASTRA Performance Tool V2.exe','Build\ASTRA Performance Tool V2_Data\Managed\Assembly-CSharp.dll','Export\ASTRA_Performance_Tool_V2.unitypackage')) {
 if ((Get-FileHash -LiteralPath (Join-Path $sourceRoot $relative)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $targetRoot $relative)).Hash) { throw "Binary mismatch: $relative" }
}
Write-Output 'ALIEN_UPDATE_DELIVERED; source, model, program and package checksums match.'
exit 0
