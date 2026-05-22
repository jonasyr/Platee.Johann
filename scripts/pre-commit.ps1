$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    exit 0
}

Set-Location $repoRoot

$stagedFiles = git diff --cached --name-only --diff-filter=ACMR
if (-not $stagedFiles) {
    exit 0
}

$stagedFiles = $stagedFiles | Where-Object { $_ -and (Test-Path $_) }
if (-not $stagedFiles) {
    exit 0
}

$normalizedPaths = $stagedFiles | ForEach-Object { $_ -replace '\\', '/' }
$buildOutputs = $normalizedPaths | Where-Object { $_ -match '(^|/)(bin|obj)/' }
if ($buildOutputs) {
    Write-Host 'Error: staged files include build outputs (bin/ or obj/).'
    $buildOutputs | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$checkExtensions = @(
    '.cs', '.xaml', '.csproj', '.props', '.targets', '.json', '.xml',
    '.yml', '.yaml', '.ps1', '.psm1', '.psd1', '.sln', '.slnx'
)

$conflictFiles = @()
$whitespaceFiles = @()
$newlineFiles = @()

foreach ($file in $stagedFiles) {
    $extension = [IO.Path]::GetExtension($file).ToLowerInvariant()
    if (-not ($checkExtensions -contains $extension)) {
        continue
    }

    $content = Get-Content -LiteralPath $file -Raw -ErrorAction SilentlyContinue
    if (-not $content) {
        continue
    }

    if ($content -match '(?m)^(<<<<<<<|=======|>>>>>>>)') {
        $conflictFiles += $file
        continue
    }

    $lines = $content -split "`r?`n"
    foreach ($line in $lines) {
        if ($line -match '[ \t]+$') {
            $whitespaceFiles += $file
            break
        }
    }

    if ($content.Length -gt 0 -and -not $content.EndsWith("`n")) {
        $newlineFiles += $file
    }
}

if ($conflictFiles) {
    Write-Host 'Error: merge conflict markers found in staged files.'
    $conflictFiles | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    exit 1
}

if ($whitespaceFiles) {
    Write-Host 'Error: trailing whitespace found in staged files.'
    $whitespaceFiles | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    exit 1
}

if ($newlineFiles) {
    Write-Host 'Error: missing final newline in staged files.'
    $newlineFiles | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$csFiles = $stagedFiles | Where-Object { $_.ToLowerInvariant().EndsWith('.cs') }
if ($csFiles) {
    $toolManifest = Join-Path $repoRoot '.config\dotnet-tools.json'
    if (-not (Test-Path $toolManifest)) {
        Write-Host 'Error: dotnet format tool manifest not found at .config/dotnet-tools.json.'
        Write-Host 'Run: dotnet tool restore'
        exit 1
    }

    $projectMap = @{
        'Platee.Johann.Domain' = 'Platee.Johann.Domain/Platee.Johann.Domain.csproj'
        'Platee.Johann.Application' = 'Platee.Johann.Application/Platee.Johann.Application.csproj'
        'Platee.Johann.Infrastructure' = 'Platee.Johann.Infrastructure/Platee.Johann.Infrastructure.csproj'
        'Platee.Johann.UI' = 'Platee.Johann.UI/Platee.Johann.UI.csproj'
        'Platee.Johann.Tests' = 'Platee.Johann.Tests/Platee.Johann.Tests.csproj'
    }

    $filesByProject = @{}
    foreach ($file in $csFiles) {
        $normalized = $file -replace '\\', '/'
        $projectKey = $projectMap.Keys | Where-Object { $normalized.StartsWith("$_/") } | Select-Object -First 1
        if (-not $projectKey) {
            Write-Host "Warning: skipping dotnet format for $file (no project mapping)."
            continue
        }

        if (-not $filesByProject.ContainsKey($projectKey)) {
            $filesByProject[$projectKey] = @()
        }

        $filesByProject[$projectKey] += $file
    }

    foreach ($projectKey in $filesByProject.Keys) {
        $projectPath = $projectMap[$projectKey]
        $projectFiles = $filesByProject[$projectKey]

        Write-Host "Running dotnet format for $projectKey..."
        dotnet tool run dotnet-format $projectPath --include $projectFiles --no-restore
        if ($LASTEXITCODE -ne 0) {
            Write-Host 'Error: dotnet format failed. If packages are missing, run: dotnet restore'
            exit $LASTEXITCODE
        }
    }

    git add -- $csFiles
}
