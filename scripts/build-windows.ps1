param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$solutionPath = Join-Path $PSScriptRoot "..\AiBatchRenamer.sln"

if (-not (Get-Command msbuild -ErrorAction SilentlyContinue)) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($msbuildPath) {
            & $msbuildPath $solutionPath /p:Configuration=$Configuration
            exit $LASTEXITCODE
        }
    }

    throw "MSBuild was not found. Install Visual Studio 2022 with .NET Framework 4.8 Developer Pack."
}

msbuild $solutionPath /p:Configuration=$Configuration
