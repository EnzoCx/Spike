using System.Windows;
using System.Windows.Controls;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void RenderPublicPreviews(string directory)
    {
        var demo = PublicPreviewFixture.Create();
        foreach (var theme in Themes.Ids)
        {
            preferences = new("en", theme, false, AutoStart: false);
            updateStatusKey = "updateCurrent";
            noticeKey = null;
            ApplyTheme(); Translate();
            SpellSearch.Clear(); TimelineExpander.IsExpanded = false;
            DisplayEncounter(demo, 1, false);
            StatusLabel.Text = T("stopped");
            SaveDashboard(directory, $"public-report-en-{theme}.png", 1424, 900);
            if (SpellList.Items.Count != 6 || OriginLabel.Text != T("demoLabel"))
                throw new InvalidOperationException("Public report must show six skills and its demo label.");
            foreach (var item in SpellList.Items)
                if (item.GetType().GetProperty("Icon")?.GetValue(item) is not Image)
                    throw new InvalidOperationException("Public report must render catalog skill icons.");
            OverlayWindow.RenderPublicPreview(preferences, demo, directory, (Style)FindResource(typeof(Button)));
        }
    }
}
