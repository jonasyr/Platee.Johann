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

# Bildschirm bleibt während des Laufs an: nach dem Bildschirm-Timeout sperrt Windows, und auf dem
# Sperrbildschirm lässt sich Johann nicht in den Vordergrund holen — jeder Klick-Test scheitert dann.
Add-Type -Namespace Johann -Name Power -MemberDefinition @'
[DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint esFlags);
'@
$esContinuous = [uint32]'0x80000000'
[void][Johann.Power]::SetThreadExecutionState($esContinuous -bor 0x3)   # SYSTEM_ + DISPLAY_REQUIRED
try {
    $env:JOHANN_UI_KEEP = '1'
    dotnet test @testArgs
    $code = $LASTEXITCODE
}
finally {
    [void][Johann.Power]::SetThreadExecutionState($esContinuous)
}
exit $code
