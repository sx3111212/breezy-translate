using System.Drawing.Drawing2D;

namespace EnglishHelper;

sealed class RoundedButton : Button
{
    bool hover;
    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand; TabStop = false;
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        var color = !Enabled ? Color.FromArgb(245, 247, 250) : hover ? ControlPaint.Light(BackColor, .13f) : BackColor;
        using var brush = new SolidBrush(color); e.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Color.FromArgb(174, 183, 201), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
    public static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var path = new GraphicsPath(); int d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
}
