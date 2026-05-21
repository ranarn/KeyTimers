using System.Runtime.InteropServices;

namespace KeyTimers.Services;

/// <summary>
/// Installs a low-level keyboard hook (WH_KEYBOARD_LL) that monitors key-down events
/// system-wide. Keys are normally passed through unchanged; set <see cref="ShouldBlockKey"/>
/// to selectively consume specific key presses before they reach other applications.
/// </summary>
public sealed class KeyboardHookService : IDisposable
{
    // ── Win32 ─────────────────────────────────────────────────────────────────

    private const int  WH_KEYBOARD_LL  = 13;
    private const int  WM_KEYDOWN      = 0x0100;
    private const int  WM_SYSKEYDOWN   = 0x0104;
    private const uint LLKHF_INJECTED  = 0x10;   // key was injected via SendInput

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint   vkCode;
        public uint   scanCode;
        public uint   flags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    // ── State ─────────────────────────────────────────────────────────────────

    private IntPtr _hookHandle = IntPtr.Zero;

    // keep a strong reference so GC doesn't collect the delegate
    private readonly LowLevelKeyboardProc _proc;

    /// <summary>
    /// Raised for every key-down event. The argument is the virtual-key code.
    /// Fired after the block check, so the handler may safely change timer state.
    /// </summary>
    public event Action<uint>? KeyDown;

    /// <summary>
    /// Optional predicate called before a key is forwarded to the rest of the system.
    /// Return <c>true</c> to consume (block) the key; <c>false</c> to pass it through.
    /// Must be safe to call on the UI thread.
    /// </summary>
    public Func<uint, bool>? ShouldBlockKey { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    /// <summary>Installs the hook. Must be called on a thread with a message loop (the UI thread).</summary>
    public void Install()
    {
        if (_hookHandle != IntPtr.Zero) return;

        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module  = process.MainModule!;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
            GetModuleHandle(module.ModuleName), 0);

        if (_hookHandle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(
                System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                "Failed to install keyboard hook. KeyTimers will not respond to key presses.");
    }

    /// <summary>Removes the hook.</summary>
    public void Uninstall()
    {
        if (_hookHandle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    // ── Hook callback ─────────────────────────────────────────────────────────

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // skip keys we injected ourselves via SendInput (auto-resend feature)
            if ((kb.flags & LLKHF_INJECTED) != 0)
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

            // evaluate block decision against current state BEFORE HandleKeyDown changes it
            bool block = ShouldBlockKey?.Invoke(kb.vkCode) ?? false;

            KeyDown?.Invoke(kb.vkCode);

            if (block)
                return new IntPtr(1); // consumed — not forwarded to other applications
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() => Uninstall();
}
