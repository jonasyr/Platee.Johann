namespace Platee.Johann.UiDriver.Automation;

using System.Globalization;
using FlaUI.Core.WindowsAPI;

/// <summary>
/// Parses a human-typed key combination such as "Ctrl+Plus" or "Shift+Tab" into the ordered
/// <see cref="VirtualKeyShort"/> sequence <c>FlaUI.Core.Input.Keyboard.Type</c> expects.
/// </summary>
public static class KeyChord
{
    private static readonly IReadOnlyDictionary<string, VirtualKeyShort> Names = BuildNames();

    public static VirtualKeyShort[] Parse(string chord)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chord);

        return [.. chord
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseKey)];
    }

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
