#requires -Version 5.1
<#
.SYNOPSIS
    Build the Aevix Windows installer end-to-end.

.DESCRIPTION
    1. Publishes the WinUI app (Release, self-contained, win-x64) into
       publish\win-x64\.
    2. Runs Inno Setup's ISCC.exe against installer\Aevix.iss to produce
       publish\installer\Aevix-Setup-v<ver>.exe.

.PARAMETER NoPublish
    Skip the dotnet publish step. Useful when you only changed the .iss
    script and just want to recompile the installer.

.EXAMPLE
    pwsh installer\build.ps1
    pwsh installer\build.ps1 -NoPublish
#>
param(
    [switch]$NoPublish
)

$ErrorActionPreference = 'Stop'
$repoRoot   = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'publish\win-x64'
$installerDir = Join-Path $repoRoot 'publish\installer'
$issFile    = Join-Path $repoRoot 'installer\Aevix.iss'

# Locate Inno Setup. Default install path on x64 Windows is in (x86).
$iscCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)
$iscc = $iscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Host "Inno Setup 6 is not installed." -ForegroundColor Yellow
    Write-Host "Download + install from https://jrsoftware.org/isdl.php"
    Write-Host "Then re-run this script."
    exit 1
}

if (-not $NoPublish) {
    Write-Host "==> dotnet publish (Release, win-x64, self-contained)" -ForegroundColor Cyan
    Push-Location $repoRoot
    try {
        if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
        dotnet publish 'src\Aevix.App\Aevix.App.csproj' `
            -c Release `
            -r win-x64 `
            --self-contained `
            -o $publishDir
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "==> Skipping publish (-NoPublish set)" -ForegroundColor Yellow
    if (-not (Test-Path "$publishDir\Aevix.App.exe")) {
        throw "publish\win-x64\Aevix.App.exe missing. Run without -NoPublish first."
    }
}

Write-Host "==> ISCC compiling installer" -ForegroundColor Cyan
& $iscc $issFile
if ($LASTEXITCODE -ne 0) { throw "ISCC failed (exit $LASTEXITCODE)" }

$setupExe = Get-ChildItem $installerDir -Filter 'Aevix-Setup-v*.exe' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($setupExe) {
    $sizeMb = [Math]::Round($setupExe.Length / 1MB, 1)
    Write-Host ""
    Write-Host "Installer built: $($setupExe.FullName)" -ForegroundColor Green
    Write-Host "Size: $sizeMb MB"
} else {
    Write-Warning "ISCC reported success but no Aevix-Setup-*.exe was found in $installerDir."
}
