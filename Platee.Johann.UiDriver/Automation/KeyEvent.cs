namespace Platee.Johann.UiDriver.Automation;

using FlaUI.Core.WindowsAPI;

/// <summary>
/// One keyboard event of a chord, exactly as it goes into a <c>SendInput</c> KEYBDINPUT:
/// virtual key, scan code, whether KEYEVENTF_EXTENDEDKEY is set, and down/up.
/// </summary>
public sealed record KeyEvent(VirtualKeyShort Key, ushort ScanCode, bool Extended, bool KeyUp);
