using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Aevix_App.Controls;

/// <summary>
/// A borderless top-level <c>HWND</c> that libVLC can paint into, *owned by*
/// the WinUI 3 main window (not parented as a child).
///
/// Why not a WS_CHILD HWND? WinUI 3 renders its entire window through
/// DirectComposition. Child HWNDs of a WinUI 3 window paint correctly at
/// the OS level — but the WinUI compositor draws *over* them, so the
/// video pixels exist but are invisible. The well-known fix is to use a
/// borderless WS_POPUP top-level window with the WinUI HWND as its
/// *owner*: owned popups render in their own Z-layer above the
/// compositor and stay grouped with the main window for activation /
/// taskbar purposes.
///
/// Usage:
///   - Construct with the WinUI <see cref="Window"/> as owner.
///   - Call <see cref="Track"/> with a XAML element whose on-screen
///     rectangle the overlay should mirror.
///   - Pass <see cref="Hwnd"/> to libVLC's <c>MediaPlayer.Hwnd</c>.
///   - <see cref="Dispose"/> when the player page unloads.
/// </summary>
public sealed class VideoChildWindow : IDisposable
{
    private const string ClassName = "AevixVideoOverlay";

    private const int WS_POPUP        = unchecked((int)0x80000000);
    private const int WS_VISIBLE      = 0x10000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private const uint SWP_NOZORDER  = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_HIDEWINDOW = 0x0080;
    private const uint SWP_NOSIZE    = 0x0001;
    private const uint SWP_NOMOVE    = 0x0002;

    private static IntPtr _hInstance;
    private static IntPtr _hbrBlack;
    private static WndProcDelegate? _wndProc;
    private static ushort _classAtom;

    private readonly IntPtr _ownerHwnd;
    private readonly Window _ownerWindow;
    private FrameworkElement? _layoutTarget;
    private bool _disposed;

    public IntPtr Hwnd { get; }

    public VideoChildWindow(Window owner)
    {
        _ownerWindow = owner;
        _ownerHwnd = WinRT.Interop.WindowNative.GetWindowHandle(owner);
        EnsureClassRegistered();

        // Top-level, borderless, owned by the WinUI window. WS_EX_TOOLWINDOW
        // keeps it out of the taskbar; WS_EX_NOACTIVATE means clicking it
        // doesn't steal focus from the WinUI window.
        Hwnd = CreateWindowEx(
            dwExStyle: (uint)(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE),
            lpClassName: ClassName,
            lpWindowName: "AevixVideoOverlay",
            dwStyle: unchecked((uint)(WS_POPUP | WS_VISIBLE | WS_CLIPSIBLINGS)),
            x: 0, y: 0, nWidth: 0, nHeight: 0,
            hWndParent: _ownerHwnd,
            hMenu: IntPtr.Zero,
            hInstance: _hInstance,
            lpParam: IntPtr.Zero);
        if (Hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"CreateWindowEx failed (error {Marshal.GetLastWin32Error()}).");
        }

        // Follow the owner window when it moves on screen.
        owner.AppWindow.Changed += OwnerAppWindowChanged;
    }

    /// <summary>
    /// Mirror the size and on-screen position of the given XAML element.
    /// Subscribes to its layout-change events plus the owner window's
    /// position-change so the overlay tracks both content scrolling and
    /// window dragging.
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

    public void Hide() => SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE | SWP_HIDEWINDOW | SWP_NOMOVE | SWP_NOSIZE);
    public void Show() { Reposition(); /* SetWindowPos with SWP_SHOWWINDOW happens in Reposition */ }

    private void OnLayoutChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) => Reposition();
    private void OnLayoutUpdated(object? sender, object e) => Reposition();
    private void OwnerAppWindowChanged(Microsoft.UI.Windowing.AppWindow s, Microsoft.UI.Windowing.AppWindowChangedEventArgs e)
    {
        if (e.DidPositionChange || e.DidSizeChange) Reposition();
    }

    private void Reposition()
    {
        if (_disposed || _layoutTarget is null || Hwnd == IntPtr.Zero) return;
        var root = _layoutTarget.XamlRoot;
        if (root is null) return;
        var dpi = root.RasterizationScale;
        var w = (int)(_layoutTarget.ActualWidth * dpi);
        var h = (int)(_layoutTarget.ActualHeight * dpi);
        if (w <= 0 || h <= 0) return;

        // Element coords relative to the XAML root (i.e. the WinUI window
        // client area, including the title bar since we extend into it).
        Windows.Foundation.Point inWindow;
        try
        {
            inWindow = _layoutTarget.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
        }
        catch
        {
            return; // not in the tree yet
        }

        // Convert to physical pixels then add the owner window's screen origin.
        var ownerPos = _ownerWindow.AppWindow.Position; // physical pixels
        var x = ownerPos.X + (int)(inWindow.X * dpi);
        var y = ownerPos.Y + (int)(inWindow.Y * dpi);

        SetWindowPos(Hwnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _ownerWindow.AppWindow.Changed -= OwnerAppWindowChanged; } catch { }
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
