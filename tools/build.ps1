$ErrorActionPreference = 'Stop'
# Builds ShitTalker26.exe in the app folder (the folder that contains tools\, src\, app\ and phrases.json)
$root = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $root 'src\Program.cs'
$output = Join-Path $root 'ShitTalker26.exe'
if (-not (Test-Path $source)) { throw "Source not found: $source" }
if (Get-Process ShitTalker26 -ErrorAction SilentlyContinue) { throw 'Close ShitTalker 26 before building.' }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll ('/out:' + $output) $source
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Host "Built $output"
