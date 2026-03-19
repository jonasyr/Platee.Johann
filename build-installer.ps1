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
#
# Struktur im Deploy-Pfad:
#   <DeployPath>/                          ← Auto-Updater liest hier (SimpleFileSource)
#     RELEASES
#     releases.win.json
#     Platee.Johann-Setup.exe             (immer aktuelle Version)
#     Platee.Johann-win-Portable.zip      (immer aktuelle Version)
#     Platee.Johann-<Version>-full.nupkg  (aktuelle Version)
#     Platee.Johann-<Version>-delta.nupkg (aktuelle Version)
#   <DeployPath>/old_versions/            ← Archiv älterer Pakete (max. 5 Versionen)
#     Platee.Johann-<Version>-full.nupkg
#     Platee.Johann-<Version>-delta.nupkg
#
$OldVersionsPath = Join-Path $DeployPath "old_versions"
$KeepVersions    = 5

Write-Host ""
Write-Host "[4/4] Kopiere Releases nach $DeployPath ..." -ForegroundColor Yellow

if (-not (Test-Path $DeployPath)) {
    Write-Host "      Erstelle Verzeichnis $DeployPath ..." -ForegroundColor DarkYellow
    New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
}
if (-not (Test-Path $OldVersionsPath)) {
    Write-Host "      Erstelle Verzeichnis $OldVersionsPath ..." -ForegroundColor DarkYellow
    New-Item -ItemType Directory -Path $OldVersionsPath -Force | Out-Null
}

# Alte nupkg-Dateien aus Root in old_versions/ verschieben
Get-ChildItem -Path $DeployPath -Filter "*.nupkg" | ForEach-Object {
    $dest = Join-Path $OldVersionsPath $_.Name
    Move-Item -Path $_.FullName -Destination $dest -Force
    Write-Host "      Archiviert: $($_.Name) -> old_versions\" -ForegroundColor DarkGray
}

# In old_versions/ nur die letzten $KeepVersions Versionen behalten
# Versionen aus Dateinamen extrahieren (Platee.Johann-<version>-full/delta.nupkg)
$allOldNupkgs = Get-ChildItem -Path $OldVersionsPath -Filter "*.nupkg"
$oldVersionGroups = $allOldNupkgs |
    Where-Object { $_.Name -match 'Platee\.Johann-(.+?)-(full|delta)\.nupkg' } |
    Group-Object { $Matches[1] } |
    Sort-Object {
        $v = [System.Version]::new(0,0,0,0)
        if ([System.Version]::TryParse($_.Name, [ref]$v)) { $v } else { [System.Version]::new(0,0,0,0) }
    } -Descending

if ($oldVersionGroups.Count -gt $KeepVersions) {
    $oldVersionGroups | Select-Object -Skip $KeepVersions | ForEach-Object {
        $_.Group | ForEach-Object {
            Remove-Item -Path $_.FullName -Force
            Write-Host "      Gelöscht (zu alt): $($_.Name)" -ForegroundColor DarkGray
        }
    }
}

# Aktuelle nupkg-Dateien ins Root kopieren
Get-ChildItem -Path $ReleasesDir -Filter "Platee.Johann-$Version-*.nupkg" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $DeployPath -Force
    Write-Host "      Kopiert: $($_.Name)" -ForegroundColor DarkGray
}

# Metadaten-Dateien ins Root kopieren
foreach ($file in @("RELEASES", "releases.win.json", "assets.win.json")) {
    $src = Join-Path $ReleasesDir $file
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination $DeployPath -Force
        Write-Host "      Kopiert: $file" -ForegroundColor DarkGray
    }
}

# Setup.exe umbenannt ins Root kopieren
$setupSrc = Join-Path $ReleasesDir "Platee.Johann-win-Setup.exe"
if (Test-Path $setupSrc) {
    Copy-Item -Path $setupSrc -Destination (Join-Path $DeployPath "Platee.Johann-Setup.exe") -Force
    Write-Host "      Kopiert: Platee.Johann-win-Setup.exe -> Platee.Johann-Setup.exe" -ForegroundColor DarkGray
}

# Portable.zip ins Root kopieren
$portableSrc = Join-Path $ReleasesDir "Platee.Johann-win-Portable.zip"
if (Test-Path $portableSrc) {
    Copy-Item -Path $portableSrc -Destination $DeployPath -Force
    Write-Host "      Kopiert: Platee.Johann-win-Portable.zip" -ForegroundColor DarkGray
}

Write-Host "Deploy abgeschlossen: $DeployPath" -ForegroundColor Green
Write-Host ""
Write-Host "User-Anleitung:" -ForegroundColor Cyan
Write-Host "  $DeployPath\Platee.Johann-Setup.exe" -ForegroundColor White
Write-Host "  Doppelklick => installiert Johann, Updates automatisch beim Programmstart." -ForegroundColor DarkGray

Write-Host ""
Write-Host "Fertig!" -ForegroundColor Green
