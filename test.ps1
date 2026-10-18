param([string]$Compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe")
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$exe = Join-Path $outDir 'ComparisonTextTests.exe'
& $Compiler /nologo /target:exe ('/out:'+$exe) (Join-Path $PSScriptRoot 'src\ComparisonText.cs') (Join-Path $PSScriptRoot 'tests\ComparisonTextTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
