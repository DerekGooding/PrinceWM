using System.Runtime.InteropServices;
using static PrinceWM.Native.NativeMethods;

namespace PrinceWM.Native;

internal sealed class AltTabHook : IDisposable
{

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;

    public bool OverlayOpen { get; set; }

    public const int ModAlt = 1, ModCtrl = 2, ModShift = 4, ModWin = 8;

    public int SummonMods { get; set; } = ModAlt;
    public int SummonKey { get; set; } = 0x09;
    public int CommitKey { get; set; } = 0x0D;
    public int CancelKey { get; set; } = 0x1B;
    public int MoveUpKey { get; set; } = 0x26;
    public int MoveDownKey { get; set; } = 0x28;
    public int MoveLeftKey { get; set; } = 0x25;
    public int MoveRightKey { get; set; } = 0x27;

    public bool Capturing { get; set; }
    public event Action<int, int>? HotkeyCaptured;

    public event Action<bool>? AltTabPressed;

    public event Action? ScreenshotKey;

    private const uint VK_SNAPSHOT = 0x2C;
    private const uint VK_CONTROL = 0x11;
    private const uint VK_SHIFT = 0x10;
    private const uint VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    public event Action<NavKey, bool>? NavPressed;

    public AltTabHook()
    {
        _proc = HookCallback;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to install low-level keyboard hook.");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var hook = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        if ((hook.flags & LLKHF_INJECTED) != 0)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        int msg = (int)wParam;

        if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && OverlayOpen)
        {
            var up = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (up.vkCode == VK_SNAPSHOT) { ScreenshotKey?.Invoke(); return 1; }
        }

        if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            uint vk = data.vkCode;

            if (Capturing)
            {
                if (IsModifier(vk)) return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                Capturing = false;
                HotkeyCaptured?.Invoke(CurrentMods(), (int)vk);
                return 1;
            }

            if (vk == VK_SNAPSHOT && OverlayOpen) return 1;

            if ((int)vk == SummonKey && ModsHeld(SummonMods))
            {
                bool reverse = (SummonMods & ModShift) == 0 && IsDown(VK_SHIFT);
                AltTabPressed?.Invoke(reverse);
                return 1;
            }

            if (OverlayOpen)
            {
                int k = (int)vk;
                NavKey nav =
                    k == CancelKey ? NavKey.Escape :
                    k == CommitKey ? NavKey.Enter :
                    k == MoveLeftKey ? NavKey.Left :
                    k == MoveUpKey ? NavKey.Up :
                    k == MoveRightKey ? NavKey.Right :
                    k == MoveDownKey ? NavKey.Down :
                    NavKey.None;
                if (nav != NavKey.None)
                {
                    NavPressed?.Invoke(nav, IsDown(VK_SHIFT));
                    return 1;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsDown(uint vk) => (GetAsyncKeyState((int)vk) & 0x8000) != 0;

    private static bool IsModifier(uint vk) =>
        vk == VK_MENU || vk == VK_CONTROL || vk == VK_SHIFT || vk == VK_LWIN || vk == VK_RWIN ||
        (vk >= 0xA0 && vk <= 0xA5);

    private static int CurrentMods()
    {
        int m = 0;
        if (IsDown(VK_MENU)) m |= ModAlt;
        if (IsDown(VK_CONTROL)) m |= ModCtrl;
        if (IsDown(VK_SHIFT)) m |= ModShift;
        if (IsDown(VK_LWIN) || IsDown(VK_RWIN)) m |= ModWin;
        return m;
    }

    private static bool ModsHeld(int mods)
    {
        if ((mods & ModAlt) != 0 && !IsDown(VK_MENU)) return false;
        if ((mods & ModCtrl) != 0 && !IsDown(VK_CONTROL)) return false;
        if ((mods & ModShift) != 0 && !IsDown(VK_SHIFT)) return false;
        if ((mods & ModWin) != 0 && !(IsDown(VK_LWIN) || IsDown(VK_RWIN))) return false;
        return mods != 0;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
