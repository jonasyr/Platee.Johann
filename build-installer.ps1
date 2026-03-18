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

    [string]$GithubToken = $env:GITHUB_TOKEN,

    # Netzwerkpfad, in den Setup + Releases kopiert werden.
    # Kann per Parameter überschrieben werden, Standard ist Z:\12_Tools\Peano\Johann
    [string]$DeployPath = "Z:\12_Tools\Peano\Johann"
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
Write-Host "=== Plate.Johann v$Version - Installer-Build ===" -ForegroundColor Cyan
Write-Host ""

# 1. Publish (self-contained, kein SingleFile - Velopack braucht einzelne DLLs)
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
    --packId "Platee.Johann" `
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
        --repoUrl "https://github.com/jonasyr/Platee.Johann" `
        --token $GithubToken `
        --tag "v$Version" `
        --outputDir $ReleasesDir

    if ($LASTEXITCODE -ne 0) { throw "GitHub-Upload fehlgeschlagen." }
    Write-Host "Upload abgeschlossen!" -ForegroundColor Green
} else {
    Write-Host "[3/3] Kein GitHub-Token - Upload uebersprungen." -ForegroundColor DarkYellow
    Write-Host "      Token setzen: [System.Environment]::SetEnvironmentVariable('GITHUB_TOKEN','...','User')" -ForegroundColor DarkYellow
}

# 4. Releases in den Deploy-Pfad kopieren (Netzlaufwerk / lokaler Freigabepfad)
Write-Host ""
Write-Host "[4/4] Kopiere Releases nach $DeployPath ..." -ForegroundColor Yellow

if (-not (Test-Path $DeployPath)) {
    Write-Host "      Erstelle Verzeichnis $DeployPath ..." -ForegroundColor DarkYellow
    New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
}

# Alle Release-Dateien kopieren (Setup.exe, nupkg, json, RELEASES)
Get-ChildItem -Path $ReleasesDir | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $DeployPath -Force
    Write-Host "      Kopiert: $($_.Name)" -ForegroundColor DarkGray
}

Write-Host "Deploy abgeschlossen: $DeployPath" -ForegroundColor Green
Write-Host ""
Write-Host "User-Anleitung:" -ForegroundColor Cyan
Write-Host "  $DeployPath\Platee.Johann-win-Setup.exe" -ForegroundColor White
Write-Host "  Doppelklick => installiert JohannCS, Updates automatisch beim Programmstart." -ForegroundColor DarkGray

Write-Host ""
Write-Host "Fertig!" -ForegroundColor Green
