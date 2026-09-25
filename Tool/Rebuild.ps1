param(
    [string]$UnityPath = 'F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe',
    [switch]$CreateSample,
    [switch]$Player
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$project=Join-Path $root 'UnityProject'
$qa=Join-Path $root 'QA'
New-Item -ItemType Directory -Path $qa -Force | Out-Null
if(-not (Test-Path -LiteralPath $UnityPath)){throw "Unity Editor not found: $UnityPath"}
$methods=@()
if($CreateSample){$methods+='ToolBuild.CreateSample'}
$methods+=if($Player){'ToolBuild.BuildPlayer'}else{'ToolBuild.VerifyAndExport'}
foreach($method in $methods){
    $log=Join-Path $qa (($method -replace '\.','-')+'.log')
    $arguments='-batchmode -nographics -quit -projectPath "'+$project+'" -executeMethod '+$method+' -logFile "'+$log+'"'
    $process=Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 80;throw "$method failed with exit code $($process.ExitCode)"}
    Write-Output "$method completed. Log: $log"
}
