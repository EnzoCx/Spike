using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Spike.Desktop;

/// <summary>Native screen coordinates keep snapping independent of WPF's logical units.
/// Only this application's window is moved. Work areas exclude each monitor's taskbar.</summary>
internal sealed partial class OverlayPlacement
{
    private readonly Window window;
    private readonly Func<bool> snapping;
    private readonly Action save;
    private readonly Func<Point?> dragPointer;
    private Vector? dragOffset;
    private HwndSource? source;
    private nint handle;
    private bool moving, closed;
    private double Scale => handle == 0 ? 1 : Math.Max(96, GetDpiForWindow(handle)) / 96d;

    public OverlayPlacement(Window window, Func<bool> snapping, Action save, Func<Point?>? dragPointer = null)
    {
        this.window = window; this.snapping = snapping; this.save = save;
        this.dragPointer = dragPointer ?? ReadDragPointer;
        window.SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(window).Handle;
            source = HwndSource.FromHwnd(handle); source?.AddHook(ProcessMessage);
            EnsureVisible();
        };
        window.Closed += (_, _) => { closed = true; source?.RemoveHook(ProcessMessage); source = null; handle = 0; };
    }

    private nint ProcessMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x0231) { moving = true; dragOffset = null; } // WM_ENTERSIZEMOVE
        if (message == 0x0216 && lParam != 0) // WM_MOVING: modify the proposed rectangle, never the pointer.
        {
            var proposed = Marshal.PtrToStructure<NativeRect>(lParam).Rect;
            if (dragPointer() is { } pointer)
            {
                // Keep the original grab offset: native proposals can start from the last snapped
                // position, which otherwise discards each small movement away from an edge.
                dragOffset ??= pointer - proposed.Location;
                proposed.Location = pointer - dragOffset.Value;
            }
            else dragOffset = null; // Preserve native keyboard movement and cursor-read failure fallback.
            var result = Snap(proposed, WorkArea(NativeRect.From(proposed)), 6 * Scale, snapping() && GetKeyState(0x10) >= 0);
            Marshal.StructureToPtr(NativeRect.From(result), lParam, false);
            handled = true; return 1;
        }
        if (message == 0x0232) { moving = false; dragOffset = null; save(); } // WM_EXITSIZEMOVE: preserve free placement when Shift bypassed snapping.
        if (message is 0x007E or 0x02E0 || (message == 0x001A && wParam == 0x002F))
            _ = window.Dispatcher.BeginInvoke(() => { if (!closed) { EnsureVisible(); save(); } });
        return 0;
    }

    private static Point? ReadDragPointer() => GetKeyState(0x01) < 0 && GetCursorPos(out var point)
        ? new Point(point.X, point.Y) : null;

    private Rect Bounds => handle != 0 && GetWindowRect(handle, out var rect) ? rect.Rect : new Rect(window.Left, window.Top, window.Width, window.Height);
    private Rect Area(Rect bounds) => handle == 0 ? SystemParameters.WorkArea : WorkArea(NativeRect.From(bounds));
    private static Rect WorkArea(NativeRect bounds)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfo(MonitorFromRect(ref bounds, 2), ref info) ? info.Work.Rect : bounds.Rect;
    }
    private void Move(Rect bounds)
    {
        if (handle == 0) { window.Left = bounds.Left; window.Top = bounds.Top; }
        else SetWindowPos(handle, 0, (int)Math.Round(bounds.Left), (int)Math.Round(bounds.Top), 0, 0, 0x0015); // NOACTIVATE | NOZORDER | NOSIZE
    }
    public void EnsureVisible()
    {
        if (closed || moving) return;
        var bounds = Bounds; Move(KeepVisible(bounds, Area(bounds)));
    }
    public void Place(string corner, Window? reference = null)
    {
        var bounds = Bounds; var work = Area(bounds);
        if (reference is not null && new WindowInteropHelper(reference).Handle is var owner && owner != 0 && GetWindowRect(owner, out var ownerBounds))
            work = WorkArea(ownerBounds);
        var x = corner.Contains("Left", StringComparison.Ordinal) ? work.Left : corner.Contains("Right", StringComparison.Ordinal) ? work.Right - bounds.Width : work.Left + (work.Width - bounds.Width) / 2;
        var y = corner.StartsWith("top", StringComparison.Ordinal) ? work.Top : corner.StartsWith("bottom", StringComparison.Ordinal) ? work.Bottom - bounds.Height : work.Top + (work.Height - bounds.Height) / 2;
        Move(KeepVisible(new Rect(x, y, bounds.Width, bounds.Height), work)); save();
    }
    public void SetHeight(double desired)
    {
        if (closed || moving) return; // Do not resize under the mouse during a native drag.
        // Before HWND creation, Left/Top are saved WPF coordinates: preserve the selected monitor.
        if (handle == 0) { window.Height = Math.Clamp(desired, window.MinHeight, window.MaxHeight); return; }
        var before = Bounds; var work = Area(before); var scale = Scale;
        var height = Math.Clamp(desired, window.MinHeight, Math.Max(window.MinHeight, Math.Min(window.MaxHeight, work.Height / scale)));
        if (Math.Abs(window.Height - height) < .1) return;
        window.Height = height;
        Move(AfterHeight(before, height * scale, work, 2 * scale));
    }

    internal static Rect Snap(Rect bounds, Rect work, double threshold, bool enabled)
    {
        if (!enabled) return bounds;
        var x = bounds.Left; var y = bounds.Top;
        if (Math.Abs(bounds.Left - work.Left) <= threshold) x = work.Left;
        else if (Math.Abs(bounds.Right - work.Right) <= threshold) x = work.Right - bounds.Width;
        if (Math.Abs(bounds.Top - work.Top) <= threshold) y = work.Top;
        else if (Math.Abs(bounds.Bottom - work.Bottom) <= threshold) y = work.Bottom - bounds.Height;
        return new Rect(x, y, bounds.Width, bounds.Height);
    }
    internal static Rect KeepVisible(Rect bounds, Rect work) => new(
        Math.Clamp(bounds.Left, work.Left, Math.Max(work.Left, work.Right - bounds.Width)),
        Math.Clamp(bounds.Top, work.Top, Math.Max(work.Top, work.Bottom - bounds.Height)), bounds.Width, bounds.Height);
    internal static Rect AfterHeight(Rect bounds, double height, Rect work, double tolerance)
    {
        var top = Math.Abs(bounds.Bottom - work.Bottom) <= tolerance ? work.Bottom - height : bounds.Top;
        return KeepVisible(new Rect(bounds.Left, top, bounds.Width, height), work);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rect Rect => new(Left, Top, Math.Max(0, Right - Left), Math.Max(0, Bottom - Top));
        public static NativeRect From(Rect rect) => new() { Left = (int)Math.Round(rect.Left), Top = (int)Math.Round(rect.Top), Right = (int)Math.Round(rect.Right), Bottom = (int)Math.Round(rect.Bottom) };
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern nint MonitorFromRect(ref NativeRect rect, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(nint hwnd, out NativeRect rect);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
}
