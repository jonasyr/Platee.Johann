$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    exit 0
}

Set-Location $repoRoot

Write-Host 'Running build (no restore)...'
dotnet build Platee.Johann.UI/Platee.Johann.UI.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Error: build failed. If packages are missing, run: dotnet restore'
    exit $LASTEXITCODE
}

Write-Host 'Running tests (no restore)...'
dotnet test Platee.Johann.Tests/Platee.Johann.Tests.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Error: tests failed. If packages are missing, run: dotnet restore'
    exit $LASTEXITCODE
}
