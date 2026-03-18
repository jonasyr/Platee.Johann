# ============================================================
#  Platé.Johann – Velopack Installer bauen & auf GitHub laden
# ============================================================
#
# Voraussetzungen (einmalig):
#   dotnet tool install -g vpk
#
# Verwendung:
#   .\build-installer.ps1 -Version 1.0.0
#   .\build-installer.ps1 -Version 1.1.0 -GithubToken $env:GITHUB_TOKEN
#
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$GithubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"

$RepoRoot   = $PSScriptRoot
$UiProject  = "$RepoRoot\Platee.Johann.UI\Platee.Johann.UI.csproj"
$PublishDir = "$RepoRoot\Platee.Johann.UI\bin\Publish\Velopack"
$AppDir     = "$PublishDir\app"
$ReleasesDir= "$PublishDir\releases"
$ExeName    = "Platee.Johann.UI.exe"
$IconPath   = "$RepoRoot\Johann.ico"

Write-Host ""
Write-Host "=== Platé.Johann v$Version – Installer-Build ===" -ForegroundColor Cyan
Write-Host ""

# 1. Publish (self-contained, kein SingleFile – Velopack braucht einzelne DLLs)
Write-Host "[1/3] dotnet publish..." -ForegroundColor Yellow
dotnet publish $UiProject `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o $AppDir `
    /p:Version=$Version `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) { throw "dotnet publish fehlgeschlagen." }

# 2. Velopack-Paket erstellen
Write-Host ""
Write-Host "[2/3] vpk pack..." -ForegroundColor Yellow
vpk pack `
    --packId "JohannCS" `
    --packVersion $Version `
    --packDir $AppDir `
    --mainExe $ExeName `
    --outputDir $ReleasesDir `
    --icon $IconPath

if ($LASTEXITCODE -ne 0) { throw "vpk pack fehlgeschlagen." }

Write-Host ""
Write-Host "Installer erstellt in: $ReleasesDir" -ForegroundColor Green

# 3. Optional: auf GitHub Releases hochladen
Write-Host ""
if ($GithubToken) {
    Write-Host "[3/3] Lade auf GitHub hoch (Tag: v$Version)..." -ForegroundColor Yellow
    vpk upload github `
        --repoUrl "https://github.com/jonasyr/JohannCS" `
        --token $GithubToken `
        --tag "v$Version" `
        --outputDir $ReleasesDir

    if ($LASTEXITCODE -ne 0) { throw "GitHub-Upload fehlgeschlagen." }
    Write-Host "Upload abgeschlossen!" -ForegroundColor Green
} else {
    Write-Host "[3/3] Kein GitHub-Token – Upload übersprungen." -ForegroundColor DarkYellow
    Write-Host "      Token übergeben: .\build-installer.ps1 -Version $Version -GithubToken <token>" -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "Fertig!" -ForegroundColor Green
