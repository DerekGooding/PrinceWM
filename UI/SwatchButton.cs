using System.Drawing.Drawing2D;

namespace PrinceWM.UI;

internal sealed class SwatchButton : Control
{
    private Color _color = Color.White;
    public Color Color
    { get => _color; set { _color = value; Invalidate(); } }

    public SwatchButton()
    {
        Size = new Size(64, 26);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ModernUI.RoundedRect(r, 7);
        using (var brush = new SolidBrush(_color)) g.FillPath(brush, path);
        using var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f); g.DrawPath(pen, path);
    }
}