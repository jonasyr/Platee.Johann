namespace Platee.Johann.UiDriver.Automation;

using System.ComponentModel;
using System.Runtime.InteropServices;
using FlaUI.Core.WindowsAPI;

/// <summary>
/// Sends a chord's <see cref="KeyEvent"/>s with a single <c>SendInput</c> call, so no other input
/// can interleave and the modifiers are held down for the whole chord. Each event carries both
/// the virtual key and its scan code (MapVirtualKey), plus KEYEVENTF_EXTENDEDKEY where needed.
/// </summary>
internal static class NativeKeyboard
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventFExtendedKey = 0x0001;
    private const uint KeyEventFKeyUp = 0x0002;
    private const uint MapVkVkToVsc = 0;

    public static void SendChord(IReadOnlyList<VirtualKeyShort> keys)
    {
        var events = KeyChord.ToEvents(keys, ScanCodeOf);
        var inputs = events.Select(ToInput).ToArray();
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException(
                $"SendInput hat nur {sent} von {inputs.Length} Tastenereignissen gesendet",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }
    }

    private static ushort ScanCodeOf(VirtualKeyShort key) => (ushort)MapVirtualKey((uint)key, MapVkVkToVsc);

    private static Input ToInput(KeyEvent keyEvent) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = (ushort)keyEvent.Key,
                ScanCode = keyEvent.ScanCode,
                Flags = (keyEvent.Extended ? KeyEventFExtendedKey : 0) | (keyEvent.KeyUp ? KeyEventFKeyUp : 0),
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            },
        },
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    /// <summary>Must include MOUSEINPUT: it is the largest member and fixes sizeof(INPUT).</summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
