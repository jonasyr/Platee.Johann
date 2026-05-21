$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    Write-Host 'Error: not a git repository.'
    exit 1
}

Set-Location $repoRoot

$hooksPath = Join-Path $repoRoot '.githooks'
if (-not (Test-Path $hooksPath)) {
    New-Item -ItemType Directory -Path $hooksPath | Out-Null
}

git config core.hooksPath '.githooks'
Write-Host 'Git hooks path set to .githooks.'

$toolManifest = Join-Path $repoRoot '.config\dotnet-tools.json'
if (Test-Path $toolManifest) {
    Write-Host 'Run dotnet tool restore once to enable dotnet format.'
}
