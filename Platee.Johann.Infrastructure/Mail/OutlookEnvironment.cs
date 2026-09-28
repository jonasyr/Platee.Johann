namespace Platee.Johann.Infrastructure.Mail;

using Microsoft.Win32;

/// <summary>
/// Which Outlook the user works with. New Outlook for Windows supports neither COM nor MAPI;
/// calling COM while it is active may start classic Outlook in the background instead of opening
/// a draft where the user looks (#57).
/// </summary>
public sealed class OutlookEnvironment
{
    private const string PreferencesKey = @"Software\Microsoft\Office\16.0\Outlook\Preferences";

    private readonly Func<int?> readUseNewOutlook;
    private readonly Func<bool> classicRegistered;
    private readonly Func<bool> newOutlookInstalled;

    /// <summary>Initializes a new instance reading the current user's registry and app alias.</summary>
    public OutlookEnvironment()
        : this(
            ReadUseNewOutlook,
            () => Type.GetTypeFromProgID("Outlook.Application") is not null,
            () => File.Exists(NewOutlookChannel.DefaultLauncherPath))
    {
    }

    /// <summary>Initializes a new instance with injectable probes, for tests.</summary>
    /// <param name="readUseNewOutlook">Returns the <c>UseNewOutlook</c> DWORD, or <c>null</c> if absent.</param>
    /// <param name="classicRegistered">Whether <c>Outlook.Application</c> is registered for COM.</param>
    /// <param name="newOutlookInstalled">Whether new Outlook's <c>olk.exe</c> launcher exists.</param>
    public OutlookEnvironment(Func<int?> readUseNewOutlook, Func<bool> classicRegistered, Func<bool>? newOutlookInstalled = null)
    {
        this.readUseNewOutlook = readUseNewOutlook;
        this.classicRegistered = classicRegistered;
        this.newOutlookInstalled = newOutlookInstalled ?? (static () => false);
    }

    /// <summary>Gets a value indicating whether the user switched to new Outlook.</summary>
    public bool IsNewOutlookActive => this.readUseNewOutlook() == 1;

    /// <summary>Gets a value indicating whether classic Outlook can be automated here.</summary>
    public bool CanUseClassicOutlook => !this.IsNewOutlookActive && this.classicRegistered();

    /// <summary>
    /// Gets a value indicating whether a draft should open in new Outlook: it is installed and
    /// either the user switched to it, or there is no classic Outlook at all — on a machine with
    /// only new Outlook nobody sets <c>UseNewOutlook</c>. Merely installed next to a classic
    /// Outlook in use, it stays out of the way.
    /// </summary>
    public bool CanUseNewOutlook =>
        this.newOutlookInstalled() && (this.IsNewOutlookActive || !this.classicRegistered());

    private static int? ReadUseNewOutlook()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PreferencesKey);
            return key?.GetValue("UseNewOutlook") as int?;
        }
        catch (Exception)
        {
            // An unreadable key is no reason to lose the mail; treat it as "not set".
            return null;
        }
    }
}
