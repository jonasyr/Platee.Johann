namespace Platee.Johann.UI.ViewModels;

using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;

/// <summary>Wording of the "really delete?" question (#55). Kept apart so it is testable.</summary>
public static class EntryDeletionPrompt
{
    public const string Title = "Eintrag löschen";

    public static string MessageFor(Entry entry, string trashDirectory) =>
        $"„{entry.SequenceNumber:D3} {entry.ProjectName} — {entry.Title}“ löschen?\n\n"
        + "Der Eintrag wird mit PDF, HTML, Transkript und Audio-Kopie in den Johann-Papierkorb verschoben:\n"
        + $"{trashDirectory}\n\n"
        + $"Dort bleibt er {(int)TrashPolicy.Retention.TotalDays} Tage und lässt sich bis dahin zurückholen; "
        + "danach entfernt Johann ihn endgültig.\n\n"
        + "Die Original-Aufnahme im Archiv bleibt erhalten.";
}
