using System;
using System.Runtime.InteropServices;

namespace sketchDeck.GlobalHooks;

public class MouseKeyboardHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private DateTime _lastLeftDownTime;
    private const int CLICK_THRESHOLD_MS = 100;
    private const int WM_MBUTTONDOWN = 0x0207;

    private const int WM_KEYDOWN = 0x0100;

    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private readonly HookProc? _mouseProc;
    private readonly HookProc? _keyboardProc;

    public event Action? LeftClick;
    public event Action? MiddleClick;
    public event Action? EnterPressed;
    public event Action? EscPressed;

    public MouseKeyboardHook()
    {
        _mouseProc = MouseCallback;
        _keyboardProc = KeyboardCallback;

        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
    }
    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out POINT p);
        return (p.X, p.Y);
    }
    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WM_LBUTTONDOWN) { _lastLeftDownTime = DateTime.Now; }
            else if (msg == WM_LBUTTONUP)
            {
                if ((DateTime.Now - _lastLeftDownTime).TotalMilliseconds <= CLICK_THRESHOLD_MS)
                {
                    LeftClick?.Invoke();
                }
            }
            else if (msg == WM_MBUTTONDOWN) MiddleClick?.Invoke();
        }
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }
    private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == 13) EnterPressed?.Invoke();
            else if (vkCode == 27) EscPressed?.Invoke();
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }
    public (byte R, byte G, byte B) GetPixelColor(int x, int y)
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        int pixel = GetPixel(hdc, x, y);
        _ = ReleaseDC(IntPtr.Zero, hdc);

        byte r = (byte)(pixel & 0x000000FF);
        byte g = (byte)((pixel & 0x0000FF00) >> 8);
        byte b = (byte)((pixel & 0x00FF0000) >> 16);

        return (r, g, b);
    }
    public void Dispose()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]
    private static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
