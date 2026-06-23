using PrinceWM.Services;
using System.Diagnostics;

namespace PrinceWM.UI;

internal sealed class SettingsForm : Form, IMessageFilter
{
    private readonly Theme _theme;
    private readonly AltTabHook _hook;
    private int _tab;
    private Action<int, int>? _capApply;
    private bool _capRequireMod;
    private Label? _capBox;

    public event Action? Changed;

    public event Action? RearrangeRequested;

    private const int PanelW = 348;
    private int Cw => PanelW - 40;
    private Panel _content = null!;

    private readonly System.Windows.Forms.Timer _slide = new() { Interval = 15 };
    private readonly Stopwatch _slideClock = new();
    private int _targetLeft, _startLeft;

    public SettingsForm(Theme theme, AltTabHook hook)
    {
        _theme = theme;
        _hook = hook;
        _hook.HotkeyCaptured += OnHotkeyCaptured;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(22, 22, 25);
        ForeColor = ModernUI.Text;
        Font = new Font("Segoe UI", 9f);

        var scr = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 800);
        Size = new Size(PanelW, scr.Height);
        _targetLeft = scr.Right - PanelW;
        Location = new Point(scr.Right, scr.Top);

        _slide.Tick += (_, _) => SlideStep();
        Build();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ModernUI.Acrylic(Handle, 0xC0121620);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _startLeft = Left;
        _slideClock.Restart();
        _slide.Start();
        Application.AddMessageFilter(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Application.RemoveMessageFilter(this);
        _hook.HotkeyCaptured -= OnHotkeyCaptured;
        _hook.Capturing = false;
        base.OnFormClosed(e);
    }

    private void OnHotkeyCaptured(int mods, int key)
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke(() =>
            {
                var apply = _capApply;
                _capApply = null;
                if (apply == null) return;
                if (_capRequireMod && mods == 0)
                {
                    if (_capBox != null) _capBox.Text = "Use a modifier";
                    return;
                }
                apply(mods, key);
                Emit();
                Build();
            });
        }
        catch { }
    }

    public bool PreFilterMessage(ref Message m)
    {
        const int WM_MOUSEWHEEL = 0x020A;
        if (m.Msg != WM_MOUSEWHEEL || !Visible || _content == null) return false;
        if (!ClientRectangle.Contains(PointToClient(Cursor.Position))) return false;
        int delta = (short)((long)m.WParam >> 16);
        int cur = -_content.AutoScrollPosition.Y;
        _content.AutoScrollPosition = new Point(0, cur - delta);
        return true;
    }

    private void SlideStep()
    {
        const float dur = 0.24f;
        float t = (float)Math.Clamp(_slideClock.Elapsed.TotalSeconds / dur, 0, 1);
        float e = 1f - MathF.Pow(1f - t, 3f);
        Left = (int)(_startLeft + (_targetLeft - _startLeft) * e);
        if (t >= 1f) _slide.Stop();
    }

    private void Build()
    {
        _hook.Capturing = false;
        Controls.Clear();

        Controls.Add(new Panel { Left = 0, Top = 0, Width = 3, Height = ClientSize.Height, BackColor = ModernUI.Accent });

        var header = new Panel { Left = 0, Top = 0, Width = ClientSize.Width, Height = 54, BackColor = Color.Transparent };
        int tx = 22;
        tx += AddTab(header, "Customize", 0, tx) + 18;
        AddTab(header, "Hotkeys", 1, tx);
        var close = new Label { Text = "✕", Left = ClientSize.Width - 42, Top = 16, Width = 24, Height = 24, ForeColor = ModernUI.SubText, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
        close.MouseEnter += (_, _) => close.ForeColor = ModernUI.Text;
        close.MouseLeave += (_, _) => close.ForeColor = ModernUI.SubText;
        close.Click += (_, _) => Close();
        header.Controls.Add(close);
        Controls.Add(header);

        int vsb = SystemInformation.VerticalScrollBarWidth;
        _content = new Panel
        {
            Left = 0,
            Top = 54,
            Width = ClientSize.Width + vsb + 2,
            Height = ClientSize.Height - 54,
            AutoScroll = true,
            BackColor = Color.Transparent,
        };
        Controls.Add(_content);

        int y = 10;
        if (_tab == 0) BuildCustomize(ref y);
        else BuildHotkeys(ref y);
    }

    private int AddTab(Panel header, string text, int index, int left)
    {
        bool active = _tab == index;
        var lbl = new Label
        {
            Text = text,
            Left = left,
            Top = 17,
            AutoSize = true,
            ForeColor = active ? ModernUI.Text : ModernUI.SubText,
            Font = new Font("Segoe UI Semibold", 12f),
            Cursor = Cursors.Hand,
        };
        lbl.Click += (_, _) => { if (_tab != index) { _tab = index; Build(); } };
        header.Controls.Add(lbl);
        int w = TextRenderer.MeasureText(text, lbl.Font).Width;
        if (active)
            header.Controls.Add(new Panel { Left = left, Top = 44, Width = w, Height = 2, BackColor = ModernUI.Accent });
        return w;
    }

    private void BuildCustomize(ref int y)
    {
        Section("Colors", ref y);
        Swatch("Background", () => _theme.Background, v => _theme.Background = v, ref y);
        Swatch("Accent / selection", () => _theme.Accent, v => _theme.Accent = v, ref y);
        Swatch("Dot grid", () => _theme.Dot, v => _theme.Dot = v, ref y);
        Swatch("Tile border", () => _theme.Border, v => _theme.Border = v, ref y);

        Section("Canvas", ref y);
        Toggle("Show dot grid", () => _theme.ShowDots, v => _theme.ShowDots = v, ref y);
        Slider("Dot size", 6, 50, () => _theme.DotSize, v => _theme.DotSize = v, ref y);
        Slider("Dot spacing", 40, 200, () => _theme.DotSpacing, v => _theme.DotSpacing = v, ref y);
        Toggle("Show app name on top", () => _theme.ShowTitles, v => _theme.ShowTitles = v, ref y);
        Toggle("Show hint bar", () => _theme.ShowHints, v => _theme.ShowHints = v, ref y);
        Toggle("Window shadows", () => _theme.WindowShadows, v => _theme.WindowShadows = v, ref y);
        Toggle("Show paint tools", () => _theme.ShowPaintButton, v => _theme.ShowPaintButton = v, ref y);

        Section("Wallpaper", ref y);
        Toggle("Use desktop wallpaper", () => _theme.UseWallpaper, v => _theme.UseWallpaper = v, ref y);
        Toggle("Show desktop icons", () => _theme.ShowDesktopIcons, v => _theme.ShowDesktopIcons = v, ref y);
        Slider("Blur", 0, 40, () => _theme.BlurAmount, v => _theme.BlurAmount = v, ref y);
        Swatch("Tint color", () => _theme.TintColor, v => _theme.TintColor = v, ref y);
        Slider("Tint strength", 0, 100, () => _theme.TintStrength, v => _theme.TintStrength = v, ref y);

        Section("Tiles", ref y);
        Toggle("Tile outlines", () => _theme.ShowTileOutline, v => _theme.ShowTileOutline = v, ref y);
        Slider("Outline thickness", 4, 40, () => _theme.BorderThickness, v => _theme.BorderThickness = v, ref y);
        Slider("Highlight glow", 0, 100, () => _theme.GlowIntensity, v => _theme.GlowIntensity = v, ref y);
        Slider("Corner radius", 0, 24, () => _theme.CornerRadius, v => _theme.CornerRadius = v, ref y);
        Slider("Tile spacing", 16, 160, () => _theme.TileGap, v => _theme.TileGap = v, ref y);

        Section("Animation", ref y);
        Slider("Animation speed", 50, 160, () => _theme.AnimSpeed, v => _theme.AnimSpeed = v, ref y);
        Slider("Collision settle", 40, 200, () => _theme.SettleSpeed, v => _theme.SettleSpeed = v, ref y);
        Slider("Hover lift", 0, 80, () => _theme.HoverLift, v => _theme.HoverLift = v, ref y);
        Toggle("Remember zoom level", () => _theme.RememberZoom, v => _theme.RememberZoom = v, ref y);

        Section("Layout", ref y);
        Toggle("Drag-to-tile windows", () => _theme.DragToTile, v => _theme.DragToTile = v, ref y, beta: true);
        Button("Rearrange all tiles", ModernUI.Accent, () => { RearrangeRequested?.Invoke(); Close(); }, ref y);

        Section("System", ref y);
        Toggle("Start with Windows", AutoStart.IsEnabled, AutoStart.SetEnabled, ref y);

        y += 6;
        Button("Reset to defaults", ModernUI.SubText, ResetDefaults, ref y);
        y += 16;
    }

    private void BuildHotkeys(ref int y)
    {
        var d = new Theme();

        _content.Controls.Add(new Label
        {
            Text = "Click a bind, press the keys.  ⟲ resets it.",
            Left = 22,
            Top = y,
            AutoSize = true,
            ForeColor = ModernUI.SubText,
            Font = new Font("Segoe UI", 8f),
        });
        y += 24;

        Section("Activation", ref y);
        KeybindRow("Open canvas",
            () => (_theme.SummonMods, _theme.SummonKey),
            (m, k) => { _theme.SummonMods = m; _theme.SummonKey = k; },
            () => { _theme.SummonMods = d.SummonMods; _theme.SummonKey = d.SummonKey; },
            true, ref y);

        Section("In-canvas", ref y);
        KeybindRow("Confirm / switch", () => (0, _theme.CommitKey), (_, k) => _theme.CommitKey = k, () => _theme.CommitKey = d.CommitKey, false, ref y);
        KeybindRow("Cancel / close", () => (0, _theme.CancelKey), (_, k) => _theme.CancelKey = k, () => _theme.CancelKey = d.CancelKey, false, ref y);
        KeybindRow("Move up", () => (0, _theme.MoveUpKey), (_, k) => _theme.MoveUpKey = k, () => _theme.MoveUpKey = d.MoveUpKey, false, ref y);
        KeybindRow("Move down", () => (0, _theme.MoveDownKey), (_, k) => _theme.MoveDownKey = k, () => _theme.MoveDownKey = d.MoveDownKey, false, ref y);
        KeybindRow("Move left", () => (0, _theme.MoveLeftKey), (_, k) => _theme.MoveLeftKey = k, () => _theme.MoveLeftKey = d.MoveLeftKey, false, ref y);
        KeybindRow("Move right", () => (0, _theme.MoveRightKey), (_, k) => _theme.MoveRightKey = k, () => _theme.MoveRightKey = d.MoveRightKey, false, ref y);

        y += 8;
        Button("Reset all keybinds", ModernUI.SubText, ResetKeybinds, ref y);
        y += 16;
    }

    private void ResetKeybinds()
    {
        var d = new Theme();
        _theme.SummonMods = d.SummonMods; _theme.SummonKey = d.SummonKey;
        _theme.CommitKey = d.CommitKey; _theme.CancelKey = d.CancelKey;
        _theme.MoveUpKey = d.MoveUpKey; _theme.MoveDownKey = d.MoveDownKey;
        _theme.MoveLeftKey = d.MoveLeftKey; _theme.MoveRightKey = d.MoveRightKey;
        Emit();
        Build();
    }

    private void KeybindRow(string label, Func<(int mods, int key)> get, Action<int, int> apply,
        Action reset, bool requireMod, ref int y)
    {
        var cur = get();
        _content.Controls.Add(new Label { Text = label, Left = 22, Top = y + 7, AutoSize = true, ForeColor = ModernUI.Text });

        var box = new Label
        {
            Left = Cw - 150 - 30,
            Top = y,
            Width = 150,
            Height = 30,
            Text = DescribeHotkey(cur.mods, cur.key),
            ForeColor = ModernUI.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(36, 255, 255, 255),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f),
        };
        box.Click += (_, _) =>
        {
            box.Text = "Press keys…";
            _capApply = apply;
            _capRequireMod = requireMod;
            _capBox = box;
            _hook.Capturing = true;
        };
        _content.Controls.Add(box);

        var rst = new Label
        {
            Text = "⟲",
            Left = Cw - 26,
            Top = y + 3,
            Width = 24,
            Height = 24,
            ForeColor = ModernUI.SubText,
            Font = new Font("Segoe UI", 13f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
        };
        rst.MouseEnter += (_, _) => rst.ForeColor = ModernUI.Text;
        rst.MouseLeave += (_, _) => rst.ForeColor = ModernUI.SubText;
        rst.Click += (_, _) => { reset(); Emit(); Build(); };
        _content.Controls.Add(rst);

        y += 40;
    }

    private void ResetDefaults()
    {
        var d = new Theme();
        _theme.Background = d.Background; _theme.Accent = d.Accent; _theme.Dot = d.Dot; _theme.Border = d.Border;
        _theme.ShowDots = d.ShowDots; _theme.ShowHints = d.ShowHints; _theme.ShowTitles = d.ShowTitles;
        _theme.ShowTileOutline = d.ShowTileOutline; _theme.WindowShadows = d.WindowShadows;
        _theme.ShowPaintButton = d.ShowPaintButton; _theme.RememberZoom = d.RememberZoom;
        _theme.DragToTile = d.DragToTile;
        _theme.GlowIntensity = d.GlowIntensity;
        _theme.CornerRadius = d.CornerRadius; _theme.TileGap = d.TileGap;
        _theme.HoverLift = d.HoverLift; _theme.AnimSpeed = d.AnimSpeed;
        _theme.SettleSpeed = d.SettleSpeed; _theme.BorderThickness = d.BorderThickness;
        _theme.DotSize = d.DotSize; _theme.DotSpacing = d.DotSpacing;
        _theme.UseWallpaper = d.UseWallpaper; _theme.BlurAmount = d.BlurAmount;
        _theme.TintColor = d.TintColor; _theme.TintStrength = d.TintStrength;
        _theme.SummonMods = d.SummonMods; _theme.SummonKey = d.SummonKey;
        Emit();
        Build();
    }

    private static string DescribeHotkey(int mods, int key)
    {
        var parts = new List<string>();
        if ((mods & AltTabHook.ModCtrl) != 0) parts.Add("Ctrl");
        if ((mods & AltTabHook.ModAlt) != 0) parts.Add("Alt");
        if ((mods & AltTabHook.ModShift) != 0) parts.Add("Shift");
        if ((mods & AltTabHook.ModWin) != 0) parts.Add("Win");
        parts.Add(KeyName(key));
        return string.Join(" + ", parts);
    }

    private static string KeyName(int vk) => (Keys)vk switch
    {
        Keys.Tab => "Tab",
        Keys.Space => "Space",
        Keys.Return => "Enter",
        Keys.Escape => "Esc",
        Keys.Up => "Up",
        Keys.Down => "Down",
        Keys.Left => "Left",
        Keys.Right => "Right",
        Keys.Oemtilde => "`",
        Keys.OemMinus => "-",
        Keys.Oemplus => "=",
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (vk - (int)Keys.D0))).ToString(),
        var k => k.ToString(),
    };

    private void Section(string text, ref int y)
    {
        y += 12;

        if (y > 24)
            _content.Controls.Add(new Panel { Left = 20, Top = y - 8, Width = Cw, Height = 1, BackColor = Color.FromArgb(48, 255, 255, 255) });
        _content.Controls.Add(new Label
        {
            Text = text.ToUpperInvariant(),
            Left = 22,
            Top = y,
            AutoSize = true,
            ForeColor = ModernUI.Accent,
            Font = new Font("Segoe UI Semibold", 8.5f),
        });
        y += 28;
    }

    private void Swatch(string label, Func<int> get, Action<int> set, ref int y)
    {
        _content.Controls.Add(new Label { Text = label, Left = 22, Top = y + 4, AutoSize = true, ForeColor = ModernUI.Text });
        var sw = new SwatchButton { Left = Cw - 64, Top = y, Color = FromRgb(get()) };
        sw.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = FromRgb(get()), FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK) { set(ToRgb(dlg.Color)); sw.Color = dlg.Color; Emit(); }
        };
        _content.Controls.Add(sw);
        y += 38;
    }

    private void Toggle(string label, Func<bool> get, Action<bool> set, ref int y, bool beta = false)
    {
        var lbl = new Label { Text = label, Left = 22, Top = y + 2, AutoSize = true, ForeColor = ModernUI.Text };
        _content.Controls.Add(lbl);
        if (beta)
        {
            var sz = TextRenderer.MeasureText(label, lbl.Font);
            _content.Controls.Add(new Label
            {
                Text = "BETA",
                Left = 22 + sz.Width + 6,
                Top = y + 3,
                AutoSize = true,
                ForeColor = Color.FromArgb(18, 20, 26),
                BackColor = ModernUI.Accent,
                Font = new Font("Segoe UI Semibold", 7f),
                Padding = new Padding(4, 1, 4, 1),
            });
        }
        var t = new FlatToggle { Left = Cw - 46, Top = y, Checked = get() };
        t.CheckedChanged += (_, _) => { set(t.Checked); Emit(); };
        _content.Controls.Add(t);
        y += 34;
    }

    private void Slider(string label, int min, int max, Func<int> get, Action<int> set, ref int y)
    {
        _content.Controls.Add(new Label { Text = label, Left = 22, Top = y, AutoSize = true, ForeColor = ModernUI.Text });
        var val = new Label { Text = get().ToString(), Left = Cw - 44, Top = y, Width = 40, ForeColor = ModernUI.SubText, TextAlign = ContentAlignment.MiddleRight };
        _content.Controls.Add(val);
        y += 22;
        var s = new FlatSlider { Left = 20, Top = y, Width = Cw - 24, Minimum = min, Maximum = max, Value = Math.Clamp(get(), min, max) };
        s.ValueChanged += (_, _) => { set(s.Value); val.Text = s.Value.ToString(); Emit(); };
        _content.Controls.Add(s);
        y += 34;
    }

    private void Button(string text, Color color, Action onClick, ref int y)
    {
        var b = new Label
        {
            Text = text,
            Left = 20,
            Top = y,
            Width = Cw - 24,
            Height = 34,
            ForeColor = color,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(36, 255, 255, 255),
        };
        b.MouseEnter += (_, _) => b.BackColor = Color.FromArgb(64, 255, 255, 255);
        b.MouseLeave += (_, _) => b.BackColor = Color.FromArgb(36, 255, 255, 255);
        b.Click += (_, _) => onClick();
        _content.Controls.Add(b);
        y += 42;
    }

    private void Emit()
    { Changed?.Invoke(); ThemeStore.Save(_theme); }

    private static Color FromRgb(int rgb) => Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

    private static int ToRgb(Color c) => (c.R << 16) | (c.G << 8) | c.B;
}