# UI-Automation (Audit + FlaUI-Suite) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Johann lässt sich gefahrlos gegen eine Sandbox fernsteuern — erst für einen einmaligen, belegten Audit der echten App (Phase A), dann als dauerhafte FlaUI-Suite gegen einen OpenAI-Stub (Phase B).

**Architecture:** Drei Umgebungsvariablen (`JOHANN_HOME`, `JOHANN_OPENAI_ENDPOINT`, `JOHANN_NO_UPDATE_CHECK`) machen Johann umlenkbar, aufgelöst in einer statischen Klasse `JohannEnvironment` (Infrastructure). Eine Testhilfe-Bibliothek `Platee.Johann.UiDriver` bündelt FlaUI-Bedienung, Sandbox-Aufbau mit Wächter und einen OpenAI-Stub-Server; das Konsolenwerkzeug `tools/ui-driver` (für Audit-Agenten) und das Testprojekt `Platee.Johann.UiTests` (Phase B) nutzen sie beide. Johann selbst wird nur als Prozess gestartet.

**Tech Stack:** .NET 10, WPF, FlaUI.UIA3 5.0.0, xUnit 2.9 + FluentAssertions 8.8, OpenAI SDK 2.2.0 (`OpenAIClientOptions.Endpoint`), `HttpListener`, NAudio 2.2.1 (MediaFoundation, nur für die Stille-Datei D8), PowerShell 7.

**Spec:** `docs/ui-automation/design.md` (Issue #111, Branch `test/111-ui-automation`)

## Global Constraints

- Niemals in `Z:\12_Tools\Peano\Johann\prompts.json` schreiben; niemals `Documents\Johann` als Arbeitsordner eines Automationslaufs benutzen.
- **Wächter vor jedem Start:** keine Zeichenkette in der `settings.json` der Sandbox darf mit `Z:\` beginnen oder den echten `Documents\Johann`-Pfad enthalten — sonst Abbruch.
- Ungesetzt/leer verhält sich jede Variable exakt wie heute. Gesetzt und ungültig → Start verweigert (Ausnahme mit Variablenname), nie stiller Rückfall.
- Ist `JOHANN_HOME` gesetzt, sucht `ApiKeyProvider` keine `.env` in Elternordnern der EXE.
- Phase-B-Läufe setzen immer `OPENAI_API_KEY=sk-stub-not-a-real-key`, damit eine fehlgeleitete Anfrage mit 401 scheitert statt Geld zu kosten.
- Kein Outlook außer im beaufsichtigten Audit-Schritt 7. Kein zweiter Johann-Prozess während eines Laufs.
- `Platee.Johann.UiTests` läuft **nicht** in `dotnet test` der Lösung und nicht im Pre-Push-Hook (`IsTestProject=false` außer mit `-p:RunUiTests=true`).
- Projektkonventionen (CLAUDE.md): sealed records/immutabel, file-scoped namespaces, `Nullable` an, kein `!`, StyleCop (System-usings zuerst), Tests `<Subject>Tests.cs` in `Platee.Johann.Tests/Unit/`, Commits `<typ>: <beschreibung> (#111)` auf Deutsch mit `Co-Authored-By`-Zeile.
- Immer `dotnet build` der **ganzen** Lösung vor „fertig“ (UI-Projekt hat kein implizites `System.IO`).
- UI-Sichtbarkeit: `AutomationId`s ändern weder Aussehen noch Verhalten.

## Review Focus

1. **`JOHANN_HOME` mit Anführungszeichen oder abschließendem Backslash** (`"C:\x\"` aus einer Shell) — erwartet: wird getrimmt und normalisiert, nicht als ungültig abgelehnt und nicht als anderer Ordner verstanden → Test in Task 1.
2. **Sandbox-`settings.json` mit Pfad in anderer Schreibweise** (`c:/users/jw/documents/johann/output`, Kleinbuchstaben, Schrägstriche) — erwartet: Wächter schlägt trotzdem an → Test in Task 5.
3. **Stub bekommt eine Anfrage, die keiner Regel entspricht** — erwartet: klare 500-Antwort mit Text „kein Stub für …“ im Protokoll, kein Hängen des Tests → Test in Task 3.
4. **Johann zeigt beim Start einen unerwarteten Dialog** (Pfadwarnung, fehlende `.env`) — erwartet: `JohannSession.Launch` meldet „unerwartetes Fenster ‚…‘“ mit Screenshot statt zu warten, bis der Timeout läuft → Test in Task 10.
5. **Ein Johann läuft bereits (Debug vom Entwickler)** — erwartet: `ui-driver start` und die Suite brechen mit „Johann läuft bereits (PID …)“ ab, statt sich an den falschen Prozess zu hängen → Test in Task 6 (CLI) und Task 10 (Fixture).

---

## Dateistruktur

```
Platee.Johann.Infrastructure/Hosting/JohannEnvironment.cs      Variablen lesen/prüfen (neu)
Platee.Johann.Infrastructure/Llm/ApiKeyProvider.cs              Home aus JohannEnvironment, kein Walk-up bei Override
Platee.Johann.Infrastructure/Llm/OpenAiLlmProvider.cs           optionaler apiRoot
Platee.Johann.Infrastructure/Llm/WhisperTranscriber.cs          optionaler apiRoot
Platee.Johann.UI/App.xaml.cs                                    settingsDir, Default-Roots, Endpoint, Update-Prüfung
Platee.Johann.UI/MainWindow.xaml, Views/*.xaml                  AutomationIds (Task 9)

Platee.Johann.UiDriver/                                         Testhilfe-Bibliothek (neu, net10.0-windows)
  Stub/OpenAiStubServer.cs        HttpListener, Regeln, Protokoll
  Stub/StubRequest.cs             protokollierte Anfrage (record)
  Stub/EntryFixture.cs            liest Abschnittstexte aus einem gespeicherten _status.json
  Sandbox/SandboxLayout.cs        Pfade einer Sandbox (record)
  Sandbox/SandboxGuard.cs         Wächter
  Sandbox/AuditSandbox.cs         Kopie der echten Daten, Pfade umgebogen
  Sandbox/TestSandbox.cs          frische Sandbox für Phase B
  Automation/JohannLaunchOptions.cs
  Automation/JohannSession.cs     Start/Attach, Finden, Bedienen, Screenshot, Baum
  Automation/UiTreeDump.cs        UI-Baum → JSON
  Automation/KeyChord.cs          "Ctrl+Plus" → VirtualKeyShort[]
  Audio/SilenceMp3.cs             D8-Erzeugung

tools/ui-driver/                                                Konsolenwerkzeug (neu)
  UiDriverTool.csproj, Program.cs, Commands.cs

Platee.Johann.UiTests/                                          Phase B (neu)
  UiTestContext.cs, ExeLocator.cs, Fixtures/ (aus Audit), *FlowTests.cs

tests/fixtures/dictations/D1..D6.txt                            Diktattexte (neu)
scripts/new-dictation-fixtures.ps1                              TTS → MP3
scripts/new-audit-sandbox.ps1                                   Wrapper um ui-driver sandbox new
scripts/run-ui-tests.ps1                                        Phase-B-Lauf
docs/ui-automation/audit-runbook.md                             Ablauf je Flow
docs/audit/2026-09-<tag>-v1.5.0.md + docs/audit/<lauf>/*.png    Befundbericht
```

---

## Phase 0 — Johann umlenkbar machen

### Task 1: `JohannEnvironment`

**Files:**
- Create: `Platee.Johann.Infrastructure/Hosting/JohannEnvironment.cs`
- Test: `Platee.Johann.Tests/Unit/JohannEnvironmentTests.cs`

**Interfaces:**
- Produces:
  - `static string JohannEnvironment.HomeDirectory(Func<string, string?>? read = null)`
  - `static bool JohannEnvironment.HasHomeOverride(Func<string, string?>? read = null)`
  - `static Uri? JohannEnvironment.OpenAiRoot(Func<string, string?>? read = null)` — Wurzel **ohne** `v1`, immer mit `/` am Ende
  - `static bool JohannEnvironment.SkipUpdateCheck(Func<string, string?>? read = null)`
  - Konstanten `HomeVariable`, `OpenAiEndpointVariable`, `NoUpdateCheckVariable`

- [ ] **Step 1: Failing tests schreiben**

```csharp
namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Infrastructure.Hosting;
using Xunit;

public sealed class JohannEnvironmentTests
{
    private static Func<string, string?> Vars(params (string Name, string Value)[] vars) =>
        name => vars.FirstOrDefault(v => v.Name == name).Value;

    private static readonly string DefaultHome = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");

    [Fact]
    public void HomeDirectory_Unset_IsDocumentsJohann()
    {
        JohannEnvironment.HomeDirectory(Vars()).Should().Be(DefaultHome);
        JohannEnvironment.HasHomeOverride(Vars()).Should().BeFalse();
    }

    [Fact]
    public void HomeDirectory_Blank_IsDocumentsJohann()
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", "   "))).Should().Be(DefaultHome);
    }

    [Fact]
    public void HomeDirectory_Absolute_IsUsed()
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", @"C:\sandbox\home")))
            .Should().Be(@"C:\sandbox\home");
        JohannEnvironment.HasHomeOverride(Vars(("JOHANN_HOME", @"C:\sandbox\home"))).Should().BeTrue();
    }

    [Theory]
    [InlineData("\"C:\\sandbox\\home\\\"")]
    [InlineData(" C:\\sandbox\\home\\ ")]
    [InlineData("C:/sandbox/home")]
    public void HomeDirectory_QuotedOrTrailingSlash_IsNormalised(string raw)
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", raw))).Should().Be(@"C:\sandbox\home");
    }

    [Theory]
    [InlineData("sandbox\\home")]
    [InlineData("..\\home")]
    [InlineData("\\\\?\\")]
    public void HomeDirectory_Invalid_Throws_NamingTheVariable(string raw)
    {
        var act = () => JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", raw)));
        act.Should().Throw<InvalidOperationException>().WithMessage("*JOHANN_HOME*");
    }

    [Fact]
    public void OpenAiRoot_Unset_IsNull()
    {
        JohannEnvironment.OpenAiRoot(Vars()).Should().BeNull();
    }

    [Theory]
    [InlineData("http://localhost:5123", "http://localhost:5123/")]
    [InlineData("http://localhost:5123/", "http://localhost:5123/")]
    [InlineData("https://proxy.example/openai", "https://proxy.example/openai/")]
    public void OpenAiRoot_Valid_EndsWithSlash(string raw, string expected)
    {
        JohannEnvironment.OpenAiRoot(Vars(("JOHANN_OPENAI_ENDPOINT", raw)))!.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData("localhost:5123")]
    [InlineData("ftp://localhost/")]
    [InlineData("nicht eine url")]
    public void OpenAiRoot_Invalid_Throws_NamingTheVariable(string raw)
    {
        var act = () => JohannEnvironment.OpenAiRoot(Vars(("JOHANN_OPENAI_ENDPOINT", raw)));
        act.Should().Throw<InvalidOperationException>().WithMessage("*JOHANN_OPENAI_ENDPOINT*");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    public void SkipUpdateCheck(string? raw, bool expected)
    {
        JohannEnvironment.SkipUpdateCheck(name => name == "JOHANN_NO_UPDATE_CHECK" ? raw : null)
            .Should().Be(expected);
    }
}
```

- [ ] **Step 2: Laufen lassen, muss scheitern**

Run: `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~JohannEnvironmentTests"`
Expected: Build-Fehler „JohannEnvironment ist nicht vorhanden“.

- [ ] **Step 3: Implementieren**

```csharp
namespace Platee.Johann.Infrastructure.Hosting;

/// <summary>
/// Liest die Umgebungsvariablen, mit denen sich Johann für Automationsläufe umlenken lässt (#111).
/// Ungesetzt oder leer gilt das heutige Verhalten. Gesetzt und ungültig wirft — ein stiller
/// Rückfall landete bei einem Tippfehler in den echten Daten oder bei der bezahlten API.
/// </summary>
public static class JohannEnvironment
{
    public const string HomeVariable = "JOHANN_HOME";
    public const string OpenAiEndpointVariable = "JOHANN_OPENAI_ENDPOINT";
    public const string NoUpdateCheckVariable = "JOHANN_NO_UPDATE_CHECK";

    public static string HomeDirectory(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(HomeVariable));
        if (raw is null)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        }

        string full;
        try
        {
            if (!Path.IsPathFullyQualified(raw))
            {
                throw new ArgumentException("kein absoluter Pfad");
            }

            full = Path.GetFullPath(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"{HomeVariable} ist gesetzt, aber kein gültiger absoluter Ordner: '{raw}'.", ex);
        }

        var root = Path.GetPathRoot(full);
        return string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
            ? full
            : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static bool HasHomeOverride(Func<string, string?>? read = null) =>
        Clean((read ?? Environment.GetEnvironmentVariable)(HomeVariable)) is not null;

    public static Uri? OpenAiRoot(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(OpenAiEndpointVariable));
        if (raw is null)
        {
            return null;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{OpenAiEndpointVariable} ist gesetzt, aber keine http(s)-Adresse: '{raw}'.");
        }

        return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
    }

    public static bool SkipUpdateCheck(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(NoUpdateCheckVariable));
        return raw is "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim().Trim('"', '\'').Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
```

- [ ] **Step 4: Tests grün**

Run: `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~JohannEnvironmentTests"`
Expected: alle bestanden. Falls `"\\\\?\\"` nicht wirft: `Path.GetFullPath` auf Geräte-Pfade prüfen und zusätzlich `raw.StartsWith(@"\\?\")` ablehnen.

- [ ] **Step 5: Commit**

```bash
git add Platee.Johann.Infrastructure/Hosting/JohannEnvironment.cs Platee.Johann.Tests/Unit/JohannEnvironmentTests.cs
git commit -m "feat: JohannEnvironment liest JOHANN_HOME, Endpunkt und Update-Schalter (#111)"
```

### Task 2: `JOHANN_HOME` verdrahten

**Files:**
- Modify: `Platee.Johann.Infrastructure/Llm/ApiKeyProvider.cs` (ganze Datei)
- Modify: `Platee.Johann.UI/App.xaml.cs:50-51` (settingsDir), `:530-548` (`ResolveDefaultOutputRoot`, `ResolveDefaultInputRoot`), Anfang von `OnStartup` (Fehlermeldung)
- Test: `Platee.Johann.Tests/Unit/ApiKeyProviderTests.cs` (neu oder erweitern, falls vorhanden)

**Interfaces:**
- Consumes: `JohannEnvironment.HomeDirectory`, `HasHomeOverride` (Task 1)
- Produces: `static string? ApiKeyProvider.TryGetOpenAiKey(Func<string, string?>? read = null, string? baseDirectory = null)` — bestehende Aufrufe ohne Argument bleiben gültig.

- [ ] **Step 1: Failing tests**

```csharp
namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Infrastructure.Llm;
using Xunit;

public sealed class ApiKeyProviderHomeTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("johann-key-").FullName;

    public void Dispose() => Directory.Delete(this.root, recursive: true);

    [Fact]
    public void HomeOverride_ReadsEnvFromHome()
    {
        var home = Directory.CreateDirectory(Path.Combine(this.root, "home")).FullName;
        File.WriteAllText(Path.Combine(home, ".env"), "OPENAI_API_KEY=sk-home\n");

        ApiKeyProvider.TryGetOpenAiKey(Read(home), baseDirectory: this.root).Should().Be("sk-home");
    }

    [Fact]
    public void HomeOverride_DoesNotWalkUpFromExe()
    {
        var home = Directory.CreateDirectory(Path.Combine(this.root, "home")).FullName;
        var exeDir = Directory.CreateDirectory(Path.Combine(this.root, "repo", "bin")).FullName;
        File.WriteAllText(Path.Combine(this.root, "repo", ".env"), "OPENAI_API_KEY=sk-repo\n");

        ApiKeyProvider.TryGetOpenAiKey(Read(home), baseDirectory: exeDir).Should().BeNull();
    }

    [Fact]
    public void NoOverride_StillWalksUp()
    {
        var exeDir = Directory.CreateDirectory(Path.Combine(this.root, "repo", "bin")).FullName;
        File.WriteAllText(Path.Combine(this.root, "repo", ".env"), "OPENAI_API_KEY=sk-repo\n");

        // Kein JOHANN_HOME, kein OPENAI_API_KEY. Documents\Johann\.env des Entwicklers kann existieren —
        // dann gewinnt sie (heutiges Verhalten). Der Test prüft nur, dass der Walk-up noch greift.
        var key = ApiKeyProvider.TryGetOpenAiKey(_ => null, baseDirectory: exeDir);
        key.Should().NotBeNull();
    }

    [Fact]
    public void EnvironmentVariable_StillWins()
    {
        ApiKeyProvider.TryGetOpenAiKey(n => n == "OPENAI_API_KEY" ? "sk-env" : null, this.root)
            .Should().Be("sk-env");
    }

    private static Func<string, string?> Read(string home) =>
        name => name == "JOHANN_HOME" ? home : null;
}
```

- [ ] **Step 2: Scheitern sehen**

Run: `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~ApiKeyProviderHomeTests"`
Expected: Build-Fehler (Überladung fehlt).

- [ ] **Step 3: `ApiKeyProvider` umbauen**

`TryGetOpenAiKey` bekommt die zwei optionalen Parameter; Schritt 2 nutzt `JohannEnvironment.HomeDirectory(read)`; Schritt 3 läuft nur ohne Override:

```csharp
public static string? TryGetOpenAiKey(Func<string, string?>? read = null, string? baseDirectory = null)
{
    read ??= Environment.GetEnvironmentVariable;

    // 1. Environment variable (highest priority)
    var fromEnv = read("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv.Trim();
    }

    // 2. <Johann-Home>\.env – Documents\Johann, oder JOHANN_HOME (#111)
    var homeEnv = Path.Combine(JohannEnvironment.HomeDirectory(read), ".env");
    if (File.Exists(homeEnv))
    {
        var key = ParseEnvFile(homeEnv, "OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }
    }

    // Eine umgelenkte Sandbox darf keinen Schlüssel aus dem Repo neben der EXE finden.
    if (JohannEnvironment.HasHomeOverride(read))
    {
        return null;
    }

    // 3. Walk up parent directories looking for a .env file
    var dir = new DirectoryInfo(baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory);
    // … Schleife unverändert …
}
```

`using Platee.Johann.Infrastructure.Hosting;` ergänzen, XML-Kommentar der Klasse um Schritt 2 (JOHANN_HOME) und die Walk-up-Ausnahme ergänzen.

- [ ] **Step 4: `App.xaml.cs` verdrahten**

Ganz am Anfang von `OnStartup` (vor `base.OnStartup(e)` ist der Crash-Logger schon da) den Home-Ordner einmal auflösen und bei ungültigem Wert beenden:

```csharp
string johannHome;
try
{
    johannHome = JohannEnvironment.HomeDirectory();
    _ = JohannEnvironment.OpenAiRoot();   // früh prüfen, damit ein Tippfehler nicht erst beim ersten Diktat auffällt
}
catch (InvalidOperationException ex)
{
    crashLogger.WriteCrashLog("ENVIRONMENT", ex);
    MessageBox.Show(ex.Message, "Platé.Johann – Start abgebrochen", MessageBoxButton.OK, MessageBoxImage.Error);
    this.Shutdown(1);
    return;
}
```

Dann `settingsDir = johannHome;` (ersetzt Zeile 50–51), `ResolveDefaultOutputRoot` → `Path.Combine(JohannEnvironment.HomeDirectory(), "output")`, `ResolveDefaultInputRoot` → `Path.Combine(JohannEnvironment.HomeDirectory(), "Eingang")`. `using Platee.Johann.Infrastructure.Hosting;` ergänzen.

- [ ] **Step 5: Build + Tests**

Run: `dotnet build` dann `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~ApiKeyProvider|FullyQualifiedName~JohannEnvironment|FullyQualifiedName~StartupPathResolver"`
Expected: 0 Fehler, alle grün.

- [ ] **Step 6: Commit**

```bash
git add Platee.Johann.Infrastructure/Llm/ApiKeyProvider.cs Platee.Johann.UI/App.xaml.cs Platee.Johann.Tests/Unit/ApiKeyProviderHomeTests.cs
git commit -m "feat: JOHANN_HOME ersetzt Documents\\Johann als Einstellungsordner (#111)"
```

### Task 3: `Platee.Johann.UiDriver` + OpenAI-Stub-Server

**Files:**
- Create: `Platee.Johann.UiDriver/Platee.Johann.UiDriver.csproj`
- Create: `Platee.Johann.UiDriver/Stub/StubRequest.cs`, `Stub/OpenAiStubServer.cs`, `Stub/EntryFixture.cs`
- Modify: `Platee.Johann.slnx` (Projekt aufnehmen), `Platee.Johann.Tests/Platee.Johann.Tests.csproj` (ProjectReference)
- Test: `Platee.Johann.Tests/Unit/OpenAiStubServerTests.cs`

**Interfaces:**
- Produces:
  - `sealed record StubRequest(string Method, string Path, string Body, string? Section)`
  - `sealed class OpenAiStubServer : IDisposable` mit `static OpenAiStubServer Start()`, `Uri Root` (z. B. `http://localhost:51234/`), `IReadOnlyList<StubRequest> Requests`, `void OnChat(Func<string /*userContent*/, string?> responder)`, `void OnTranscription(Func<string /*fileName*/, string?> responder)`, `void FailNext(string pathPrefix, int status)`, `void Hang(string pathPrefix)`, `void MissingModel(string modelId)`
  - `sealed record EntryFixture(string Transcript, string Title, IReadOnlyDictionary<string, string> Sections)` mit `static EntryFixture Load(string statusJsonPath)`; Schlüssel: `abstract`, `longSummary`, `proseSummary`, `emailText`, `conversationNote`, `taskList`, `stundenzettelText`, `analogText`

- [ ] **Step 1: Projekt anlegen**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FlaUI.UIA3" Version="5.0.0" />
    <PackageReference Include="NAudio" Version="2.2.1" />
  </ItemGroup>
</Project>
```

In `Platee.Johann.slnx` ein `<Project Path="Platee.Johann.UiDriver/Platee.Johann.UiDriver.csproj" />` ergänzen; im Testprojekt `<ProjectReference Include="..\Platee.Johann.UiDriver\Platee.Johann.UiDriver.csproj" />`.
Run: `dotnet build` → Expected: 0 Fehler.

- [ ] **Step 2: Failing tests für den Stub**

```csharp
namespace Platee.Johann.Tests.Unit;

using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Platee.Johann.UiDriver.Stub;
using Xunit;

public sealed class OpenAiStubServerTests
{
    private static StringContent Chat(string user) => new(
        $$"""{"model":"gpt-5.6-luna","messages":[{"role":"system","content":"sys"},{"role":"user","content":{{System.Text.Json.JsonSerializer.Serialize(user)}}}]}""",
        Encoding.UTF8, "application/json");

    [Fact]
    public async Task Chat_UsesResponder_AndLogsRequest()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(user => user.StartsWith("Titel") ? "Mein Titel" : null);
        using var http = new HttpClient { BaseAddress = stub.Root };

        var response = await http.PostAsync("v1/chat/completions", Chat("Titel bitte"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"content\":\"Mein Titel\"");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/chat/completions");
    }

    [Fact]
    public async Task Chat_WithoutMatchingRule_Returns500_NamingTheRequest()
    {
        using var stub = OpenAiStubServer.Start();
        using var http = new HttpClient { BaseAddress = stub.Root };

        var response = await http.PostAsync("v1/chat/completions", Chat("unbekannt"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).Should().Contain("kein Stub für");
    }

    [Fact]
    public async Task FailNext_AppliesOnce()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(_ => "ok");
        stub.FailNext("/v1/chat/completions", 500);
        using var http = new HttpClient { BaseAddress = stub.Root };

        (await http.PostAsync("v1/chat/completions", Chat("a"))).StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await http.PostAsync("v1/chat/completions", Chat("a"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Models_ExistUnlessMarkedMissing()
    {
        using var stub = OpenAiStubServer.Start();
        stub.MissingModel("gpt-5.6-sol");
        using var http = new HttpClient { BaseAddress = stub.Root };

        (await http.GetAsync("v1/models/gpt-5.6-luna")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await http.GetAsync("v1/models/gpt-5.6-sol")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void EntryFixture_ReadsSectionsFromStatusJson()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("fx-").FullName, "x_status.json");
        File.WriteAllText(path, """{"title":"T","transcript":"Hallo","taskList":"- a","abstract":"kurz"}""");

        var fx = EntryFixture.Load(path);

        fx.Title.Should().Be("T");
        fx.Transcript.Should().Be("Hallo");
        fx.Sections.Should().Contain("taskList", "- a").And.Contain("abstract", "kurz");
    }
}
```

- [ ] **Step 3: Scheitern sehen**

Run: `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~OpenAiStubServerTests"` → Build-Fehler.

- [ ] **Step 4: Implementieren**

`StubRequest.cs`:
```csharp
namespace Platee.Johann.UiDriver.Stub;

public sealed record StubRequest(string Method, string Path, string Body, string? FileName);
```

`OpenAiStubServer.cs` (Kern; `HttpListener` auf `http://localhost:<freier Port>/`, Port via `TcpListener(IPAddress.Loopback, 0)` ermitteln):
```csharp
namespace Platee.Johann.UiDriver.Stub;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class OpenAiStubServer : IDisposable
{
    private readonly HttpListener listener = new();
    private readonly ConcurrentQueue<StubRequest> requests = new();
    private readonly ConcurrentDictionary<string, int> failNext = new();
    private readonly ConcurrentDictionary<string, bool> hanging = new();
    private readonly ConcurrentDictionary<string, bool> missingModels = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource stop = new();
    private Func<string, string?> chat = _ => null;
    private Func<string, string?> transcription = _ => null;

    private OpenAiStubServer(int port)
    {
        this.Root = new Uri($"http://localhost:{port}/");
        this.listener.Prefixes.Add(this.Root.AbsoluteUri);
    }

    public Uri Root { get; }

    public IReadOnlyList<StubRequest> Requests => this.requests.ToArray();

    public static OpenAiStubServer Start()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var server = new OpenAiStubServer(port);
        server.listener.Start();
        _ = Task.Run(server.LoopAsync);
        return server;
    }

    public void OnChat(Func<string, string?> responder) => this.chat = responder;

    public void OnTranscription(Func<string, string?> responder) => this.transcription = responder;

    public void FailNext(string pathPrefix, int status) => this.failNext[pathPrefix] = status;

    public void Hang(string pathPrefix) => this.hanging[pathPrefix] = true;

    public void MissingModel(string modelId) => this.missingModels[modelId] = true;

    public void Dispose()
    {
        this.stop.Cancel();
        this.listener.Close();
    }

    private async Task LoopAsync()
    {
        while (!this.stop.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await this.listener.GetContextAsync();
            }
            catch (Exception) when (this.stop.IsCancellationRequested)
            {
                return;
            }

            _ = Task.Run(() => this.HandleAsync(ctx));
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;
        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var fileName = path.EndsWith("/audio/transcriptions", StringComparison.Ordinal)
            ? MultipartFileName(body)
            : null;
        this.requests.Enqueue(new StubRequest(ctx.Request.HttpMethod, path, body, fileName));

        var hang = this.hanging.Keys.FirstOrDefault(p => path.StartsWith(p, StringComparison.Ordinal));
        if (hang is not null)
        {
            await Task.Delay(Timeout.Infinite, this.stop.Token).ContinueWith(_ => { });
            return;
        }

        var fail = this.failNext.Keys.FirstOrDefault(p => path.StartsWith(p, StringComparison.Ordinal));
        if (fail is not null && this.failNext.TryRemove(fail, out var status))
        {
            await Write(ctx, status, """{"error":{"message":"stub failure","type":"server_error"}}""");
            return;
        }

        if (path.StartsWith("/v1/models/", StringComparison.Ordinal))
        {
            var id = Uri.UnescapeDataString(path["/v1/models/".Length..]);
            await (this.missingModels.ContainsKey(id)
                ? Write(ctx, 404, """{"error":{"message":"model not found","type":"invalid_request_error"}}""")
                : Write(ctx, 200, JsonSerializer.Serialize(new { id, @object = "model", created = 0, owned_by = "stub" })));
            return;
        }

        if (path == "/v1/chat/completions")
        {
            var user = JsonNode.Parse(body)?["messages"]?.AsArray().LastOrDefault()?["content"]?.GetValue<string>() ?? string.Empty;
            var text = this.chat(user);
            await (text is null
                ? Write(ctx, 500, $"kein Stub für Chat-Anfrage: {Shorten(user)}")
                : Write(ctx, 200, ChatCompletion(text)));
            return;
        }

        if (path == "/v1/audio/transcriptions")
        {
            var text = this.transcription(fileName ?? string.Empty);
            await (text is null
                ? Write(ctx, 500, $"kein Stub für Transkription: {fileName}")
                : Write(ctx, 200, JsonSerializer.Serialize(new { text })));
            return;
        }

        await Write(ctx, 404, $"kein Stub für {ctx.Request.HttpMethod} {path}");
    }

    private static string ChatCompletion(string text) => JsonSerializer.Serialize(new
    {
        id = "chatcmpl-stub",
        @object = "chat.completion",
        created = 0,
        model = "stub",
        choices = new[] { new { index = 0, message = new { role = "assistant", content = text }, finish_reason = "stop" } },
        usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 },
    });

    private static string? MultipartFileName(string body)
    {
        const string marker = "filename=";
        var i = body.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }

        var rest = body[(i + marker.Length)..].TrimStart('"');
        var end = rest.IndexOfAny(['"', '\r', '\n', ';']);
        return end < 0 ? rest : rest[..end];
    }

    private static string Shorten(string s) => s.Length <= 80 ? s : s[..80] + "…";

    private static async Task Write(HttpListenerContext ctx, int status, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = body.StartsWith('{') ? "application/json" : "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }
}
```
Hinweis: Die Transkription ist multipart; der Stub liest den Body als Text nur, um den Dateinamen zu finden — die MP3-Bytes sind egal.

`EntryFixture.cs`:
```csharp
namespace Platee.Johann.UiDriver.Stub;

using System.Text.Json.Nodes;

public sealed record EntryFixture(string Transcript, string Title, IReadOnlyDictionary<string, string> Sections)
{
    public static readonly string[] SectionKeys =
        ["abstract", "longSummary", "proseSummary", "emailText", "conversationNote", "taskList", "stundenzettelText", "analogText"];

    public static EntryFixture Load(string statusJsonPath)
    {
        var node = JsonNode.Parse(File.ReadAllText(statusJsonPath))!;
        var sections = SectionKeys
            .Select(k => (k, v: node[k]?.GetValue<string>()))
            .Where(p => !string.IsNullOrEmpty(p.v))
            .ToDictionary(p => p.k, p => p.v!);
        return new EntryFixture(
            node["transcript"]?.GetValue<string>() ?? string.Empty,
            node["title"]?.GetValue<string>() ?? string.Empty,
            sections);
    }
}
```
Vor dem Implementieren prüfen: `JsonRepository` schreibt `_status.json` camelCase (`EntryDto`-Felder oben). Falls PascalCase, Schlüssel entsprechend anpassen und Test mitziehen.

- [ ] **Step 5: Tests grün**

Run: `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~OpenAiStubServerTests"` → alle bestanden. Scheitert `HttpListener` mit „Zugriff verweigert“: `localhost` statt `127.0.0.1` verwenden (braucht keine URL-ACL).

- [ ] **Step 6: Commit**

```bash
git add Platee.Johann.UiDriver Platee.Johann.slnx Platee.Johann.Tests/Platee.Johann.Tests.csproj Platee.Johann.Tests/Unit/OpenAiStubServerTests.cs
git commit -m "test: UiDriver-Bibliothek mit OpenAI-Stub-Server (#111)"
```

### Task 4: `JOHANN_OPENAI_ENDPOINT` + `JOHANN_NO_UPDATE_CHECK` verdrahten

**Files:**
- Modify: `Platee.Johann.Infrastructure/Llm/OpenAiLlmProvider.cs` (Konstruktor, `ResolveClient`)
- Modify: `Platee.Johann.Infrastructure/Llm/WhisperTranscriber.cs:39-42` (Konstruktor)
- Modify: `Platee.Johann.UI/App.xaml.cs:224-231` (Provider), `:267` (Probe), `:408` (Update-Prüfung)
- Test: `Platee.Johann.Tests/Unit/OpenAiEndpointOverrideTests.cs`

**Interfaces:**
- Consumes: `JohannEnvironment.OpenAiRoot` (Task 1), `OpenAiStubServer` (Task 3)
- Produces: `OpenAiLlmProvider(string apiKey, Uri? apiRoot = null)`, `WhisperTranscriber(string apiKey, Uri? apiRoot = null)`; `apiRoot` = Wurzel ohne `v1`.

- [ ] **Step 1: Failing tests**

```csharp
namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Infrastructure.Llm;
using Platee.Johann.UiDriver.Stub;
using Xunit;

public sealed class OpenAiEndpointOverrideTests
{
    [Fact]
    public async Task LlmProvider_SendsToOverriddenRoot()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(_ => "vom Stub");
        var provider = new OpenAiLlmProvider("sk-stub", stub.Root);

        var text = await provider.GenerateAsync("sys", "user", new LlmOptions(100, false, "gpt-5.6-luna"));

        text.Should().Be("vom Stub");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/chat/completions");
    }

    [Fact]
    public async Task Transcriber_SendsToOverriddenRoot()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnTranscription(name => name == "d1.mp3" ? "Hallo Welt" : null);
        var dir = Directory.CreateTempSubdirectory("tr-").FullName;
        var mp3 = Path.Combine(dir, "d1.mp3");
        File.WriteAllBytes(mp3, new byte[1024]);   // Inhalt egal, Dauer-Leser wirft nie

        var result = await new WhisperTranscriber("sk-stub", stub.Root).TranscribeAsync(mp3);

        result.Transcript.Should().Be("Hallo Welt");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/audio/transcriptions");
    }
}
```
Vor Step 2 die tatsächliche Signatur von `LlmOptions` und den Feldnamen von `TranscriptionResult` (`Transcript`/`Text`) mit Serena `find_symbol` prüfen und den Test anpassen.

- [ ] **Step 2: Scheitern sehen** — `dotnet test … --filter "FullyQualifiedName~OpenAiEndpointOverrideTests"` → Build-Fehler.

- [ ] **Step 3: Provider umbauen**

`OpenAiLlmProvider`:
```csharp
private readonly Uri? apiRoot;

public OpenAiLlmProvider(string apiKey, Uri? apiRoot = null)
{
    this.apiKey = apiKey;
    this.apiRoot = apiRoot;
}

private ChatClient ResolveClient(string? model) =>
    this.clients.GetOrAdd(
        model ?? SummaryModelCatalog.Default.Id,
        id => this.apiRoot is null
            ? new ChatClient(id, this.apiKey)
            : new ChatClient(id, new ApiKeyCredential(this.apiKey), new OpenAIClientOptions { Endpoint = new Uri(this.apiRoot, "v1") }));
```
`using System.ClientModel; using OpenAI;` ergänzen. `WhisperTranscriber` analog:
```csharp
public WhisperTranscriber(string apiKey, Uri? apiRoot = null)
{
    this.client = apiRoot is null
        ? new AudioClient(ModelName, apiKey)
        : new AudioClient(ModelName, new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(apiRoot, "v1") });
}
```

- [ ] **Step 4: `App.xaml.cs`**

```csharp
var openAiRoot = JohannEnvironment.OpenAiRoot();   // bereits beim Start geprüft (Task 2)

ILlmProvider llmProvider = apiKey is not null
    ? new OpenAiLlmProvider(apiKey, openAiRoot)
    : new NoOpLlmProvider();

IAudioTranscriber transcriber = apiKey is not null
    ? new WhisperTranscriber(apiKey, openAiRoot)
    : new NoOpAudioTranscriber();
```
Probe (Zeile 267): `new OpenAiModelAvailabilityProbe(apiKey, openAiRoot)` (der `baseAddress`-Parameter erwartet genau die Wurzel ohne `v1`). Update (Zeile 408):
```csharp
if (!JohannEnvironment.SkipUpdateCheck())
{
    _ = CheckForUpdatesAsync(crashLogger);
}
```

- [ ] **Step 5: Build + Tests** — `dotnet build`; `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~OpenAiEndpointOverrideTests|FullyQualifiedName~OpenAiModelAvailabilityProbe|FullyQualifiedName~WhisperTranscriber"` → grün.

- [ ] **Step 6: Gesamtlauf** — `dotnet test` → alle grün (vorher 816+).

- [ ] **Step 7: Commit**

```bash
git add Platee.Johann.Infrastructure/Llm Platee.Johann.UI/App.xaml.cs Platee.Johann.Tests/Unit/OpenAiEndpointOverrideTests.cs
git commit -m "feat: JOHANN_OPENAI_ENDPOINT und JOHANN_NO_UPDATE_CHECK (#111)"
```

---

## Phase A — Werkzeug und Audit

### Task 5: Sandbox mit Wächter

**Files:**
- Create: `Platee.Johann.UiDriver/Sandbox/SandboxLayout.cs`, `SandboxGuard.cs`, `AuditSandbox.cs`, `TestSandbox.cs`
- Test: `Platee.Johann.Tests/Unit/SandboxGuardTests.cs`, `Platee.Johann.Tests/Unit/AuditSandboxTests.cs`

**Interfaces:**
- Produces:
  - `sealed record SandboxLayout(string Root)` mit `Home`, `Output`, `Eingang`, `Archiv`, `TeamPrompts`, `SettingsFile`
  - `static IReadOnlyList<string> SandboxGuard.Violations(string settingsJson, IEnumerable<string> forbiddenRoots)`
  - `static IReadOnlyList<string> SandboxGuard.DefaultForbiddenRoots()` → `[@"Z:\", <Documents>\Johann]`
  - `static void SandboxGuard.Ensure(SandboxLayout layout)` — wirft `InvalidOperationException` mit allen Verstößen
  - `static SandboxLayout AuditSandbox.Create(string root, string realHome, string? teamPromptFile)`
  - `static SandboxLayout TestSandbox.Create(string root)`

- [ ] **Step 1: Failing tests**

```csharp
public sealed class SandboxGuardTests
{
    private static readonly string[] Forbidden = [@"Z:\", @"C:\Users\JW\Documents\Johann"];

    [Fact]
    public void CleanSandbox_HasNoViolations()
    {
        var json = """{"quellverzeichnis":"C:\\Temp\\sb\\eingang","globalPromptFilePath":"C:\\Temp\\sb\\team\\prompts.json"}""";
        SandboxGuard.Violations(json, Forbidden).Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{"globalPromptFilePath":"Z:\\12_Tools\\Peano\\Johann\\prompts.json"}""")]
    [InlineData("""{"ausgabeverzeichnis":"C:\\Users\\JW\\Documents\\Johann\\output"}""")]
    [InlineData("""{"ausgabeverzeichnis":"c:/users/jw/documents/johann/output"}""")]
    [InlineData("""{"nested":{"x":["C:\\Users\\JW\\Documents\\Johann"]}}""")]
    [InlineData("""{"globalPromptFilePath":"z:/12_Tools/p.json"}""")]
    public void ForbiddenPath_AnywhereInJson_IsViolation(string json)
    {
        SandboxGuard.Violations(json, Forbidden).Should().NotBeEmpty();
    }

    [Fact]
    public void BrokenJson_IsViolation()
    {
        SandboxGuard.Violations("{kaputt", Forbidden).Should().ContainSingle().Which.Should().Contain("nicht lesbar");
    }
}

public sealed class AuditSandboxTests : IDisposable
{
    private readonly string tmp = Directory.CreateTempSubdirectory("audit-").FullName;

    public void Dispose() => Directory.Delete(this.tmp, recursive: true);

    [Fact]
    public void Create_CopiesAndRedirects_AndPassesGuard()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        File.WriteAllText(Path.Combine(realHome, "settings.json"),
            $$"""{"name":"JW","ausgabeverzeichnis":{{System.Text.Json.JsonSerializer.Serialize(Path.Combine(realHome, "output"))}},"globalPromptFilePath":"Z:\\12_Tools\\Peano\\Johann\\prompts.json"}""");
        File.WriteAllText(Path.Combine(realHome, "prompts.personal.json"), "{}");
        File.WriteAllText(Path.Combine(realHome, ".env"), "OPENAI_API_KEY=sk-x");
        Directory.CreateDirectory(Path.Combine(realHome, "output", "2026-09-23", "_raw"));
        var team = Path.Combine(this.tmp, "team.json");
        File.WriteAllText(team, "{}");

        var layout = AuditSandbox.Create(Path.Combine(this.tmp, "sb"), realHome, team);

        File.Exists(Path.Combine(layout.Home, ".env")).Should().BeTrue();
        File.Exists(layout.TeamPrompts).Should().BeTrue();
        Directory.Exists(Path.Combine(layout.Output, "2026-09-23", "_raw")).Should().BeTrue();
        SandboxGuard.Violations(File.ReadAllText(layout.SettingsFile), [@"Z:\", realHome]).Should().BeEmpty();
        File.ReadAllText(layout.SettingsFile).Should().Contain("\"name\": \"JW\"");   // Rest bleibt erhalten
    }

    [Fact]
    public void Create_RefusesExistingNonEmptyRoot()
    {
        var root = Directory.CreateDirectory(Path.Combine(this.tmp, "sb")).FullName;
        File.WriteAllText(Path.Combine(root, "x"), "x");
        var act = () => AuditSandbox.Create(root, this.tmp, null);
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Scheitern sehen** — Build-Fehler.

- [ ] **Step 3: Implementieren**

`SandboxLayout`:
```csharp
namespace Platee.Johann.UiDriver.Sandbox;

public sealed record SandboxLayout(string Root)
{
    public string Home => Path.Combine(this.Root, "home");
    public string Output => Path.Combine(this.Root, "output");
    public string Eingang => Path.Combine(this.Root, "eingang");
    public string Archiv => Path.Combine(this.Eingang, "Archiv");
    public string TeamPrompts => Path.Combine(this.Root, "team", "prompts.json");
    public string SettingsFile => Path.Combine(this.Home, "settings.json");
}
```

`SandboxGuard`:
```csharp
namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json;

public static class SandboxGuard
{
    public static IReadOnlyList<string> DefaultForbiddenRoots() =>
    [
        @"Z:\",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann"),
    ];

    public static IReadOnlyList<string> Violations(string settingsJson, IEnumerable<string> forbiddenRoots)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(settingsJson);
        }
        catch (JsonException ex)
        {
            return [$"settings.json nicht lesbar: {ex.Message}"];
        }

        using (doc)
        {
            var roots = forbiddenRoots.Select(Normalise).ToArray();
            return Strings(doc.RootElement)
                .SelectMany(s => roots
                    .Where(r => Normalise(s).Contains(r, StringComparison.OrdinalIgnoreCase))
                    .Select(r => $"'{s}' zeigt auf verbotenen Bereich '{r}'"))
                .ToArray();
        }
    }

    public static void Ensure(SandboxLayout layout)
    {
        var violations = Violations(File.ReadAllText(layout.SettingsFile), DefaultForbiddenRoots());
        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Sandbox-Wächter: Start verweigert." + Environment.NewLine + string.Join(Environment.NewLine, violations));
        }
    }

    private static string Normalise(string path) => path.Replace('/', '\\').TrimEnd('\\') + "\\";

    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };
}
```
(`Normalise` hängt `\` an beide Seiten, damit `Z:\` und `Z:\x` zusammenpassen und `C:\...\JohannX` nicht fälschlich trifft.)

`AuditSandbox.Create`: Wurzel muss fehlen oder leer sein (sonst `InvalidOperationException`); legt `home`, `eingang`, `eingang\Archiv`, `team` an; kopiert `settings.json`, `prompts.personal.json`, `.env` aus `realHome` (fehlende überspringen), `output\` rekursiv, `teamPromptFile` → `TeamPrompts` (fehlt es: `{}` schreiben). Dann `settings.json` per `JsonNode` umbiegen: `quellverzeichnis`, `archivverzeichnis`, `ausgabeverzeichnis`, `globalPromptFilePath` auf die Sandbox-Pfade; Rest unverändert; mit `WriteIndented = true` schreiben. Zum Schluss `SandboxGuard.Ensure(layout)`. **Prompt-Cache-Dateien nicht kopieren** — ihr Name hängt am Pfad der Team-Datei.

`TestSandbox.Create(root)`: legt dieselben Ordner an und schreibt eine frische `settings.json`:
```json
{ "name": "UI-Test", "firma": "Test GmbH",
  "quellverzeichnis": "<Eingang>", "archivverzeichnis": "<Archiv>", "ausgabeverzeichnis": "<Output>",
  "globalPromptFilePath": "", "sectionModesMigrationDone": true,
  "lastSeenReleaseNotesVersion": "<Version der EXE, vom Aufrufer gesetzt>", "summaryModel": "gpt-5.6-luna" }
```
Signatur daher: `TestSandbox.Create(string root, string? lastSeenReleaseNotesVersion)`; `null` lässt den Schlüssel weg (für den Test, der die Erststart-Neuigkeiten prüft). `.env` mit `OPENAI_API_KEY=sk-stub-not-a-real-key`. Danach `SandboxGuard.Ensure`.

Vor dem Schreiben prüfen: leerer `globalPromptFilePath` → `App` behandelt ihn als „kein Team-Pfad“ (`string.IsNullOrWhiteSpace`, Zeile 93) — also eingebaute Prompts. Das ist für Phase B gewollt.

- [ ] **Step 4: Tests grün** — `dotnet test Platee.Johann.Tests --filter "FullyQualifiedName~SandboxGuardTests|FullyQualifiedName~AuditSandboxTests"`.

- [ ] **Step 5: Commit** — `git commit -m "test: Sandbox mit Waechter fuer Automationslaeufe (#111)"`

### Task 6: `JohannSession` (FlaUI) + Konsolenwerkzeug `ui-driver`

**Files:**
- Create: `Platee.Johann.UiDriver/Automation/JohannLaunchOptions.cs`, `JohannSession.cs`, `UiTreeDump.cs`, `KeyChord.cs`
- Create: `Platee.Johann.UiDriver/Audio/SilenceMp3.cs`
- Create: `tools/ui-driver/UiDriverTool.csproj`, `tools/ui-driver/Program.cs`, `tools/ui-driver/Commands.cs`
- Modify: `Platee.Johann.slnx`
- Test: `Platee.Johann.Tests/Unit/KeyChordTests.cs`, `Platee.Johann.Tests/Unit/UiTreeDumpTests.cs` (nur die reine JSON-Formung)

**Interfaces:**
- Consumes: `SandboxLayout`, `SandboxGuard.Ensure` (Task 5)
- Produces:
  - `sealed record JohannLaunchOptions(string ExePath, SandboxLayout Sandbox, Uri? OpenAiRoot, string? ApiKey, bool SkipUpdateCheck = true)`
  - `sealed class JohannSession : IDisposable`:
    - `static JohannSession Launch(JohannLaunchOptions options, TimeSpan? timeout = null)` — wirft, wenn schon ein `Platee.Johann.UI` läuft oder der Wächter anschlägt; wartet auf das Hauptfenster; meldet jedes andere Top-Level-Fenster als `UnexpectedWindowException(title, screenshotPath)`
    - `static JohannSession Attach(int processId)`
    - `int ProcessId`, `Window MainWindow`, `IReadOnlyList<Window> Windows()`
    - `AutomationElement Find(string idOrName, TimeSpan? timeout = null, Window? scope = null)` — erst `AutomationId`, dann `Name`, über alle Fenster des Prozesses
    - `AutomationElement? TryFind(string idOrName, TimeSpan? timeout = null)`
    - `void Click(string idOrName, bool mouse = false)`, `void RightClick(string idOrName)`, `void DoubleClick(string idOrName)`
    - `void Type(string idOrName, string text)`, `void Key(string chord)`
    - `string Screenshot(string path, string? idOrName = null)`
    - `string Tree(string? idOrName = null, int depth = 8)`
    - `static string ReadClipboard()`
    - `void Close()`
  - `static class KeyChord { static VirtualKeyShort[] Parse(string chord); }` — `"Ctrl+Plus"`, `"Ctrl+0"`, `"Delete"`, `"Tab"`, `"Shift+Tab"`, `"Enter"`, `"Escape"`
  - `static class SilenceMp3 { static void Write(string path, TimeSpan duration); }`

- [ ] **Step 1: Failing tests für die reinen Teile**

```csharp
public sealed class KeyChordTests
{
    [Theory]
    [InlineData("Delete", new[] { VirtualKeyShort.DELETE })]
    [InlineData("Ctrl+Plus", new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.ADD })]
    [InlineData("ctrl+0", new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_0 })]
    [InlineData("Shift+Tab", new[] { VirtualKeyShort.SHIFT, VirtualKeyShort.TAB })]
    public void Parse(string chord, VirtualKeyShort[] expected) =>
        KeyChord.Parse(chord).Should().Equal(expected);

    [Fact]
    public void Parse_Unknown_ThrowsNamingKey() =>
        FluentActions.Invoking(() => KeyChord.Parse("Ctrl+Wurst")).Should().Throw<ArgumentException>().WithMessage("*Wurst*");
}
```
`UiTreeDumpTests`: `UiTreeDump.ToJson(IEnumerable<UiNode>)` mit `sealed record UiNode(string ControlType, string? AutomationId, string? Name, bool IsEnabled, bool IsOffscreen, string Bounds, IReadOnlyList<UiNode> Children)` → erwartet eingerücktes JSON, `null`-Felder weggelassen, Umlaute unverändert (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`).

- [ ] **Step 2: Scheitern sehen, implementieren, grün** (`KeyChord`: Wörterbuch Name→`VirtualKeyShort`, Ziffern und Buchstaben generisch; `UiTreeDump`: `System.Text.Json`).

- [ ] **Step 3: `JohannSession` implementieren**

Kern:
```csharp
public static JohannSession Launch(JohannLaunchOptions o, TimeSpan? timeout = null)
{
    var running = Process.GetProcessesByName("Platee.Johann.UI");
    if (running.Length > 0)
    {
        throw new InvalidOperationException($"Johann läuft bereits (PID {running[0].Id}) — bitte schließen.");
    }

    SandboxGuard.Ensure(o.Sandbox);

    var psi = new ProcessStartInfo(o.ExePath) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(o.ExePath)! };
    psi.Environment["JOHANN_HOME"] = o.Sandbox.Home;
    psi.Environment["JOHANN_NO_UPDATE_CHECK"] = o.SkipUpdateCheck ? "1" : "0";
    if (o.OpenAiRoot is not null)
    {
        psi.Environment["JOHANN_OPENAI_ENDPOINT"] = o.OpenAiRoot.AbsoluteUri;
    }

    if (o.ApiKey is not null)
    {
        psi.Environment["OPENAI_API_KEY"] = o.ApiKey;
    }
    else
    {
        psi.Environment.Remove("OPENAI_API_KEY");   // Audit: Schlüssel aus der Sandbox-.env
    }

    var app = FlaUI.Core.Application.Launch(psi);
    var session = new JohannSession(app);
    session.WaitForMainWindow(timeout ?? TimeSpan.FromSeconds(30));
    return session;
}
```
`WaitForMainWindow`: pollt `app.GetAllTopLevelWindows(automation)` alle 250 ms; erscheint ein Fenster mit Titel ≠ Hauptfenster-Titel (Titel des Hauptfensters aus `MainWindow.xaml` `Title=` ablesen), wird es fotografiert (`%TEMP%\johann-ui-driver\unexpected-<zeit>.png`) und `UnexpectedWindowException` geworfen. Für Tests, die Dialoge erwarten, `Launch(..., expectDialogs: true)` als zusätzlicher Parameter, der nur auf irgendein Fenster wartet.

`Find`: `Retry.WhileNull(() => Windows().Select(w => w.FindFirstDescendant(cf => cf.ByAutomationId(key).Or(cf.ByName(key)))).FirstOrDefault(e => e is not null), timeout ?? 5 s)`; bei `null` → `ElementNotFoundException($"Element '{key}' nicht gefunden", Tree())`.
`Click`: `mouse || !el.Patterns.Invoke.IsSupported` → `el.Click()`, sonst `el.Patterns.Invoke.Pattern.Invoke()`. Für Knöpfe, die modale Dialoge öffnen, ruft der Aufrufer `mouse: true` (Invoke kann bei modalen WPF-Dialogen blockieren).
`Type`: `el.AsTextBox().Text = text` wenn `Value`-Muster, sonst Fokus + `Keyboard.Type(text)`.
`ReadClipboard`: STA-Thread mit `System.Windows.Forms.Clipboard.GetText()`.
`Close`: `MainWindow.Close()`, 10 s warten, sonst `app.Kill()`. `Dispose` ruft `Close` und entsorgt `UIA3Automation`.

`SilenceMp3.Write`: `MediaFoundationApi.Startup()`; `SilenceProvider(new WaveFormat(44100, 16, 2)).ToWaveProvider()` begrenzt über `.Take(duration)` auf `ISampleProvider`-Ebene → `MediaFoundationEncoder.EncodeToMp3(provider, path, 128000)`. 30 min bei 128 kbit/s ≈ 28,8 MB > 25 MB.

- [ ] **Step 4: Konsolenwerkzeug**

`tools/ui-driver/UiDriverTool.csproj`: `OutputType Exe`, `net10.0-windows`, `AssemblyName ui-driver`, ProjectReference auf UiDriver. In `Platee.Johann.slnx` aufnehmen.

Befehle (`Program.cs` → `Commands.Run(args)`, Ausgabe immer JSON auf stdout, Fehler als `{"error": "..."}` + Exit-Code 1):

| Befehl | Wirkung |
|---|---|
| `sandbox new --root <d> [--from <realHome>] [--team <file>]` | `AuditSandbox.Create` (Default `--from` = echtes `Documents\Johann`, `--team` = `Z:\…\prompts.json` **nur lesend**) |
| `sandbox check --root <d>` | `SandboxGuard.Ensure` |
| `start --root <d> [--endpoint <url>] [--exe <path>]` | `JohannSession.Launch`, PID nach `%TEMP%\johann-ui-driver\session.json` |
| `windows` | Titel aller Fenster |
| `tree [--of <id\|name>] [--depth n]` | UI-Baum |
| `click <id\|name> [--mouse]` · `rightclick` · `doubleclick` | Bedienen |
| `type <id\|name> <text>` · `key <chord>` | Eingabe |
| `screenshot <file> [--of <id\|name>]` | PNG |
| `wait-for <id\|name> [--timeout s]` | warten |
| `clipboard` | Text der Zwischenablage |
| `close` | Johann beenden |
| `silence --minutes n <out.mp3>` | D8 erzeugen |

Alle Befehle außer `sandbox`/`silence`/`start` hängen sich per `JohannSession.Attach(pid aus session.json)` an.

- [ ] **Step 5: Rauchtest von Hand (belegt Review-Focus 5)**

```powershell
dotnet build
$sb = "$env:TEMP\johann-smoke"; Remove-Item $sb -Recurse -Force -ErrorAction SilentlyContinue
dotnet run --project tools/ui-driver -- sandbox new --root $sb --from "$env:USERPROFILE\Documents\Johann" --team "Z:\12_Tools\Peano\Johann\prompts.json"
dotnet run --project tools/ui-driver -- start --root $sb
dotnet run --project tools/ui-driver -- screenshot "$sb\main.png"
dotnet run --project tools/ui-driver -- start --root $sb      # zweiter Start
dotnet run --project tools/ui-driver -- close
```
Expected: `main.png` zeigt Johann mit den Tagen aus der Kopie; der zweite `start` endet mit `{"error":"Johann läuft bereits (PID …)"}`; `Documents\Johann\settings.json` ist unverändert (`Get-FileHash` vorher/nachher vergleichen).

- [ ] **Step 6: Commit** — `git commit -m "test: JohannSession (FlaUI) und Konsolenwerkzeug ui-driver (#111)"`

### Task 7: Fixture-Diktate

**Files:**
- Create: `tests/fixtures/dictations/D1-aufgaben.txt` … `D6-korrekturliste.txt`
- Create: `scripts/new-dictation-fixtures.ps1`
- Output (committet): `tests/fixtures/dictations/*.mp3`

- [ ] **Step 1: Texte schreiben** (gesprochen, keine Markdown-Zeichen)

`D1-aufgaben.txt`:
> Projekt Neubau Kita Sonnenschein. Ich habe heute mit der Bauleitung gesprochen, folgende Punkte sind offen. Erstens die Fenster im Obergeschoss: Aufmaß prüfen, Angebot bei Firma Keller einholen, und bis Freitag an die Bauherrin schicken. Zweitens die Elektroplanung: Steckdosen im Gruppenraum nachzählen, dabei die Höhe für Kindersicherung beachten, und die Änderungen an Herrn Brandt weitergeben. Drittens Termin für die Abnahme des Dachs mit dem Sachverständigen vereinbaren. Wichtig ist, dass das Angebot für die Fenster wirklich bis Freitag rausgeht.

`D2-telefonat.txt`:
> Telefonat mit Frau Neele Hartmann von der Stadtverwaltung, heute um zehn Uhr. Sie hat angerufen wegen der Baugenehmigung für das Projekt Lindenstraße. Frau Hartmann sagt, dass noch der Nachweis zum Brandschutz fehlt. Ich habe ihr erklärt, dass unser Gutachter den Bericht nächste Woche liefert. Wir haben vereinbart, dass ich ihr den Bericht bis zum Dreißigsten per Mail schicke und sie dann innerhalb von zwei Wochen entscheidet. Sie hat außerdem gefragt, ob die Stellplätze wie geplant bleiben — das habe ich bestätigt.

`D3-stundenzettel.txt`:
> Zeiterfassung für heute, Projekt Rathaus Umbau. Von acht bis zehn Uhr Planbesprechung im Büro, zwei Stunden. Dann von zehn Uhr dreißig bis zwölf Uhr Baustellenbegehung mit dem Statiker, eineinhalb Stunden. Am Nachmittag drei Stunden Ausschreibung Trockenbau geschrieben. Insgesamt also sechseinhalb Stunden.

`D4-email.txt`:
> Mail an Thomas Berger von der Firma Holzbau Berger. Hallo Thomas, danke dir für das Angebot vom Montag. Wir würden gern den Auftrag für die Dachkonstruktion vergeben, brauchen aber noch zwei Dinge von dir: einen verbindlichen Liefertermin für das Brettschichtholz und eine Bestätigung, dass der Preis bis Ende Oktober gilt. Kannst du mir das bis Mittwoch schicken? Viele Grüße.

`D5-englisch.txt`:
> Quick note after the call with the client from Rotterdam. They want the revised floor plans by next Tuesday. The main change is the second staircase on the east side. We also need to check whether the fire exit width still meets the regulations. I will ask our engineer to confirm the structural impact.

`D6-korrekturliste.txt`:
> Kurze Notiz für Piano intern. Ich habe mit Nele gesprochen, wir wollen im Team JGPT stärker für Protokolle nutzen. Nele bereitet dazu bis nächste Woche eine kurze Anleitung vor.

(„Piano“, „Nele“, „JGPT“ stehen in der Standard-Korrekturliste → erwartet „Peano“, „Neele“, „ChatGPT“.)

- [ ] **Step 2: Skript**

```powershell
# scripts/new-dictation-fixtures.ps1 — erzeugt D1..D6 als MP3 per OpenAI-TTS (#111)
param(
    [string]$Model = 'gpt-4o-mini-tts',
    [string]$Voice = 'onyx',
    [string]$EnvFile = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Johann\.env')
)
$ErrorActionPreference = 'Stop'
$key = $env:OPENAI_API_KEY
if (-not $key) { $key = (Get-Content $EnvFile | Where-Object { $_ -match '^OPENAI_API_KEY=' }) -replace '^OPENAI_API_KEY=', '' -replace '["'']', '' }
if (-not $key) { throw "Kein OPENAI_API_KEY gefunden." }
$dir = Join-Path $PSScriptRoot '..\tests\fixtures\dictations'
Get-ChildItem $dir -Filter 'D*.txt' | ForEach-Object {
    $out = [IO.Path]::ChangeExtension($_.FullName, '.mp3')
    $body = @{ model = $Model; voice = $Voice; input = (Get-Content $_.FullName -Raw); response_format = 'mp3' } | ConvertTo-Json
    Invoke-RestMethod -Uri 'https://api.openai.com/v1/audio/speech' -Method Post `
        -Headers @{ Authorization = "Bearer $key" } -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes($body)) -OutFile $out
    Write-Host "$($_.Name) -> $([Math]::Round((Get-Item $out).Length / 1KB)) KB"
}
```
Falls das TTS-Modell abgelehnt wird: Modellliste per `GET /v1/models` prüfen und `-Model` setzen; Rückfall Windows SAPI (`System.Speech.Synthesis.SpeechSynthesizer` mit deutscher Stimme → WAV → `ui-driver` bekommt dafür keinen eigenen Befehl, sondern NAudio `MediaFoundationEncoder` in einem Einzeiler-Skript).

- [ ] **Step 3: Ausführen** — `pwsh scripts/new-dictation-fixtures.ps1`. Expected: sechs MP3s, je 20–60 s, zusammen < 3 MB. Einmal anhören (D1 und D5).

- [ ] **Step 4: Commit** — `git add tests/fixtures/dictations scripts/new-dictation-fixtures.ps1` → `git commit -m "test: Fixture-Diktate D1-D6 per TTS (#111)"`

D7 (echte, fast stille Aufnahme) und D8 (> 25 MB) werden **nicht** committet — D7 wählt der Audit aus der Sandbox-Kopie, D8 erzeugt `ui-driver silence --minutes 30`.

### Task 8: Audit durchführen

**Files:**
- Create: `docs/ui-automation/audit-runbook.md`
- Create: `docs/audit/2026-09-<tag>-v1.5.0.md`, `docs/audit/<lauf>/*.png`
- Create: `Platee.Johann.UiTests/Fixtures/D1..D6_status.json` (Kopien der im Audit erzeugten `_status.json`, Grundlage für Phase B)

Dies ist eine Ausführungs-Aufgabe, keine TDD-Aufgabe. Sie läuft **nacheinander** auf einem Desktop; der Nutzer bedient den PC in der Zeit nicht.

- [ ] **Step 1: Runbook schreiben** — je Flow 1–9 aus der Spec die konkreten `ui-driver`-Schritte, das erwartete Ergebnis und die Soll-Quelle (Datei + Abschnitt). Kopfzeile: Sicherheitsregeln aus „Global Constraints“.

- [ ] **Step 2: Sandbox + Start**

```powershell
dotnet build
$run = "audit-$(Get-Date -Format yyyyMMdd-HHmm)"; $sb = "$env:TEMP\johann-audit\$run"
dotnet run --project tools/ui-driver -- sandbox new --root $sb
Copy-Item tests/fixtures/dictations/*.mp3 "$sb\eingang-wartend\"   # erst in Flow 2 in eingang\ schieben
dotnet run --project tools/ui-driver -- silence --minutes 30 "$sb\eingang-wartend\D8-gross.mp3"
dotnet run --project tools/ui-driver -- start --root $sb
```

- [ ] **Step 3: Flows 1–6, 8, 9 abarbeiten** — je Schritt `screenshot docs/audit/$run/<flow>-<nr>-<wort>.png` vor/nach; bei Abweichung Befund notieren (Flow, Soll + Quelle, Ist, Screenshot, Schwere, Reproduktion). Flow 1 enthält einen zweiten, kurzen Start **ohne** `.env` in einer eigenen Sandbox (`Remove-Item $sb2\home\.env`).

- [ ] **Step 4: Flow 7 (Mail) beaufsichtigt** — Nutzer vorher fragen; „Aufgaben“ und „E-Mail“ an D1/D4; Nutzer bestätigt, was im Entwurf steht; Entwürfe danach verwerfen.

- [ ] **Step 5: Screenshots parallel auswerten** — Agenten ohne Desktop-Zugriff prüfen Screenshot-Gruppen gegen Runbook-Soll (Kontrast, abgeschnittene Texte, Ausrichtung, Rohes Markdown), jeweils mit Quelle.

- [ ] **Step 6: Zweiter Durchgang** — jeden Befund in der laufenden App erneut reproduzieren; nicht reproduzierbare streichen oder als „nicht reproduziert“ markieren.

- [ ] **Step 7: Fixtures sichern** — die `_status.json` der D1–D6-Einträge aus `$sb\output\<tag>\_raw\` nach `Platee.Johann.UiTests/Fixtures/D<n>_status.json` kopieren (für den Stub in Phase B). Enthalten nur Testtexte, keine echten Diktate.

- [ ] **Step 8: Bericht + Commit** — Bericht mit Übersichtstabelle (Schwere × Flow) und den Einzelbefunden; `git add docs/ui-automation/audit-runbook.md docs/audit Platee.Johann.UiTests/Fixtures` → `git commit -m "docs: Audit v1.5.0 der laufenden App (#111)"`. **Nutzer-Review abwarten**, bestätigte Befunde erst auf Zuruf als Issues anlegen.

- [ ] **Step 9: Aufräumen** — `ui-driver close`; Sandbox löschen, sofern kein Befund sie zur Analyse braucht.

---

## Phase B — Dauerhafte Suite

### Task 9: AutomationIds

**Files:**
- Modify: `Platee.Johann.UI/MainWindow.xaml`, `Views/SettingsView.xaml`, `Views/SectionModeMigrationDialog.xaml`, `Views/ReleaseNotesWindow.xaml`, `Views/ToastView.xaml`
- Test: `Platee.Johann.Tests/Unit/AutomationIdTests.cs`

**Interfaces:**
- Produces: stabile Ids (PascalCase, Präfix je Bereich), u. a.:
  `Main.Settings`, `Main.ReleaseNotes`, `Main.Handbook`, `Main.StatusLog`, `Dates.List`, `Dates.ShowAll`, `Entries.List`, `Entries.SortById`, `Entries.SortByProject`, `Entries.OnlyOpen`, `Entries.Dictate`, `Entries.StopDictation`, `Entries.Delete`, `Detail.ToggleDone`, `Detail.Delete`, `Detail.Pdf`, `Detail.Html`, `Detail.TaskMail`, `Detail.Email`, `Detail.Copy`, `Detail.Reprocess`, `Detail.EditTranscript`, `Detail.RegenerateFromTranscript`, `Detail.CancelEditTranscript`, `Detail.TranscriptEditor`, `Detail.ZoomIn`, `Detail.ZoomOut`, `Detail.ZoomText`, `Sections.<Id>` je Häkchen (`Sections.ShowTaskList` …), `Sections.Reset`,
  `Settings.Save`, `Settings.Reset`, `Settings.SaveTarget` (ComboBox „Speichern nach:“, Items `Settings.SaveTarget.Personal`/`.Global`), `Settings.CategoryName`, `Settings.CategoryPrompt`, `Settings.Status`, `Settings.AddCorrection`, `Settings.AddCategory`, `Settings.RemoveCategory`, `Settings.ModelPicker`, `Settings.ModelCost`, `Settings.Section.<Name>`, `Migration.UseRecommended`, `Migration.KeepCurrent`, `ReleaseNotes.Close`, `Toast.Item`, `Toast.Details`.
  Abschnitts-Kopiersymbole: `AutomationProperties.AutomationId="{Binding SectionId, StringFormat=Copy.{0}}"` im Template.

- [ ] **Step 1: Failing test** — parst die XAML-Dateien mit `XDocument` (wie `EntryListLayoutTests`) und prüft: (a) jedes `Button`, `CheckBox`, `ComboBox`, `TextBox`, `ListBox` mit `Command`- oder `IsChecked`-/`SelectedItem`-/`Text`-Bindung trägt `AutomationProperties.AutomationId`; (b) keine Id doppelt je Datei; (c) die Liste oben ist vollständig vorhanden.

- [ ] **Step 2: Scheitern sehen**, **Step 3: Ids setzen** (nur das Attribut, keine anderen Änderungen), **Step 4: grün + `dotnet build`**.

- [ ] **Step 5: Sichtprüfung** — App starten (Sandbox via `ui-driver start`), Screenshot vorher/nachher vergleichen: identisch.

- [ ] **Step 6: Commit** — `git commit -m "test: AutomationIds an allen bedienbaren Elementen (#111)"`

### Task 10: `Platee.Johann.UiTests` — Gerüst, Kontext, Rauchtest, Skript

**Files:**
- Create: `Platee.Johann.UiTests/Platee.Johann.UiTests.csproj`, `ExeLocator.cs`, `UiTestContext.cs`, `UiCollection.cs`, `SmokeTests.cs`
- Create: `scripts/run-ui-tests.ps1`
- Modify: `Platee.Johann.slnx`

**Interfaces:**
- Consumes: `JohannSession`, `TestSandbox`, `OpenAiStubServer`, `EntryFixture` (Tasks 3–6), Fixtures aus Task 8
- Produces:
  - `sealed class UiTestContext : IDisposable` mit `static UiTestContext Start(bool firstRun = false, Action<OpenAiStubServer>? stub = null, Func<JsonObject, JsonObject>? adjustSettings = null, bool teamFile = false, bool keepSandbox = false)` — `firstRun` lässt `lastSeenReleaseNotesVersion` weg; `adjustSettings` ändert die frische `settings.json` vor dem Start; `teamFile` legt `Sandbox.TeamPrompts` mit den eingebauten Prompts an und setzt `globalPromptFilePath` darauf; `keepSandbox` löscht die Sandbox beim `Dispose` nicht, `JohannSession App`, `OpenAiStubServer Stub`, `SandboxLayout Sandbox`, `Task<string> DropDictationAsync(string fixture /* "D1" */, TimeSpan? timeout = null)` (legt MP3 in `eingang\`, wartet, bis ein Listeneintrag mit dem Fixture-Titel erscheint, gibt den Titel zurück), `void KeepOnFailure()`
  - Standard-Stub: Transkription je Dateiname aus `D<n>_status.json`; Chat: erkennt den Abschnitt am Präfix des Prompts (Text von `SummaryPrompts.<X>` bis zum ersten `{`), Titel am Präfix „Bitte formuliere einen sehr kurzen, prägnanten Titel“, und antwortet mit dem Fixture-Text des Diktats, dessen Transkript im Anfragetext steht

- [ ] **Step 1: Projekt**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <!-- Nie Teil von "dotnet test" der Lösung oder des Pre-Push-Hooks: belegt den Desktop (#111). -->
    <IsTestProject Condition="'$(RunUiTests)' != 'true'">false</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Platee.Johann.UiDriver\Platee.Johann.UiDriver.csproj" />
    <ProjectReference Include="..\Platee.Johann.Application\Platee.Johann.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />
    <None Include="..\tests\fixtures\dictations\*.mp3" LinkBase="Fixtures" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```
`UiCollection.cs`: `[CollectionDefinition("Desktop", DisableParallelization = true)]` — alle UI-Tests in diese Collection (ein Desktop, ein Johann).

- [ ] **Step 2: Rauchtests (failing)**

```csharp
[Collection("Desktop")]
public sealed class SmokeTests
{
    [Fact]
    public void Starts_WithoutDialogs_AndShowsEmptyLists()
    {
        using var ctx = UiTestContext.Start();
        ctx.App.Find("Entries.Dictate").IsEnabled.Should().BeTrue();
        ctx.Stub.Requests.Should().OnlyContain(r => r.Path.StartsWith("/v1/models/"));
    }

    [Fact]
    public void FirstRun_ShowsReleaseNotes()
    {
        using var ctx = UiTestContext.Start(firstRun: true);
        ctx.App.Windows().Should().Contain(w => w.Title.Contains("Neuigkeiten") || w.Title.Contains("Was ist neu"));
    }

    [Fact]
    public void RefusesToStart_WhenJohannAlreadyRuns()
    {
        using var first = UiTestContext.Start();
        var act = () => UiTestContext.Start();
        act.Should().Throw<InvalidOperationException>().WithMessage("*läuft bereits*");
    }

    [Fact]
    public void UnexpectedDialog_IsReported_NotWaitedOut()
    {
        // Ausgabeordner auf ein nicht anlegbares Ziel → Pfadwarnung beim Start
        var act = () => UiTestContext.Start(adjustSettings: json =>
        {
            json["ausgabeverzeichnis"] = @"Q:\gibt-es-nicht\output";
            return json;
        });
        act.Should().Throw<UnexpectedWindowException>().Which.Title.Should().Contain("Verzeichnisse");
    }
}
```
(Der letzte Test nutzt `adjustSettings`; dafür `TestSandbox.Create` um einen Parameter `Func<JsonObject, JsonObject>? adjust` erweitern, der vor dem Schreiben angewendet wird — der Wächter prüft danach wie immer. Den Fenstertitel des Release-Notes-Fensters vorher in `ReleaseNotesWindow.xaml` ablesen und den Test darauf festlegen.)

- [ ] **Step 3: `ExeLocator` + `UiTestContext` implementieren** — `ExeLocator.Find()`: vom Testausgabeordner aufwärts bis `Platee.Johann.slnx`, dann `Platee.Johann.UI/bin/<Configuration>/net10.0-windows/Platee.Johann.UI.exe`; fehlt sie → Meldung „erst `dotnet build` ausführen“. `UiTestContext.Start`: Sandbox unter `%TEMP%\johann-ui\<guid>`, Stub starten + Standardregeln, `JohannSession.Launch(new JohannLaunchOptions(exe, sandbox, stub.Root, "sk-stub-not-a-real-key"))`. `Dispose`: bei fehlgeschlagenem Test (xUnit 2: über `KeepOnFailure()` vom Test selbst oder immer Screenshot + Baum nach `TestResults/ui/<test>/` schreiben, wenn `Environment.GetEnvironmentVariable("JOHANN_UI_KEEP")` gesetzt ist) Artefakte sichern; Johann schließen; Stub beenden; Sandbox löschen.

- [ ] **Step 4: Skript**

```powershell
# scripts/run-ui-tests.ps1 — Phase-B-Suite gegen die echte EXE (#111)
param([string]$Filter)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Platee.Johann.UI' -ErrorAction SilentlyContinue) { throw 'Johann läuft – bitte schließen.' }
dotnet build (Join-Path $PSScriptRoot '..\Platee.Johann.slnx')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$args = @((Join-Path $PSScriptRoot '..\Platee.Johann.UiTests'), '--no-build', '-p:RunUiTests=true', '--blame-hang-timeout', '3m', '--results-directory', 'TestResults')
if ($Filter) { $args += @('--filter', $Filter) }
$env:JOHANN_UI_KEEP = '1'
dotnet test @args
exit $LASTEXITCODE
```
Achtung: `--no-build` + `-p:RunUiTests=true` — prüfen, dass `IsTestProject` zur Laufzeit von `dotnet test` ausgewertet wird; sonst im Skript ohne `--no-build` bauen.

- [ ] **Step 5: grün** — `pwsh scripts/run-ui-tests.ps1 -Filter "FullyQualifiedName~SmokeTests"`; außerdem `dotnet test` (Lösung) → UiTests werden **nicht** ausgeführt (Ausgabe prüfen).

- [ ] **Step 6: Commit** — `git commit -m "test: UiTests-Geruest mit Stub, Sandbox und Rauchtests (#111)"`

### Task 11: Flows Liste und Löschen

**Files:** Create `Platee.Johann.UiTests/EntryListFlowTests.cs`

- [ ] **Step 1: Tests schreiben**

```csharp
[Collection("Desktop")]
public sealed class EntryListFlowTests
{
    [Fact]
    public async Task WatchFolder_NewEntryAppears_InSortOrder()
    {
        using var ctx = UiTestContext.Start();
        await ctx.DropDictationAsync("D1");
        await ctx.DropDictationAsync("D3");
        ctx.App.Click("Entries.SortById");
        ctx.EntryTitles().Should().HaveCount(2);            // Hilfsmethode liest Titel aus Entries.List
        ctx.EntryNumbers().Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task MarkDone_KeepsSelection_AndOnlyOpenRemovesRow()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");
        var t3 = await ctx.DropDictationAsync("D3");
        ctx.SelectEntry(t1);
        ctx.App.Click("Detail.ToggleDone");
        ctx.SelectedEntryTitle().Should().Be(t1);             // #100
        ctx.App.Click("Entries.OnlyOpen");
        ctx.EntryTitles().Should().Equal(t3);
        ctx.SelectedEntryTitle().Should().Be(t3);             // nächste Zeile rückt nach
    }

    [Fact]
    public async Task Delete_WithKey_ConfirmNo_KeepsEntry()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");
        ctx.SelectEntry(t1);
        ctx.App.Key("Delete");
        ctx.App.Click("Nein", mouse: true);                   // „Nein“ ist vorausgewählt (#55)
        ctx.EntryTitles().Should().Contain(t1);
    }

    [Fact]
    public async Task Delete_MovesFilesToTrash_AndDayDisappears()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");
        ctx.SelectEntry(t1);
        ctx.App.Click("Detail.Delete", mouse: true);
        ctx.App.Click("Ja", mouse: true);
        ctx.EntryTitles().Should().BeEmpty();
        ctx.DateItems().Should().BeEmpty();                   // Tag ohne Einträge verschwindet
        Directory.EnumerateFiles(Path.Combine(ctx.Sandbox.Output, "_Papierkorb"), "geloescht.json", SearchOption.AllDirectories)
            .Should().ContainSingle();
    }
}
```
Hilfsmethoden `EntryTitles()`, `EntryNumbers()`, `SelectEntry(title)`, `SelectedEntryTitle()`, `DateItems()` in `UiTestContext` ergänzen (lesen `ListItem`-Kinder von `Entries.List`/`Dates.List`; Titel über den `Name` des Titel-`Text`-Elements im Item — Struktur einmal mit `ui-driver tree --of Entries.List` ansehen und festhalten).

- [ ] **Step 2: laufen lassen** — `pwsh scripts/run-ui-tests.ps1 -Filter "FullyQualifiedName~EntryListFlowTests"`. Jeder Test muss grün werden **oder** einen echten Befund belegen — dann Befund im Audit-Bericht ergänzen und Test mit `Skip = "Befund #<nr>"` markieren, bis er behoben ist.

- [ ] **Step 3: Commit** — `git commit -m "test: UI-Flows Liste und Loeschen (#111)"`

### Task 12: Flows Detail, Kopieren, Transkript, PDF

**Files:** Create `Platee.Johann.UiTests/DetailFlowTests.cs`

- [ ] **Step 1: Tests**

```csharp
[Collection("Desktop")]
public sealed class DetailFlowTests
{
    [Fact]
    public async Task HidingSection_RemovesItFromCopy()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));
        ctx.App.Click("Detail.Copy");
        JohannSession.ReadClipboard().Should().Contain("Aufgaben");
        ctx.App.Click("Sections.ShowTaskList");
        ctx.App.Click("Detail.Copy");
        JohannSession.ReadClipboard().Should().NotContain("Aufgaben").And.NotContain("**");
    }

    [Fact]
    public async Task SectionCopySymbol_CopiesOnlyThatSection()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));
        ctx.App.Click("Copy.builtin.taskList");
        var text = JohannSession.ReadClipboard();
        text.Should().StartWith("Aufgaben");
        text.Should().NotContain("Transkript");
    }

    [Fact]
    public async Task EditTranscript_Regenerate_SendsEditedText()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D6"));
        ctx.App.Click("Detail.EditTranscript");
        ctx.App.Type("Detail.TranscriptEditor", "Korrigierter Text über Peano.");
        ctx.App.Click("Detail.RegenerateFromTranscript");
        await ctx.WaitUntilIdleAsync();
        ctx.Stub.Requests.Where(r => r.Path == "/v1/chat/completions")
            .Should().Contain(r => r.Body.Contains("Korrigierter Text über Peano."));
    }

    [Fact]
    public async Task OnDemandSection_IsGeneratedOnClick()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D3"));
        var before = ctx.Stub.Requests.Count;
        ctx.App.Click("Generate.builtin.stundenzettel");
        await ctx.WaitUntilIdleAsync();
        ctx.Stub.Requests.Count.Should().Be(before + 1);
        ctx.App.Find("Section.builtin.stundenzettel").Should().NotBeNull();
    }

    [Fact]
    public async Task Pdf_ContainsSections_WithoutMarkdownStars()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));
        ctx.App.Click("Detail.Pdf");
        var pdf = await ctx.WaitForFileAsync(ctx.Sandbox.Output, "*.pdf");
        var text = PdfText.Extract(pdf);                     // PdfPig im UiTests-Projekt
        text.Should().Contain("Aufgaben").And.NotContain("**");
    }

    [Fact]
    public async Task Zoom_KeysChangeZoomText()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));
        ctx.App.Key("Ctrl+Plus");
        ctx.App.Find("Detail.ZoomText").Name.Should().Be("110 %");
        ctx.App.Key("Ctrl+0");
        ctx.App.Find("Detail.ZoomText").Name.Should().Be("100 %");
    }
}
```
Ergänzungen: `WaitUntilIdleAsync()` (wartet, bis `Detail.Reprocess` wieder aktiviert ist), `WaitForFileAsync(dir, pattern)`, `PdfText.Extract` (Paket `UglyToad.PdfPig` 1.7.x im UiTests-Projekt). Die Ids `Generate.<Id>` und `Section.<Id>` in Task 9 mit aufnehmen, falls noch nicht geschehen; das exakte Zoom-Textformat vorher in `EntryDetailViewModel.ZoomText` nachsehen.

- [ ] **Step 2: laufen lassen** (wie Task 11), **Step 3: Commit** — `git commit -m "test: UI-Flows Detail, Kopieren, Transkript, PDF (#111)"`

### Task 13: Flows Einstellungen, Fehler, Neuigkeiten, Tastatur

**Files:** Create `Platee.Johann.UiTests/SettingsFlowTests.cs`, `ErrorFlowTests.cs`, `KeyboardFlowTests.cs`

- [ ] **Step 1: Tests**

```csharp
[Collection("Desktop")]
public sealed class SettingsFlowTests
{
    [Fact]
    public void PersonalCategory_SurvivesRestart()
    {
        string sandboxRoot;
        using (var ctx = UiTestContext.Start(keepSandbox: true))
        {
            sandboxRoot = ctx.Sandbox.Root;
            ctx.App.Click("Main.Settings", mouse: true);
            ctx.App.Click("Settings.Section.Vorlagen");
            ctx.App.Click("Settings.AddCategory");
            ctx.App.Type("Settings.CategoryName", "Baustellenbericht");
            ctx.App.Type("Settings.CategoryPrompt", "Fasse den Baustellenstand zusammen: {transcript}");
            ctx.App.Click("Settings.Save");
        }

        File.ReadAllText(Path.Combine(sandboxRoot, "home", "prompts.personal.json")).Should().Contain("Baustellenbericht");
    }

    [Fact]
    public void MissingModel_BlocksSave_NetworkErrorDoesNot()
    {
        using var ctx = UiTestContext.Start(stub: s => s.MissingModel("gpt-5.6-sol"));
        ctx.App.Click("Main.Settings", mouse: true);
        ctx.App.Click("Settings.Section.KI-Modell");
        ctx.SelectModel("gpt-5.6-sol");
        ctx.App.Find("Settings.Save").IsEnabled.Should().BeFalse();
        ctx.Stub.FailNext("/v1/models/", 500);
        ctx.SelectModel("gpt-5.6-terra");
        ctx.App.Find("Settings.Save").IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void GlobalSaveTarget_WritesOnlyTheSandboxTeamFile()
    {
        using var ctx = UiTestContext.Start(teamFile: true);
        ctx.App.Click("Main.Settings", mouse: true);
        ctx.App.Click("Settings.Section.Vorlagen");
        ctx.App.Click("Settings.SaveTarget", mouse: true);
        ctx.App.Click("Settings.SaveTarget.Global", mouse: true);
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Teamvorlage Test");
        ctx.App.Type("Settings.CategoryPrompt", "Liste alle Termine auf: {transcript}");
        ctx.App.Click("Settings.Save");

        File.ReadAllText(ctx.Sandbox.TeamPrompts).Should().Contain("Teamvorlage Test");
        File.ReadAllText(Path.Combine(ctx.Sandbox.Home, "prompts.personal.json")).Should().NotContain("Teamvorlage Test");
    }
}

[Collection("Desktop")]
public sealed class ErrorFlowTests
{
    [Fact]
    public async Task ServerError_ShowsErrorToast()
    {
        using var ctx = UiTestContext.Start(stub: s => s.FailNext("/v1/audio/transcriptions", 500));
        ctx.DropFile("D1");
        (await ctx.WaitForToastAsync()).Should().Contain("Fehler");
        ctx.App.Find("Toast.Details").Should().NotBeNull();
    }

    [Fact]
    public async Task TooLargeFile_IsRefused_WithoutUpload()
    {
        using var ctx = UiTestContext.Start();
        SilenceMp3.Write(Path.Combine(ctx.Sandbox.Eingang, "gross.mp3"), TimeSpan.FromMinutes(30));
        (await ctx.WaitForToastAsync()).Should().Contain("25 MB");
        ctx.Stub.Requests.Should().NotContain(r => r.Path == "/v1/audio/transcriptions");
    }

    [Fact]
    public void ReleaseNotesButton_OpensWindow()
    {
        using var ctx = UiTestContext.Start();
        ctx.App.Click("Main.ReleaseNotes", mouse: true);
        ctx.App.Find("ReleaseNotes.Close").Should().NotBeNull();
    }
}

[Collection("Desktop")]
public sealed class KeyboardFlowTests
{
    [Fact]
    public async Task EveryMainButton_IsReachableByTab()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));
        var reached = ctx.TabThroughWindow(maxSteps: 120);   // sammelt AutomationIds des fokussierten Elements
        reached.Should().Contain(["Main.Settings", "Main.ReleaseNotes", "Entries.Dictate", "Detail.Copy", "Detail.Pdf", "Detail.ToggleDone"]);
    }
}
```
Ergänzungen im Kontext: `SelectModel(id)` (öffnet `Settings.ModelPicker`, wählt das Item, dessen Name den Anzeigenamen der Id enthält), `DropFile(fixture)` (MP3 nach `eingang\`, ohne auf einen Eintrag zu warten), `Task<string> WaitForToastAsync()` (Text des ersten `Toast.Item`), `IReadOnlyList<string> TabThroughWindow(int maxSteps)`.

- [ ] **Step 2: laufen lassen**, **Step 3: Commit** — `git commit -m "test: UI-Flows Einstellungen, Fehler, Neuigkeiten, Tastatur (#111)"`

### Task 14: CI-Job, Doku, Abschluss

**Files:**
- Modify: `.github/workflows/build.yml`
- Modify: `CLAUDE.md` (Build-Befehle, Architektur, Custom Notes), `README.md` nur falls Entwickler-Abschnitt vorhanden
- Serena-Memories: `backlog` („Vor jedem Release“: UI-Suite grün), `task_completion`, `suggested_commands`, `core` (Umgebungsvariablen)

- [ ] **Step 1: CI-Job** (eigener Job, nicht blockierend)

```yaml
  ui-tests:
    name: UI tests (FlaUI, non-blocking)
    runs-on: windows-latest
    timeout-minutes: 25
    continue-on-error: true
    steps:
      - uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - name: Build and run UI tests
        shell: powershell
        run: |
          dotnet build Platee.Johann.slnx
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
          dotnet test Platee.Johann.UiTests --no-build -p:RunUiTests=true --blame-hang-timeout 3m --results-directory TestResults
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
      - name: Upload UI artefacts
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: ui-test-results
          path: TestResults/
```
`actions/upload-artifact` auf einen festen Commit-SHA pinnen wie die anderen Actions (aktuellen SHA von v4 nachschlagen).

- [ ] **Step 2: Doku** — CLAUDE.md: neue Projekte in „Architecture“, Befehle `pwsh scripts/run-ui-tests.ps1`, `dotnet run --project tools/ui-driver -- …`, die drei Variablen mit Zweck, Sicherheitsregeln, „UiTests nie im Pre-Push“. Serena-Memories wie oben.

- [ ] **Step 3: Verifikation** — `dotnet build` (0 Fehler) · `dotnet test` (alle grün, UiTests nicht dabei) · `pwsh scripts/run-ui-tests.ps1` (alle grün oder mit Befund-Skip) · `dotnet format --verify-no-changes`.

- [ ] **Step 4: Commit + PR** — `git commit -m "ci: UI-Suite als eigener, nicht blockierender Job (#111)"`; Johann geschlossen; `git push -u origin test/111-ui-automation`; PR nach `release/v1.5.0` mit Sichtprüfungs-Checkliste (Screenshots vor/nach AutomationIds identisch) und Hinweis „Issue #111 von Hand schließen“.
