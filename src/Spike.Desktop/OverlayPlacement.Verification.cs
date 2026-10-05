using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Spike.Desktop;

internal sealed partial class OverlayPlacement
{
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
        Point? simulatedPointer = null;
        var controller = new OverlayPlacement(test, () => enabled, () => { }, () => simulatedPointer);
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

            Rect Propose(Rect rectangle)
            {
                Marshal.StructureToPtr(NativeRect.From(rectangle), pointer, false);
                SendMessage(hwnd, 0x0216, 0, pointer);
                return Marshal.PtrToStructure<NativeRect>(pointer).Rect;
            }
            enabled = true;
            var steps = (int)Math.Ceiling(6 * controller.Scale) + 4;
            // Feed each snapped result back as the native loop's next starting position.
            // One-pixel pointer movements must still detach, with no acceleration required.
            foreach (var direction in new[] { new Vector(1, 0), new Vector(-1, 0), new Vector(0, 1), new Vector(0, -1),
                new Vector(1, 1), new Vector(-1, 1), new Vector(1, -1), new Vector(-1, -1) })
            {
                var origin = new Point(
                    direction.X > 0 ? work.Left : direction.X < 0 ? work.Right - bounds.Width : work.Left + 100,
                    direction.Y > 0 ? work.Top : direction.Y < 0 ? work.Bottom - bounds.Height : work.Top + 100);
                var start = new Rect(origin, bounds.Size);
                simulatedPointer = origin + new Vector(40, 20);
                SendMessage(hwnd, 0x0231, 0, 0);
                var current = Propose(start);
                for (var step = 1; step <= steps; step++)
                {
                    simulatedPointer += direction;
                    current.Offset(direction);
                    current = Propose(current);
                }
                Check(current.Location == origin + direction * steps, "Slow drag remained stuck to an edge or corner.");
                Check(Propose(current) == current, "Stationary pointer drifted after detaching.");
                for (var step = steps - 1; step >= 0; step--)
                {
                    simulatedPointer -= direction;
                    current.Offset(-direction);
                    current = Propose(current);
                }
                Check(current == start, "Returning slowly to an edge lost the original grab offset.");
                SendMessage(hwnd, 0x0232, 0, 0);
                Check(controller.dragOffset is null, "Finished drag retained its grab offset.");
            }

            // A fresh drag and a missing cursor must use the new native proposal.
            var fresh = new Rect(work.Left + 100, work.Top + 100, bounds.Width, bounds.Height);
            simulatedPointer = fresh.Location + new Vector(80, 15);
            SendMessage(hwnd, 0x0231, 0, 0);
            Check(Propose(fresh) == fresh, "New drag reused an old grab offset.");
            simulatedPointer += new Vector(20, 20);
            enabled = false;
            fresh.Offset(20, 20);
            Check(Propose(fresh) == fresh, "Disabling snapping interrupted pointer tracking.");
            simulatedPointer = null;
            fresh.Offset(30, 30);
            Check(Propose(fresh) == fresh && controller.dragOffset is null, "Missing cursor did not preserve native movement.");
            SendMessage(hwnd, 0x0232, 0, 0);
            Check(!test.IsVisible, "Placement verification must never show a window.");
        }
        finally { Marshal.FreeHGlobal(pointer); test.Close(); }
        return checks;
    }
}
