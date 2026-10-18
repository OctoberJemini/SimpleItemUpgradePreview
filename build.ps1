param(
    [Parameter(Mandatory=$true)][string]$GameManagedDirectory,
    [Parameter(Mandatory=$true)][string]$BepInExCoreDirectory,
    [string]$Compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
)
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$refs = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','assembly_valheim.dll','assembly_utils.dll','assembly_guiutils.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll') | ForEach-Object { '/reference:'+(Join-Path $GameManagedDirectory $_) }
$refs += @('BepInEx.dll','0Harmony.dll') | ForEach-Object { '/reference:'+(Join-Path $BepInExCoreDirectory $_) }
& $Compiler /nologo /noconfig /target:library /optimize+ /nostdlib+ ('/out:'+(Join-Path $outDir 'SimpleItemUpgradePreview.dll')) $refs (Join-Path $PSScriptRoot 'src\Plugin.cs') (Join-Path $PSScriptRoot 'src\ComparisonText.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Get-FileHash (Join-Path $outDir 'SimpleItemUpgradePreview.dll')
