namespace Platee.Johann.Infrastructure.Mail;

using System.Runtime.InteropServices;
using Platee.Johann.Application.Mail;

/// <summary>
/// Opens a draft in classic Outlook through its COM object model, late bound so no Office interop
/// assembly has to ship with Johann.
/// <para>
/// Order matters: <c>Display()</c> first, because only then does Outlook insert the user's
/// default signature; afterwards the content is placed right after the body tag, above that
/// signature. Setting the body before <c>Display()</c> suppresses the signature entirely.
/// </para>
/// <para>
/// Runs on a dedicated STA thread: Outlook's object model is apartment-threaded, and starting
/// Outlook can take seconds that must not freeze Johann's window.
/// </para>
/// </summary>
public sealed class ClassicOutlookChannel : IMailChannel
{
    private const int OlMailItem = 0;

    /// <inheritdoc/>
    public Task ComposeAsync(MailDraft draft, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                Compose(draft);
                done.SetResult();
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Johann Outlook",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }

    private static void Compose(MailDraft draft)
    {
        var type = Type.GetTypeFromProgID("Outlook.Application", throwOnError: true)!;
        object? application = null;
        object? mail = null;
        try
        {
            application = Activator.CreateInstance(type)!;
            mail = ((dynamic)application).CreateItem(OlMailItem);
            dynamic item = mail!;

            item.Subject = draft.Subject;
            foreach (var path in draft.Attachments)
            {
                item.Attachments.Add(path);
            }

            item.Display(false);
            string outlookHtml = item.HTMLBody;
            item.HTMLBody = MailHtml.InsertAtBodyStart(outlookHtml, MailHtml.ToFragment(draft.BodyMarkdown));
        }
        finally
        {
            // Release our references only; the displayed mail and Outlook itself stay open.
            if (mail is not null && Marshal.IsComObject(mail))
            {
                Marshal.ReleaseComObject(mail);
            }

            if (application is not null && Marshal.IsComObject(application))
            {
                Marshal.ReleaseComObject(application);
            }
        }
    }
}
