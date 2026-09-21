namespace Platee.Johann.Infrastructure.Mail;

using System.Diagnostics;
using System.Text;
using Platee.Johann.Application.Mail;

/// <summary>
/// Opens a draft in the new Outlook for Windows by writing an .eml file (<see cref="EmlDraft"/>)
/// and handing it to <c>olk.exe</c>. Checked by hand on Outlook 1.2026.812: the draft opens
/// editable, with HTML, nested lists, the PDF attached and the user's signature added below.
/// <para>
/// The launcher is addressed directly rather than through the .eml file association, which on a
/// machine with both Outlooks often still points to classic Outlook.
/// </para>
/// </summary>
public sealed class NewOutlookChannel : IMailChannel
{
    private static readonly TimeSpan KeepDrafts = TimeSpan.FromDays(1);

    private readonly string launcherPath;
    private readonly string draftDirectory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="launcherPath">Path to <c>olk.exe</c>; defaults to the app execution alias.</param>
    /// <param name="draftDirectory">Where the .eml files go; defaults to <c>%TEMP%\Johann\Mail</c>.</param>
    public NewOutlookChannel(string? launcherPath = null, string? draftDirectory = null)
    {
        this.launcherPath = launcherPath ?? DefaultLauncherPath;
        this.draftDirectory = draftDirectory ?? Path.Combine(Path.GetTempPath(), "Johann", "Mail");
    }

    /// <summary>Gets the app execution alias new Outlook registers for the current user.</summary>
    public static string DefaultLauncherPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft",
        "WindowsApps",
        "olk.exe");

    /// <summary>Writes the draft file; separate from launching so it can be tested without Outlook.</summary>
    /// <param name="draft">The mail.</param>
    /// <returns>The path of the written .eml file.</returns>
    public string WriteDraft(MailDraft draft)
    {
        Directory.CreateDirectory(this.draftDirectory);
        this.DeleteOldDrafts();

        var attachments = draft.Attachments
            .Select(path => (Path.GetFileName(path), File.ReadAllBytes(path)))
            .ToList();
        var id = Guid.NewGuid();
        var eml = EmlDraft.Build(draft.Subject, MailHtml.ToFragment(draft.BodyMarkdown), attachments, DateTimeOffset.Now, id);

        var path = Path.Combine(this.draftDirectory, $"Johann-{id:N}.eml");
        File.WriteAllText(path, eml, Encoding.ASCII);
        return path;
    }

    /// <inheritdoc/>
    public Task ComposeAsync(MailDraft draft, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var path = this.WriteDraft(draft);
        // ShellExecute, not a child process: the long-running Outlook must not inherit Johann's
        // handles (a test host, for one, waited for it to exit).
        using var _ = Process.Start(new ProcessStartInfo(this.launcherPath, $"\"{path}\"") { UseShellExecute = true });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Outlook copies the draft into its own store, so the file is only needed while it opens.
    /// Keeping a day's worth avoids deleting one that is still being read.
    /// </summary>
    private void DeleteOldDrafts()
    {
        foreach (var file in Directory.EnumerateFiles(this.draftDirectory, "Johann-*.eml"))
        {
            try
            {
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > KeepDrafts)
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Still open elsewhere; try again next time.
            }
            catch (UnauthorizedAccessException)
            {
                // Not ours to delete; leave it.
            }
        }
    }
}
