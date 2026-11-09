using System.Drawing.Drawing2D;

namespace EnglishHelper;

sealed class AutoReadSwitch : CheckBox
{
    public AutoReadSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Text = "自动朗读"; AccessibleName = "自动朗读单词"; AutoSize = false; Size = new Size(116, 28); Cursor = Cursors.Hand;
        Font = new Font("Microsoft YaHei UI", 9); BackColor = Color.White;
    }
    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, 0, Width - 39, Height), Color.FromArgb(116, 127, 150), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        var track = new Rectangle(Width - 34, (Height - 17) / 2, 32, 17);
        using var shape = RoundedButton.Rounded(track, 8);
        using var fill = new SolidBrush(Checked ? Color.FromArgb(100, 120, 222) : Color.FromArgb(220, 226, 236));
        e.Graphics.FillPath(fill, shape);
        using var thumb = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(thumb, Checked ? track.Right - 15 : track.Left + 2, track.Top + 2, 13, 13);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }
}

sealed class SpeakerButton : Button
{
    public SpeakerButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Size = new Size(30, 30); BackColor = Color.White;
        AccessibleName = "朗读单词"; Cursor = Cursors.Hand;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(139, 151, 174), 1.5f) { LineJoin = LineJoin.Round };
        e.Graphics.DrawPolygon(pen, [new Point(5, 12), new Point(9, 12), new Point(14, 8), new Point(14, 23), new Point(9, 19), new Point(5, 19)]);
        e.Graphics.DrawArc(pen, 12, 10, 10, 12, -60, 120); e.Graphics.DrawArc(pen, 12, 6, 16, 20, -55, 110);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }
}
