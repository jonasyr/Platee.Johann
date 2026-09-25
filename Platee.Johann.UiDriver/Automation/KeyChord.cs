namespace Platee.Johann.UiDriver.Automation;

using System.Globalization;
using FlaUI.Core.WindowsAPI;

/// <summary>
/// Parses a human-typed key combination such as "Ctrl+Plus" or "Shift+Tab" into its ordered
/// <see cref="VirtualKeyShort"/> keys, and turns those into the down/up event sequence a chord
/// needs (<see cref="ToEvents"/>). A chord is <b>not</b> a sequence of taps: FlaUI's
/// <c>Keyboard.Type(params VirtualKeyShort[])</c> presses and releases each key in turn, so
/// "Ctrl+0" became "tap Ctrl, then tap 0" and no <c>InputBinding</c> ever fired (S7).
/// </summary>
public static class KeyChord
{
    /// <summary>Keys whose scan code lives in the E0-prefixed block and need KEYEVENTF_EXTENDEDKEY.</summary>
    private static readonly HashSet<VirtualKeyShort> ExtendedKeys =
    [
        VirtualKeyShort.RCONTROL,
        VirtualKeyShort.RMENU,
        VirtualKeyShort.INSERT,
        VirtualKeyShort.DELETE,
        VirtualKeyShort.HOME,
        VirtualKeyShort.END,
        VirtualKeyShort.PRIOR,
        VirtualKeyShort.NEXT,
        VirtualKeyShort.LEFT,
        VirtualKeyShort.RIGHT,
        VirtualKeyShort.UP,
        VirtualKeyShort.DOWN,
        VirtualKeyShort.NUMLOCK,
        VirtualKeyShort.DIVIDE,
        VirtualKeyShort.SNAPSHOT,
        VirtualKeyShort.CANCEL,
        VirtualKeyShort.LWIN,
        VirtualKeyShort.RWIN,
        VirtualKeyShort.APPS,
    ];

    private static readonly IReadOnlyDictionary<string, VirtualKeyShort> Names = BuildNames();

    /// <summary>
    /// Pure: the ordered key events for a parsed chord — every key pressed in order (modifiers
    /// first, so they are held for the whole chord), then released in reverse order.
    /// <paramref name="scanCodeOf"/> maps a virtual key to its scan code (MapVirtualKey in
    /// production, a fake in tests).
    /// </summary>
    public static IReadOnlyList<KeyEvent> ToEvents(IReadOnlyList<VirtualKeyShort> keys, Func<VirtualKeyShort, ushort> scanCodeOf)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(scanCodeOf);

        var downs = keys.Select(key => ToEvent(key, scanCodeOf, keyUp: false));
        var ups = keys.Reverse().Select(key => ToEvent(key, scanCodeOf, keyUp: true));
        return [.. downs, .. ups];
    }

    public static bool IsExtended(VirtualKeyShort key) => ExtendedKeys.Contains(key);

    public static VirtualKeyShort[] Parse(string chord)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chord);

        return [.. chord
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseKey)];
    }

    private static KeyEvent ToEvent(VirtualKeyShort key, Func<VirtualKeyShort, ushort> scanCodeOf, bool keyUp) =>
        new(key, scanCodeOf(key), IsExtended(key), keyUp);

    private static VirtualKeyShort ParseKey(string name)
    {
        if (Names.TryGetValue(name, out var key))
        {
            return key;
        }

        throw new ArgumentException($"Unbekannte Taste '{name}' in Tastenkombination.", nameof(name));
    }

    private static Dictionary<string, VirtualKeyShort> BuildNames()
    {
        var map = new Dictionary<string, VirtualKeyShort>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ctrl"] = VirtualKeyShort.CONTROL,
            ["Control"] = VirtualKeyShort.CONTROL,
            ["Shift"] = VirtualKeyShort.SHIFT,
            ["Alt"] = VirtualKeyShort.ALT,
            ["Plus"] = VirtualKeyShort.ADD,
            ["Minus"] = VirtualKeyShort.SUBTRACT,
            ["Delete"] = VirtualKeyShort.DELETE,
            ["Tab"] = VirtualKeyShort.TAB,
            ["Enter"] = VirtualKeyShort.ENTER,
            ["Escape"] = VirtualKeyShort.ESCAPE,
            ["Space"] = VirtualKeyShort.SPACE,
            ["PageDown"] = VirtualKeyShort.NEXT,
            ["PageUp"] = VirtualKeyShort.PRIOR,
            ["Home"] = VirtualKeyShort.HOME,
            ["End"] = VirtualKeyShort.END,
            ["Left"] = VirtualKeyShort.LEFT,
            ["Right"] = VirtualKeyShort.RIGHT,
            ["Up"] = VirtualKeyShort.UP,
            ["Down"] = VirtualKeyShort.DOWN,
        };

        for (var digit = 0; digit <= 9; digit++)
        {
            var name = digit.ToString(CultureInfo.InvariantCulture);
            map[name] = Enum.Parse<VirtualKeyShort>($"KEY_{name}");
        }

        for (var letter = 'A'; letter <= 'Z'; letter++)
        {
            map[letter.ToString(CultureInfo.InvariantCulture)] = Enum.Parse<VirtualKeyShort>($"KEY_{letter}");
        }

        for (var fKey = 1; fKey <= 24; fKey++)
        {
            var name = "F" + fKey.ToString(CultureInfo.InvariantCulture);
            map[name] = Enum.Parse<VirtualKeyShort>(name);
        }

        return map;
    }
}
