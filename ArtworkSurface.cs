using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace EnglishHelper;

// A per-pixel-alpha window keeps the generated PNG's smooth transparent edges.
class ArtworkSurface : Form
{
    readonly Bitmap artwork;
    readonly System.Windows.Forms.Timer animation = new() { Interval = 32 };
    float reveal;
    bool hover;
    float phase;
    protected bool IsWorking { get; set; }
    protected bool IsSuccessful { get; set; }
    protected bool IsSelectionMode { get; set; }
    protected bool ExpandToRight { get; set; }
    protected bool PointerOverArtwork => hover;
    protected Rectangle OrbBounds => new(ExpandToRight ? 2 : 150, 2, 44, 44);
    internal Rectangle BubbleScreenBounds => new(Left + (int)((ExpandToRight ? 2f : 150f) / 196 * Width), Top + (int)(2f / 48 * Height), (int)(44f / 196 * Width), (int)(44f / 48 * Height));

    protected ArtworkSurface()
    {
        using var stream = typeof(ArtworkSurface).Assembly.GetManifestResourceStream("EnglishHelper.Assets.translation-orb.png")
            ?? throw new InvalidOperationException("Missing floating-button artwork.");
        artwork = new Bitmap(stream);
        Text = UiText.T("app"); AccessibleName = UiText.T("bubbleAccessible");
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(196, 48);
        Cursor = Cursors.Hand;
        animation.Tick += (_, _) =>
        {
            bool inside = ClientRectangle.Contains(PointToClient(MousePosition));
            if (!inside) hover = false;
            float target = hover || IsWorking ? 1 : 0;
            reveal += (target - reveal) * .24f;
            if (Math.Abs(target - reveal) < .01f) reveal = target;
            phase = (phase + 9) % 360;
            UpdateArtwork();
            if (reveal == target && !IsWorking) animation.Stop();
        };
    }

    protected Point LogicalPoint(Point client) => new((int)(client.X * 196f / ClientSize.Width), (int)(client.Y * 48f / ClientSize.Height));
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x80000 | 0x80; return cp; }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x21) { m.Result = (IntPtr)3; return; }
        base.WndProc(ref m);
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e); Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x13); UpdateArtwork();
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; animation.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; animation.Start(); }
    protected override void OnLocationChanged(EventArgs e) { base.OnLocationChanged(e); if (Visible) UpdateArtwork(); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (Visible) UpdateArtwork(); }
    protected void RefreshArtwork() { UpdateArtwork(); if (IsWorking) animation.Start(); }

    public Bitmap RenderPreview(bool expanded)
        => Render(expanded ? 1 : 0, expanded);

    internal static string ShortcutCaption(bool selection, AppPreferences preferences)
        => preferences.HotkeysEnabled && (selection ? preferences.ReadHotkeyEnabled : preferences.InputHotkeyEnabled)
            ? selection ? preferences.ReadHotkey : preferences.InputHotkey
            : UiText.T("shortcutDisabled", preferences.InterfaceLanguage);

    Bitmap Render(float expansion, bool hovered)
    {
        var canvas = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(canvas);
        g.ScaleTransform(ClientSize.Width / 196f, ClientSize.Height / 48f);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        if (expansion > 0)
        {
            int alpha = (int)(255 * expansion);
            float slide = (1 - expansion) * 15;
            g.TranslateTransform(slide, 0);
            using var shadow = RoundedButton.Rounded(new Rectangle(ExpandToRight ? 23 : 8, 10, 169, 32), 15);
            using var shadowBrush = new SolidBrush(Color.FromArgb((int)(20 * expansion), 35, 45, 80)); g.FillPath(shadowBrush, shadow);
            using var panel = RoundedButton.Rounded(new Rectangle(ExpandToRight ? 21 : 6, 8, 169, 32), 15);
            using var fill = new SolidBrush(Color.FromArgb((int)(248 * expansion), 250, 251, 255)); g.FillPath(fill, panel);
            using var stroke = new Pen(Color.FromArgb((int)(180 * expansion), 219, 224, 243)); g.DrawPath(stroke, panel);
            using var title = new Font("Microsoft YaHei UI", IsWorking ? 9 : 8.5f);
            using var ink = new SolidBrush(Color.FromArgb(alpha, 61, 64, 100));
            using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            var caption = IsWorking ? UiText.T("translate") + "…" : IsSelectionMode ? UiText.T("readKey") : UiText.T("translate") + " → " + Languages.Get(UserPreferences.Current.InputTarget).Name;
            g.DrawString(caption, title, ink, new RectangleF(ExpandToRight ? 50 : 12, IsWorking ? 8 : 7, 134, IsWorking ? 32 : 18), centered);
            if (!IsWorking)
            {
                using var shortcutFont = new Font("Microsoft YaHei UI", 7.5f);
                using var shortcutInk = new SolidBrush(Color.FromArgb(alpha, 102, 111, 145));
                g.DrawString(ShortcutCaption(IsSelectionMode, UserPreferences.Current), shortcutFont, shortcutInk,
                    new RectangleF(ExpandToRight ? 50 : 12, 24, 134, 15), centered);
            }
            g.TranslateTransform(-slide, 0);
        }
        float size = 41 + expansion * 3;
        float centerX = ExpandToRight ? 24 : 172;
        g.DrawImage(artwork, new RectangleF(centerX - size / 2, 24 - size / 2 - expansion, size, size));
        if (IsWorking)
        {
            using var ring = new Pen(Color.FromArgb(210, 120, 106, 235), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(ring, centerX - 17, 6, 34, 34, phase, 110);
        }
        if (IsSuccessful)
        {
            using var fill = new SolidBrush(Color.FromArgb(36, 174, 139)); g.FillEllipse(fill, centerX + 9, 30, 13, 13);
            using var tick = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(tick, [new PointF(centerX + 12, 36), new PointF(centerX + 15, 39), new PointF(centerX + 19, 34)]);
        }
        return canvas;
    }

    void UpdateArtwork()
    {
        if (!IsHandleCreated || IsDisposed || artwork == null || ClientSize.Width < 1 || ClientSize.Height < 1) return;
        using var canvas = Render(reveal, hover);
        AlphaWindow.Update(Handle, canvas, Location);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { animation.Dispose(); artwork.Dispose(); }
        base.Dispose(disposing);
    }
}

static class AlphaWindow
{
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] struct SIZE { public int Width, Height; public SIZE(int w, int h) { Width = w; Height = h; } }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct BLEND { public byte Operation, Flags, Alpha, Format; }
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll", SetLastError = true)] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dstDc, ref POINT position, ref SIZE size, IntPtr srcDc, ref POINT source, uint color, ref BLEND blend, uint flags);

    public static void Update(IntPtr hwnd, Bitmap image, Point location)
    {
        var screen = GetDC(IntPtr.Zero); var memory = CreateCompatibleDC(screen);
        IntPtr bitmap = IntPtr.Zero, old = IntPtr.Zero;
        try
        {
            bitmap = image.GetHbitmap(Color.FromArgb(0)); old = SelectObject(memory, bitmap);
            var position = new POINT(location.X, location.Y); var size = new SIZE(image.Width, image.Height); var source = new POINT(0, 0);
            var blend = new BLEND { Alpha = 255, Format = 1 };
            if (!UpdateLayeredWindow(hwnd, screen, ref position, ref size, memory, ref source, 0, ref blend, 2))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (old != IntPtr.Zero) SelectObject(memory, old);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            DeleteDC(memory); ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
