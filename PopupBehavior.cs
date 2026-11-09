using System.Runtime.InteropServices;

namespace EnglishHelper;

static class PopupChrome
{
    [StructLayout(LayoutKind.Sequential)] struct Margins { public int Left, Right, Top, Bottom; }
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);
    public static void Apply(IntPtr window)
    {
        // Window regions are binary masks and produce jagged corners. Let DWM
        // composite the edge and shadow instead; older Windows keeps a clean rectangle.
        int rounded = 2, border = 0x00F0E8E2, rendering = 2;
        DwmSetWindowAttribute(window, 2, ref rendering, sizeof(int));
        var margins = new Margins { Left = 1, Right = 1, Top = 1, Bottom = 1 };
        DwmExtendFrameIntoClientArea(window, ref margins);
        DwmSetWindowAttribute(window, 33, ref rounded, sizeof(int));
        DwmSetWindowAttribute(window, 34, ref border, sizeof(int));
    }
}

sealed class PopupDismissal : IDisposable
{
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window, uint command);
    readonly Form popup;
    readonly Action dismiss;
    readonly MouseObserver observer = new();
    public PopupDismissal(Form popup, Action dismiss)
    {
        this.popup = popup; this.dismiss = dismiss;
        observer.ButtonChanged += (point, down) =>
        {
            if (!down || popup.IsDisposed || !popup.IsHandleCreated) return;
            popup.BeginInvoke(() => CheckPointer(point));
        };
    }
    internal void CheckPointer(Point point)
    {
        if (popup.IsDisposed || !popup.Visible) return;
        var window = Native.GetAncestor(Native.WindowFromPoint(point), 2);
        for (int depth = 0; window != IntPtr.Zero && depth < 8; depth++)
        {
            if (window == popup.Handle) return;
            window = GetWindow(window, 4); // Keep owned menus and other popup-owned windows usable.
        }
        dismiss();
    }
    public void Dispose() => observer.Dispose();
}
