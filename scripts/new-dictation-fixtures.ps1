# Erzeugt die Fixture-Diktate D1..D6 als MP3 per OpenAI-TTS (#111).
# Die Texte liegen in tests/fixtures/dictations/D*.txt; die MP3s landen daneben.
# Kosten: Bruchteile eines Cents je Lauf. Der Schlüssel kommt aus OPENAI_API_KEY
# oder aus Documents\Johann\.env und wird nie ausgegeben.
param(
    [string]$Model = 'gpt-4o-mini-tts',
    [string]$Voice = 'onyx',
    [string]$EnvFile = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Johann\.env')
)

$ErrorActionPreference = 'Stop'

$key = $env:OPENAI_API_KEY
if (-not $key -and (Test-Path $EnvFile)) {
    $line = Get-Content $EnvFile | Where-Object { $_ -match '^\s*OPENAI_API_KEY=' } | Select-Object -First 1
    if ($line) { $key = ($line -replace '^\s*OPENAI_API_KEY=', '').Trim().Trim('"', "'") }
}
if (-not $key) { throw 'Kein OPENAI_API_KEY gefunden (Umgebung oder .env).' }

$dir = Join-Path $PSScriptRoot '..\tests\fixtures\dictations'
Get-ChildItem $dir -Filter 'D*.txt' | Sort-Object Name | ForEach-Object {
    $out = [IO.Path]::ChangeExtension($_.FullName, '.mp3')
    $body = @{
        model           = $Model
        voice           = $Voice
        input           = (Get-Content $_.FullName -Raw -Encoding utf8).Trim()
        response_format = 'mp3'
    } | ConvertTo-Json
    Invoke-RestMethod -Uri 'https://api.openai.com/v1/audio/speech' -Method Post `
        -Headers @{ Authorization = "Bearer $key" } -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes($body)) -OutFile $out
    Write-Host ('{0} -> {1} KB' -f $_.Name, [Math]::Round((Get-Item $out).Length / 1KB))
}
