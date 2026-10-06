using System.IO;
using System.Media;
using System.Windows;

namespace Spike.Desktop;

internal static class ReminderSound
{
    private static SoundPlayer? player;
    private static Stream? stream;
    internal static void Play()
    {
        try
        {
            if (player is null)
            {
                stream = Application.GetResourceStream(new Uri("pack://application:,,,/Sounds/reminder.wav"))!.Stream;
                var loaded = new SoundPlayer(stream);
                loaded.Load(); // Validate before Play: never fall back to a Windows system beep.
                player = loaded;
            }
            player.Play();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException)
        { player?.Dispose(); player = null; stream?.Dispose(); stream = null; }
    }
}
