namespace Platee.Johann.UiDriver.Automation;

/// <summary>
/// Buttons of a Windows message box, by their Win32 control id (IDOK, IDCANCEL, IDYES, IDNO).
/// The ids are the same in every display language; the captions are not.
/// </summary>
public enum DialogButton
{
    Ok = 1,
    Cancel = 2,
    Yes = 6,
    No = 7,
}
