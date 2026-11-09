using System.IO;
using System.Runtime.InteropServices;

namespace EnglishHelper;

static partial class UiTest
{
    public static void SaveAnimationPreview()
    {
        using var owner = new Form();
        using var reading = new ScenarioPreview(false) { Size = new Size(665, 154), TargetCode = "zh-CN" };
        using var writing = new ScenarioPreview(true) { Size = new Size(665, 174), TargetCode = "en", Shortcut = "Ctrl+Alt+E" };
        owner.Controls.AddRange([reading, writing]);
        using var sheet = new Bitmap(1354, 4 * 194 + 12);
        using var graphics = Graphics.FromImage(sheet); graphics.Clear(Color.FromArgb(237, 241, 248));
        double[] frames = [1.6, 3.3, 4.2, 6.2];
        for (int index = 0; index < frames.Length; index++)
        {
            foreach (var preview in new[] { reading, writing })
            {
                preview.SeekPreview(frames[index]);
                using var frame = new Bitmap(preview.Width, preview.Height);
                preview.DrawToBitmap(frame, preview.ClientRectangle);
                graphics.DrawImageUnscaled(frame, preview.IsInput ? 677 : 12, 12 + index * 194);
                frame.Save(Path.Combine(AppContext.BaseDirectory, (preview.IsInput ? "input-demo-" : "selection-demo-") + index + ".png"));
            }
        }
        sheet.Save(Path.Combine(AppContext.BaseDirectory, "animation-preview.png"));
    }
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    public static void SavePopupPreviews()
    {
        var work = Screen.PrimaryScreen!.WorkingArea;
        using var background = new Form
        {
            FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true,
            StartPosition = FormStartPosition.Manual, BackColor = Color.FromArgb(237, 241, 248),
            Size = new Size(640, 600), Location = new Point(work.Left + (work.Width - 640) / 2, work.Top + (work.Height - 600) / 2)
        };
        background.Show(); Application.DoEvents();
        void Save(ResultWindow popup, string file)
        {
            popup.Owner = background; popup.StartPosition = FormStartPosition.Manual;
            popup.Location = new Point(background.Left + 30, background.Top + 30);
            popup.Show(); FocusTestWindow(popup.Handle); Application.DoEvents(); DwmFlush();
            var rectangle = Rectangle.Inflate(popup.Bounds, 8, 8);
            using var image = new Bitmap(rectangle.Width, rectangle.Height);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(rectangle.Location, Point.Empty, rectangle.Size);
            image.Save(Path.Combine(AppContext.BaseDirectory, file)); popup.Close();
        }
        using (var word = new ResultWindow("move", IntPtr.Zero, false, allowAutoRead: false))
        {
            word.Translate().GetAwaiter().GetResult(); Save(word, "dictionary-preview.png");
        }
        using (var sentence = new ResultWindow("A small change can make things easier.", IntPtr.Zero, false, allowAutoRead: false))
        {
            sentence.SetResult("一个小小的改变就能让事情变得更轻松。", ""); Save(sentence, "sentence-preview.png");
        }
        background.Close();
    }
}
