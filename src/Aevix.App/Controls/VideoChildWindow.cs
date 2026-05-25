using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Aevix_App.Controls;

/// <summary>
/// A bare Win32 child <c>HWND</c> that libVLC can paint into, parented to
/// the WinUI 3 main window. Necessary because WinUI 3 renders the whole
/// frame through DirectComposition — handing libVLC the WinUI HWND results
/// in the video being painted *behind* the WinUI compositor and never
/// shown.
///
/// Usage:
///   - Construct one. Pass the WinUI <see cref="Window"/> as the parent.
///   - Set <see cref="LayoutTarget"/> to a XAML element whose size /
///     position the surface should mirror.
///   - Pass <see cref="Hwnd"/> to libVLC's <c>MediaPlayer.Hwnd</c>.
///   - Call <see cref="Dispose"/> when the player page unloads.
/// </summary>
public sealed class VideoChildWindow : IDisposable
{
    private const string ClassName = "AevixVideoChild";
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_HIDEWINDOW = 0x0080;

    private static IntPtr _hInstance;
    private static IntPtr _hbrBlack;
    private static WndProcDelegate? _wndProc;
    private static ushort _classAtom;

    private readonly IntPtr _parentHwnd;
    private FrameworkElement? _layoutTarget;
    private bool _disposed;

    public IntPtr Hwnd { get; }

    public VideoChildWindow(Window parent)
    {
        _parentHwnd = WinRT.Interop.WindowNative.GetWindowHandle(parent);
        EnsureClassRegistered();

        // Create at (0,0) zero-sized — Resize() will reposition once XAML lays out.
        Hwnd = CreateWindowEx(
            dwExStyle: 0,
            lpClassName: ClassName,
            lpWindowName: "AevixVideoSurface",
            dwStyle: WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
            x: 0, y: 0, nWidth: 0, nHeight: 0,
            hWndParent: _parentHwnd,
            hMenu: IntPtr.Zero,
            hInstance: _hInstance,
            lpParam: IntPtr.Zero);
        if (Hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"CreateWindowEx failed (error {Marshal.GetLastWin32Error()}).");
        }
    }

    /// <summary>
    /// Mirror the size and on-screen position of the given XAML element.
    /// Subscribes to <see cref="FrameworkElement.SizeChanged"/> so the
    /// surface tracks layout updates (resize, splitter drags, etc.).
    /// </summary>
    public void Track(FrameworkElement target)
    {
        Untrack();
        _layoutTarget = target;
        target.SizeChanged += OnLayoutChanged;
        target.LayoutUpdated += OnLayoutUpdated;
        Reposition();
    }

    public void Untrack()
    {
        if (_layoutTarget is null) return;
        _layoutTarget.SizeChanged -= OnLayoutChanged;
        _layoutTarget.LayoutUpdated -= OnLayoutUpdated;
        _layoutTarget = null;
    }

    public void Show() => SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW | 0x0001 /* SWP_NOSIZE */ | 0x0002 /* SWP_NOMOVE */);
    public void Hide() => SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE | SWP_HIDEWINDOW | 0x0001 | 0x0002);

    private void OnLayoutChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) => Reposition();
    private void OnLayoutUpdated(object? sender, object e) => Reposition();

    private void Reposition()
    {
        if (_layoutTarget is null || Hwnd == IntPtr.Zero) return;
        // Translate the element's top-left from window-relative DIPs to
        // physical pixels in the parent HWND's client area.
        var dpi = _layoutTarget.XamlRoot?.RasterizationScale ?? 1.0;
        var win = _layoutTarget.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
        var w = (int)(_layoutTarget.ActualWidth * dpi);
        var h = (int)(_layoutTarget.ActualHeight * dpi);
        var x = (int)(win.X * dpi);
        var y = (int)(win.Y * dpi);
        if (w <= 0 || h <= 0) return;
        SetWindowPos(Hwnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Untrack();
        if (Hwnd != IntPtr.Zero)
        {
            DestroyWindow(Hwnd);
        }
    }

    // -------- Win32 plumbing ---------------------------------------------

    private static void EnsureClassRegistered()
    {
        if (_classAtom != 0) return;
        _hInstance = GetModuleHandle(null);
        _hbrBlack = CreateSolidBrush(0x00000000); // black backdrop while VLC is initialising
        _wndProc = DefWindowProc;
        var wcex = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = 0,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = _hInstance,
            hIcon = IntPtr.Zero,
            hCursor = IntPtr.Zero,
            hbrBackground = _hbrBlack,
            lpszMenuName = null,
            lpszClassName = ClassName,
            hIconSm = IntPtr.Zero,
        };
        _classAtom = RegisterClassEx(ref wcex);
        if (_classAtom == 0)
        {
            var err = Marshal.GetLastWin32Error();
            // ERROR_CLASS_ALREADY_EXISTS (1410) is fine — we'll reuse it.
            if (err != 1410) throw new InvalidOperationException($"RegisterClassEx failed ({err}).");
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string  lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName, string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint crColor);
}
