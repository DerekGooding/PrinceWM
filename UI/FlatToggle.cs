using PrinceWM.Helpers;
using System.Drawing.Drawing2D;

namespace PrinceWM.UI;

internal sealed class FlatToggle : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set { if (_checked == value) return; _checked = value; CheckedChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
    }

    public FlatToggle()
    {
        Size = new Size(44, 24);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnClick(EventArgs e)
    { Checked = !Checked; base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(0, (Height - 20) / 2, 42, 20);
        using (var path = ModernUI.RoundedRect(track, 10))
        using (var brush = new SolidBrush(_checked ? ModernUI.Accent : ModernUI.TrackOff))
            g.FillPath(brush, path);

        int knobX = _checked ? track.Right - 18 : track.Left + 2;
        using var knob = new SolidBrush(Color.White);
        g.FillEllipse(knob, knobX, track.Top + 2, 16, 16);
    }
}