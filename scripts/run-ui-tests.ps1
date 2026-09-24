# scripts/run-ui-tests.ps1 — Phase-B-Suite gegen die echte EXE (#111)
#
# Belegt den echten Desktop (Fensterfokus, Screenshots) — nicht laufen lassen, während jemand
# an der Maschine arbeitet, und niemals aus dem normalen `dotnet test` / Pre-Push-Hook heraus.
param([string]$Filter)
$ErrorActionPreference = 'Stop'

if (Get-Process -Name 'Platee.Johann.UI' -ErrorAction SilentlyContinue) {
    throw 'Johann läuft – bitte schließen.'
}

dotnet build (Join-Path $PSScriptRoot '..\Platee.Johann.slnx')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testArgs = @(
    (Join-Path $PSScriptRoot '..\Platee.Johann.UiTests'),
    '--no-build',
    '-p:RunUiTests=true',
    '--blame-hang-timeout', '3m',
    '--results-directory', 'TestResults'
)
if ($Filter) { $testArgs += @('--filter', $Filter) }

$env:JOHANN_UI_KEEP = '1'
dotnet test @testArgs
exit $LASTEXITCODE
