using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static PrinceWM.Helpers.NativeMethods;

namespace PrinceWM.Helpers;

internal static class WindowScanner
{
    private static readonly HashSet<string> _classBlocklist = new(StringComparer.Ordinal)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Windows.UI.Core.CoreWindow",
        "XamlExplorerHostIslandWindow",
        "ForegroundStaging",
        "MSCTFIME UI",
        "Default IME",
    };

    public static List<WindowItem> Scan(IntPtr selfHwnd, bool includeMinimized = true,
    Dictionary<string, Vector2>? sizeCache = null)
    {
        var results = new List<WindowItem>();
        var shell = GetShellWindow();

        EnumWindows((hWnd, _) =>
        {
            if (hWnd == selfHwnd) return true;
            if (hWnd == shell) return true;
            if (!IsWindowVisible(hWnd)) return true;

            var len = GetWindowTextLength(hWnd);
            if (len == 0) return true;

            var exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            var toolWindow = (exStyle & WS_EX_TOOLWINDOW) != 0;
            var appWindow = (exStyle & WS_EX_APPWINDOW) != 0;
            if (toolWindow && !appWindow) return true;

            if (!appWindow)
            {
                var root = GetAncestor(hWnd, GA_ROOTOWNER);
                if (LastVisibleActivePopup(root) != hWnd) return true;
            }

            if (IsCloaked(hWnd)) return true;

            var cls = GetClass(hWnd);
            if (_classBlocklist.Contains(cls)) return true;

            var title = GetTitle(hWnd, len);
            if (string.IsNullOrWhiteSpace(title)) return true;

            var minimized = IsIconic(hWnd);
            if (minimized && !includeMinimized) return true;

            int x, y, w, h;
            if (minimized)
            {
                var wp = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
                if (GetWindowPlacement(hWnd, ref wp))
                {
                    var nr = wp.rcNormalPosition;
                    x = nr.Left; y = nr.Top; w = nr.Width; h = nr.Height;
                }
                else { x = 0; y = 0; w = 960; h = 600; }
            }
            else
            {
                RECT r;
                if (GetExtendedFrameBounds(hWnd, out var efb) && efb.Width > 0 && efb.Height > 0)
                    r = efb;
                else if (!GetWindowRect(hWnd, out r)) return true;
                x = r.Left; y = r.Top; w = r.Width; h = r.Height;
            }
            if (w <= 0 || h <= 0) { w = 960; h = 600; }

            var appKey = $"{ProcessNameOf(hWnd)}|{cls}";

            var size = new Vector2(w, h);
            if (sizeCache != null)
            {
                var sane = w >= 150 && h >= 120 && w <= 16000 && h <= 16000;
                var hasGood = sizeCache.TryGetValue(appKey, out var good);
                if (!minimized && sane) sizeCache[appKey] = size;
                else if (hasGood) size = good;
                else if (sane) sizeCache[appKey] = size;
            }

            results.Add(new WindowItem
            {
                Hwnd = hWnd,
                Title = title,
                AppKey = appKey,
                Key = $"{appKey}|{title}",
                WorldPos = new Vector2(x, y),
                WorldSize = size,
                IsMinimized = minimized,
            });
            return true;
        }, IntPtr.Zero);

        return results;
    }

    private static string ProcessNameOf(IntPtr hWnd)
    {
        try
        {
            GetWindowThreadProcessId(hWnd, out var pid);
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return "?"; }
    }

    private static IntPtr LastVisibleActivePopup(IntPtr window)
    {
        var cur = window;
        for (var i = 0; i < 32; i++)
        {
            var popup = GetLastActivePopup(cur);
            if (IsWindowVisible(popup)) return popup;
            if (popup == cur) return IntPtr.Zero;
            cur = popup;
        }
        return IntPtr.Zero;
    }

    private static bool IsCloaked(IntPtr hWnd)
    {
        var hr = DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int));
        return hr == 0 && cloaked != 0;
    }

    private static string GetTitle(IntPtr hWnd, int len)
    {
        var sb = new StringBuilder(len + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClass(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static bool Activate(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);

        // Make SetForegroundWindow take effect immediately and synchronously WITHOUT attaching
        // our input queue to the target's thread. Attaching is what made the switch crisp, but it
        // stalls if the target thread is busy (a game in its render loop) - that was the freeze.
        // Zeroing the foreground-lock timeout for the call grants the foreground change at once,
        // so the target is the foreground window before the overlay hides (no old-window flash),
        // with no thread attach to stall on. The timeout is restored right after.
        uint prevTimeout = 0;
        var gotTimeout = SystemParametersInfoGet(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref prevTimeout, 0);
        SystemParametersInfoSet(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0); // in-memory, no broadcast

        BringWindowToTop(hWnd);
        var ok = SetForegroundWindow(hWnd);

        if (gotTimeout)
            SystemParametersInfoSet(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, (IntPtr)prevTimeout, 0);

        return ok;
    }

    private static IntPtr GetForegroundWindowSafe() => ForegroundImport.GetForegroundWindow();

    private static class ForegroundImport
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}