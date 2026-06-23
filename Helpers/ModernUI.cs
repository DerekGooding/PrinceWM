using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PrinceWM.Helpers;

internal static class ModernUI
{
    public static readonly Color Accent = Color.FromArgb(208, 211, 216);
    public static readonly Color Text = Color.FromArgb(234, 236, 240);
    public static readonly Color SubText = Color.FromArgb(138, 142, 150);
    public static readonly Color TrackOff = Color.FromArgb(56, 58, 64);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void RoundCorners(IntPtr hwnd)
    {
        var pref = 2;
        DwmSetWindowAttribute(hwnd, 33, ref pref, sizeof(int));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void Acrylic(IntPtr hwnd, uint tintArgb)
    {
        uint a = (tintArgb >> 24) & 0xFF, r = (tintArgb >> 16) & 0xFF, g = (tintArgb >> 8) & 0xFF, b = tintArgb & 0xFF;
        var abgr = (a << 24) | (b << 16) | (g << 8) | r;

        var accent = new AccentPolicy { AccentState = 4, GradientColor = abgr };
        var size = Marshal.SizeOf(accent);
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new WindowCompositionAttributeData { Attribute = 19, Data = ptr, SizeOfData = size };
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}