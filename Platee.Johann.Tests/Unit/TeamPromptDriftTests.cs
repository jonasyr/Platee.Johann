using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Platee.Johann.Application.Processing;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Wacht darüber, dass die Prompt-Konstanten im Code nicht von der Team-Datei abdriften.
/// <para>
/// Die Datei auf dem Team-Share ist die Wahrheit: sie gewinnt zur Laufzeit immer über die
/// Konstanten (<c>dto.AufgabePrompt ?? defaults.AufgabePrompt</c>). Die Konstanten greifen
/// nur bei Neuinstallationen und wenn der Share nicht erreichbar ist. Laufen beide
/// auseinander, bekommt genau derjenige veraltete Prompts, der ohnehin schon ein Problem
/// hat — offline oder frisch installiert.
/// </para>
/// <para>
/// Ist der Share nicht erreichbar (CI, fremder Rechner, kein VPN), überspringt sich der
/// Test selbst, statt rot zu werden.
/// </para>
/// </summary>
public sealed class TeamPromptDriftTests
{
    private const string DefaultTeamPromptPath = @"Z:\12_Tools\Peano\Johann\prompts.json";

    /// <summary>Erlaubt es, den Pfad zu überschreiben, ohne den Test anzufassen.</summary>
    private const string PathOverrideVariable = "JOHANN_TEAM_PROMPTS";

    public static TheoryData<string, string> PromptPairs() => new()
    {
        { nameof(SummaryPrompts.SystemMessage), "systemMessage" },
        { nameof(SummaryPrompts.Abstract), "abstractPrompt" },
        { nameof(SummaryPrompts.Structured), "structuredPrompt" },
        { nameof(SummaryPrompts.Prose), "prosePrompt" },
        { nameof(SummaryPrompts.Email), "emailPrompt" },
        { nameof(SummaryPrompts.Aufgabe), "aufgabePrompt" },
        { nameof(SummaryPrompts.Gespraechsnotiz), "gespraechsnotizPrompt" },
        { nameof(SummaryPrompts.Stundenzettel), "stundenzettelPrompt" },
        { nameof(SummaryPrompts.Analog), "analogPrompt" },
    };

    [Theory]
    [MemberData(nameof(PromptPairs))]
    public void Constant_matches_the_team_file(string constantName, string jsonKey)
    {
        var teamPrompts = TryLoadTeamPrompts();
        if (teamPrompts is null)
        {
            // Share nicht erreichbar — nichts zu vergleichen, kein Fehler.
            return;
        }

        teamPrompts.TryGetValue(jsonKey, out var fromFile).Should().BeTrue(
            $"die Team-Datei muss den Schlüssel '{jsonKey}' enthalten");

        Normalize(ConstantValue(constantName)).Should().Be(
            Normalize(fromFile),
            $"'{constantName}' im Code und '{jsonKey}' auf dem Team-Share müssen identisch sein. "
            + "Weicht etwas ab, wurde eine der beiden Seiten allein geändert.");
    }

    private static string ConstantValue(string name) => name switch
    {
        nameof(SummaryPrompts.SystemMessage) => SummaryPrompts.SystemMessage,
        nameof(SummaryPrompts.Abstract) => SummaryPrompts.Abstract,
        nameof(SummaryPrompts.Structured) => SummaryPrompts.Structured,
        nameof(SummaryPrompts.Prose) => SummaryPrompts.Prose,
        nameof(SummaryPrompts.Email) => SummaryPrompts.Email,
        nameof(SummaryPrompts.Aufgabe) => SummaryPrompts.Aufgabe,
        nameof(SummaryPrompts.Gespraechsnotiz) => SummaryPrompts.Gespraechsnotiz,
        nameof(SummaryPrompts.Stundenzettel) => SummaryPrompts.Stundenzettel,
        nameof(SummaryPrompts.Analog) => SummaryPrompts.Analog,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unbekannte Prompt-Konstante."),
    };

    /// <summary>
    /// Liest die Team-Datei, oder gibt <c>null</c> zurück, wenn sie aus irgendeinem Grund
    /// nicht lesbar ist. Bewusst großzügig im Fangen: ein nicht verbundenes Netzlaufwerk
    /// wirft je nach Zustand ganz unterschiedliche Ausnahmen, und keine davon ist ein
    /// Testfehler.
    /// </summary>
    private static IReadOnlyDictionary<string, string>? TryLoadTeamPrompts()
    {
        var path = Environment.GetEnvironmentVariable(PathOverrideVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = DefaultTeamPromptPath;
        }

        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    result[property.Name] = property.Value.GetString() ?? string.Empty;
                }
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).ReplaceLineEndings("\n").Trim();
}
