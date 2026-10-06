using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Spike.Desktop;

internal sealed partial class ActivityToast
{
    internal static void Verify(Preferences preferences, Window reference, string directory)
    {
        var toast = new ActivityToast(preferences, () => { }, reference);
        for (var i = 0; i < 6; i++) toast.Enqueue(Text.Get(new[] { "eventShugo", "eventRift", "eventSiege", "eventSiegeBosses", "eventNahma", "eventKaira" }[i], preferences.Language), "12:00 · " + string.Format(Text.Get("startsIn", preferences.Language), 5));
        if (toast.VisibleCount is < 1 or > 4 || toast.VisibleCount + toast.PendingCount != 6)
            throw new InvalidOperationException("Every simultaneous notification must be visible or queued.");
        var count = toast.VisibleCount;
        var now = DateTimeOffset.UtcNow;
        toast.Expire(now.AddMinutes(1), reading: true);
        if (toast.VisibleCount != count || toast.VisibleCount + toast.PendingCount != 6)
            throw new InvalidOperationException("Reading a stack must pause expiration.");
        var content = (FrameworkElement)toast.Content;
        content.Measure(new Size(toast.Width, double.PositiveInfinity));
        var height = Math.Ceiling(content.DesiredSize.Height);
        content.Arrange(new Rect(0, 0, toast.Width, height)); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)toast.Width, (int)height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(directory, $"notifications-{preferences.Language}-{preferences.Theme}.png"))) png.Save(file);
        toast.Expire(now.AddMinutes(2), reading: false);
        if (toast.VisibleCount + toast.PendingCount != 6 - count)
            throw new InvalidOperationException("Expired cards must promote the queue without losing reminders.");
        toast.Expire(DateTimeOffset.UtcNow, reading: false);
        if (toast.VisibleCount + toast.PendingCount != 6 - count)
            throw new InvalidOperationException("Newly visible cards must receive a full reading time.");
        toast.Close();
    }
}
