namespace Platee.Johann.UI.ViewModels;

using System.Collections.Generic;
using System.Linq;
using Platee.Johann.UI;

internal static class Finding04State
{
    public const string NoEntryDisabledReason = "Eintrag auswählen, um Aktionen zu nutzen.";

    public const string NoApiKeyDisabledReason = "Kein API-Key in Einstellungen → KI hinterlegt.";

    public static StartupPathIssue? FindMissingInputPathIssue(IReadOnlyList<StartupPathIssue> issues) =>
        issues.FirstOrDefault(static issue => issue.Label == "Quellverzeichnis");

    public static bool CanUseDetailActions(bool hasEntry, bool canProcess) => hasEntry && canProcess;

    public static string GetDetailActionDisabledReason(bool hasEntry, bool canProcess)
    {
        if (!hasEntry)
        {
            return NoEntryDisabledReason;
        }

        return canProcess ? string.Empty : NoApiKeyDisabledReason;
    }
}
