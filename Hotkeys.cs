using System.Runtime.InteropServices;

namespace EnglishHelper;

sealed record HotkeyChord(uint Modifiers, Keys Key)
{
    public override string ToString() => ((Modifiers & 2) != 0 ? "Ctrl+" : "") + ((Modifiers & 1) != 0 ? "Alt+" : "") + ((Modifiers & 4) != 0 ? "Shift+" : "")
        + (Key is >= Keys.D0 and <= Keys.D9 ? ((int)Key - (int)Keys.D0).ToString() : Key.ToString());
    public static bool TryParse(string? value, out HotkeyChord chord)
    {
        if (string.IsNullOrWhiteSpace(value)) { chord = new(0, Keys.None); return false; }
        uint modifiers = 0; Keys key = Keys.None;
        foreach (var piece in value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (piece.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || piece.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= 2;
            else if (piece.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= 1;
            else if (piece.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= 4;
            else
            {
                if (key != Keys.None) { chord = new(0, Keys.None); return false; }
                if (piece.Length == 1 && char.IsDigit(piece[0])) key = Keys.D0 + (piece[0] - '0');
                else if (!Enum.TryParse(piece, true, out key)) { chord = new(0, Keys.None); return false; }
            }
        }
        chord = new(modifiers, key);
        return (modifiers & 3) != 0 && key is >= Keys.Space and <= Keys.F24 && key != Keys.F12 && key is not (Keys.LWin or Keys.RWin or Keys.ControlKey or Keys.ShiftKey or Keys.Menu);
    }
    public static string FromKey(KeyEventArgs key)
    {
        uint modifiers = (key.Control ? 2u : 0) | (key.Alt ? 1u : 0) | (key.Shift ? 4u : 0);
        return new HotkeyChord(modifiers, key.KeyCode).ToString();
    }
}

sealed class HotkeyManager(IntPtr window) : IDisposable
{
    public const int ReadId = 0x5101, InputId = 0x5102;
    [DllImport("user32.dll", SetLastError = true)] static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    AppPreferences? applied;
    bool suspended;
    void Clear() { UnregisterHotKey(window, ReadId); UnregisterHotKey(window, InputId); }
    bool Register(AppPreferences value)
    {
        if (!value.HotkeysEnabled) return true;
        HotkeyChord.TryParse(value.ReadHotkey, out var read); HotkeyChord.TryParse(value.InputHotkey, out var input);
        if (value.ReadHotkeyEnabled && !RegisterHotKey(window, ReadId, read.Modifiers | 0x4000, (uint)read.Key)) return false;
        if (value.InputHotkeyEnabled && !RegisterHotKey(window, InputId, input.Modifiers | 0x4000, (uint)input.Key)) { Clear(); return false; }
        return true;
    }
    public bool TryApply(AppPreferences value, out string error)
    {
        error = "";
        HotkeyChord.TryParse(value.ReadHotkey, out var read); HotkeyChord.TryParse(value.InputHotkey, out var input);
        if (value.HotkeysEnabled && (value.ReadHotkeyEnabled && !HotkeyChord.TryParse(value.ReadHotkey, out _) || value.InputHotkeyEnabled && !HotkeyChord.TryParse(value.InputHotkey, out _) || value.ReadHotkeyEnabled && value.InputHotkeyEnabled && read == input))
        { error = UiText.T("keyRule", value.InterfaceLanguage); return false; }
        var old = applied; bool wasSuspended = suspended; Clear();
        if (!Register(value))
        {
            Clear(); if (old != null && !wasSuspended) Register(old);
            error = UiText.T("conflict", value.InterfaceLanguage); return false;
        }
        applied = value; suspended = false; return true;
    }
    public void Suspend() { Clear(); suspended = true; }
    public bool Resume(AppPreferences value, out string error)
    {
        error = "";
        if (!suspended && applied != null && applied.HotkeysEnabled == value.HotkeysEnabled && applied.ReadHotkeyEnabled == value.ReadHotkeyEnabled && applied.InputHotkeyEnabled == value.InputHotkeyEnabled && applied.ReadHotkey == value.ReadHotkey && applied.InputHotkey == value.InputHotkey) return true;
        return TryApply(value, out error);
    }
    public static async Task<bool> WaitForModifiers()
    {
        for (int count = 0; count < 100; count++)
        {
            if ((Native.GetAsyncKeyState(0x11) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x10) & 0x8000) == 0) return true;
            await Task.Delay(15);
        }
        return false;
    }
    public void Dispose() => Clear();
}
