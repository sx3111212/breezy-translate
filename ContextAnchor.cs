using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace EnglishHelper;

sealed record SelectedText(string Text, Rectangle Bounds, IntPtr Window);

static class ContextAnchor
{
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);

    public static SelectedText? FindSelection(Point? releasedAt = null)
    {
        var window = Native.GetForegroundWindow();
        GetWindowThreadProcessId(window, out uint process);
        var candidates = new List<AutomationElement>();
        if (releasedAt is Point point)
        {
            try { candidates.Add(AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y))); } catch { }
        }
        try { if (AutomationElement.FocusedElement is { } focus) candidates.Add(focus); } catch { }
        foreach (var candidate in candidates)
        {
            var element = candidate;
            for (int depth = 0; element != null && depth < 8; depth++)
            {
                try
                {
                    if (element.Current.ProcessId != process || element.Current.IsPassword) break;
                    if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
                    {
                        foreach (var range in ((TextPattern)pattern).GetSelection())
                        {
                            var text = range.GetText(3001);
                            if (string.IsNullOrWhiteSpace(text)) continue;
                            var rectangles = range.GetBoundingRectangles().Where(r => !r.IsEmpty && r.Width > 0 && r.Height > 0).ToArray();
                            Rectangle bounds;
                            if (rectangles.Length > 0)
                            {
                                // Anchor at the first selected line, rather than a document-wide box.
                                var first = rectangles[0];
                                bounds = Rectangle.FromLTRB((int)first.Left, (int)first.Top, (int)Math.Ceiling(first.Right), (int)Math.Ceiling(first.Bottom));
                            }
                            else if (releasedAt is Point fallback) bounds = new Rectangle(fallback.X - 2, fallback.Y - 14, 4, 16);
                            else continue;
                            if (Native.GetForegroundWindow() != window) return null;
                            return new SelectedText(text, bounds, window);
                        }
                    }
                    element = TreeWalker.ControlViewWalker.GetParent(element);
                }
                catch { break; }
            }
        }
        return null;
    }

    public static Point SelectionCenter(Rectangle selection, int diameter) => new(selection.Right + diameter / 2 + 6, selection.Top - diameter / 2 - 3);
    public static Point InputCenter(Rectangle input, int diameter, bool bottom) => new(input.Right - 7, bottom ? input.Bottom - 7 : input.Top + Math.Min(input.Height / 2, 16));

    public static Point ClampCenter(Point point, int diameter, Rectangle work)
    {
        int half = diameter / 2;
        return new Point(Math.Clamp(point.X, work.Left + half + 4, Math.Max(work.Left + half + 4, work.Right - half - 4)),
            Math.Clamp(point.Y, work.Top + half + 4, Math.Max(work.Top + half + 4, work.Bottom - half - 4)));
    }

    public static Point PopupLocation(Rectangle anchor, Size popup, Rectangle work)
    {
        int x = anchor.Right + 10, y = anchor.Bottom + 10;
        if (x + popup.Width > work.Right) x = anchor.Right - popup.Width;
        if (y + popup.Height > work.Bottom) y = anchor.Top - popup.Height - 10;
        return new Point(Math.Clamp(x, work.Left + 8, Math.Max(work.Left + 8, work.Right - popup.Width - 8)),
            Math.Clamp(y, work.Top + 8, Math.Max(work.Top + 8, work.Bottom - popup.Height - 8)));
    }
}

// The hook only posts coordinates. Accessibility work runs later, outside the callback.
sealed class MouseObserver : IDisposable
{
    delegate IntPtr HookCallback(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] struct MouseData { public int X, Y; public uint Mouse, Flags, Time; public UIntPtr Extra; }
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookCallback callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? module);
    readonly HookCallback callback;
    IntPtr hook;
    public event Action<Point, bool>? ButtonChanged;
    public MouseObserver()
    {
        callback = Callback;
        hook = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0);
        // Polling remains available on systems that do not permit a global mouse observer.
    }
    IntPtr Callback(int code, IntPtr message, IntPtr data)
    {
        int kind = (int)message;
        if (code >= 0 && kind is 0x201 or 0x202 or 0x204 or 0x205 or 0x207 or 0x208)
        {
            var mouse = Marshal.PtrToStructure<MouseData>(data);
            try { ButtonChanged?.Invoke(new Point(mouse.X, mouse.Y), kind is 0x201 or 0x204 or 0x207); } catch { }
        }
        return CallNextHookEx(hook, code, message, data);
    }
    public void Dispose() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }
}
