using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DPSMeter.Desktop;

/// <summary>Native screen coordinates keep snapping independent of WPF's logical units.
/// Only this application's window is moved. Work areas exclude each monitor's taskbar.</summary>
internal sealed class OverlayPlacement
{
    private readonly Window window;
    private readonly Func<bool> snapping;
    private readonly Action save;
    private HwndSource? source;
    private nint handle;
    private bool moving, closed;
    private double Scale => handle == 0 ? 1 : Math.Max(96, GetDpiForWindow(handle)) / 96d;

    public OverlayPlacement(Window window, Func<bool> snapping, Action save)
    {
        this.window = window; this.snapping = snapping; this.save = save;
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
        if (message == 0x0231) moving = true; // WM_ENTERSIZEMOVE
        if (message == 0x0216 && lParam != 0) // WM_MOVING: modify the proposed rectangle, never the pointer.
        {
            var proposed = Marshal.PtrToStructure<NativeRect>(lParam);
            var result = Snap(proposed.Rect, WorkArea(proposed), 12 * Scale, snapping() && GetKeyState(0x10) >= 0);
            Marshal.StructureToPtr(NativeRect.From(result), lParam, false);
            handled = true; return 1;
        }
        if (message == 0x0232) { moving = false; save(); } // WM_EXITSIZEMOVE: preserve free placement when Shift bypassed snapping.
        if (message is 0x007E or 0x02E0 || (message == 0x001A && wParam == 0x002F))
            _ = window.Dispatcher.BeginInvoke(() => { if (!closed) { EnsureVisible(); save(); } });
        return 0;
    }

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

    internal static int Verify()
    {
        var checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        var primary = new Rect(0, 0, 1920, 1040);
        Check(Snap(new Rect(8, 9, 460, 400), primary, 12, true).Location == new Point(0, 0), "Top-left snap failed.");
        Check(Snap(new Rect(1453, 633, 460, 400), primary, 12, true).Location == new Point(1460, 640), "Bottom-right snap failed.");
        var free = new Rect(-8, 9, 460, 400);
        Check(Snap(free, primary, 12, false) == free, "Snap bypass moved window.");
        Check(Snap(new Rect(40, 50, 460, 400), primary, 12, true).Location == new Point(40, 50), "Snapping pulled a distant window.");
        var leftMonitor = new Rect(-2560, -200, 2560, 1400);
        Check(Snap(new Rect(-2550, -195, 460, 400), leftMonitor, 12, true).Location == leftMonitor.Location, "Negative monitor coordinates failed.");
        var enlarged = new Rect(0, 40, 3840, 2040);
        Check(Snap(new Rect(22, 62, 920, 800), enlarged, 24, true).Location == enlarged.Location, "DPI-scaled snap distance failed.");
        Check(KeepVisible(new Rect(4000, -1500, 460, 400), primary).Location == new Point(1460, 0), "Disconnected screen recovery failed.");
        Check(AfterHeight(new Rect(1460, 640, 460, 400), 600, primary, 2).Bottom == primary.Bottom, "Bottom anchoring failed when growing.");
        Check(AfterHeight(new Rect(1460, 640, 460, 400), 260, primary, 2).Bottom == primary.Bottom, "Bottom anchoring failed when shrinking.");
        Check(AfterHeight(new Rect(40, 60, 460, 400), 600, primary, 2).Top == 60, "Undocked overlay jumped when resizing.");

        // Exercise the real HWND hook without showing a window or moving the user's mouse.
        var test = new Window
        {
            Width = 460,
            Height = 260,
            MinHeight = 200,
            MaxHeight = 1000,
            Left = 40,
            Top = 80,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false
        };
        var enabled = true;
        var controller = new OverlayPlacement(test, () => enabled, () => { });
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
        try
        {
            var hwnd = new WindowInteropHelper(test).EnsureHandle();
            var bounds = controller.Bounds; var work = controller.Area(bounds);
            var proposed = NativeRect.From(new Rect(work.Left + 5, work.Top + 5, bounds.Width, bounds.Height));
            Marshal.StructureToPtr(proposed, pointer, false);
            var bypass = GetKeyState(0x10) < 0;
            Check(SendMessage(hwnd, 0x0216, 0, pointer) == 1, "Native move hook not reached.");
            var snapped = Marshal.PtrToStructure<NativeRect>(pointer).Rect;
            Check(snapped.Location == (bypass ? proposed.Rect.Location : work.Location), "Native hook produced incorrect screen coordinates.");
            enabled = false; Marshal.StructureToPtr(proposed, pointer, false); SendMessage(hwnd, 0x0216, 0, pointer);
            Check(Marshal.PtrToStructure<NativeRect>(pointer).Rect == proposed.Rect, "Native disabled setting ignored.");
            controller.Place("bottomRight");
            Check(Math.Abs(controller.Bounds.Bottom - work.Bottom) <= 1 && Math.Abs(controller.Bounds.Right - work.Right) <= 1, "Native corner placement failed.");
            controller.SetHeight(320);
            Check(Math.Abs(controller.Bounds.Bottom - work.Bottom) <= 1, "Native height change broke bottom anchoring.");
            Check(!test.IsVisible, "Placement verification must never show a window.");
        }
        finally { Marshal.FreeHGlobal(pointer); test.Close(); }
        return checks;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rect Rect => new(Left, Top, Math.Max(0, Right - Left), Math.Max(0, Bottom - Top));
        public static NativeRect From(Rect rect) => new() { Left = (int)Math.Round(rect.Left), Top = (int)Math.Round(rect.Top), Right = (int)Math.Round(rect.Right), Bottom = (int)Math.Round(rect.Bottom) };
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromRect(ref NativeRect rect, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(nint hwnd, out NativeRect rect);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
}
