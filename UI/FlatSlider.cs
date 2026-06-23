using PrinceWM.Helpers;
using System.Drawing.Drawing2D;

namespace PrinceWM.UI;

internal sealed class FlatSlider : Control
{
    private int _min, _max = 100, _value;
    private bool _drag;

    public event EventHandler? ValueChanged;

    public int Minimum
    { get => _min; set { _min = value; Invalidate(); } }
    public int Maximum
    { get => _max; set { _max = value; Invalidate(); } }

    public int Value
    {
        get => _value;
        set { int v = Math.Clamp(value, _min, _max); if (v == _value) return; _value = v; ValueChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
    }

    public FlatSlider()
    {
        Height = 24;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    private void SetFromX(int x)
    {
        float t = Math.Clamp((x - 8f) / Math.Max(1f, Width - 16f), 0f, 1f);
        Value = _min + (int)MathF.Round(t * (_max - _min));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    { _drag = true; SetFromX(e.X); base.OnMouseDown(e); }

    protected override void OnMouseMove(MouseEventArgs e)
    { if (_drag) SetFromX(e.X); base.OnMouseMove(e); }

    protected override void OnMouseUp(MouseEventArgs e)
    { _drag = false; base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float t = _max > _min ? (_value - _min) / (float)(_max - _min) : 0f;
        int cy = Height / 2;
        int x0 = 8, x1 = Width - 8;
        int hx = x0 + (int)(t * (x1 - x0));

        using (var back = new Pen(ModernUI.TrackOff, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(back, x0, cy, x1, cy);
        using (var fill = new Pen(ModernUI.Accent, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(fill, x0, cy, hx, cy);
        using (var knob = new SolidBrush(Color.White))
            g.FillEllipse(knob, hx - 7, cy - 7, 14, 14);
    }
}