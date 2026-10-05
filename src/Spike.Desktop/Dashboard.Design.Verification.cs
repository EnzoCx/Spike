using System.Windows;
using System.Windows.Media;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyDesignFonts()
    {
        foreach (var weight in new[] { FontWeights.Normal, FontWeights.Medium, FontWeights.SemiBold })
        {
            var face = new Typeface(FontFamily, FontStyles.Normal, weight, FontStretches.Normal);
            if (!face.TryGetGlyphTypeface(out var glyph) || !glyph.FontUri.ToString().Contains("Geist", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The interface must use embedded Geist fonts without a system fallback.");
            foreach (var character in "Ééèàçùñ¿¡0123456789")
                if (!glyph.CharacterToGlyphMap.ContainsKey(character))
                    throw new InvalidOperationException("The interface font must cover all three languages and combat values.");
        }
    }
}
