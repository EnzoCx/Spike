using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Spike.Desktop;

/// <summary>The same vector master is used by the app, SVG kit and Windows icon.</summary>
public sealed class BrandMark : FrameworkElement
{
    private static readonly Geometry Symbol = LoadSymbol();
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(BrandMark), new FrameworkPropertyMetadata(Brushes.Goldenrod, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public BrandMark()
    {
        SetResourceReference(FillProperty, "Accent");
        IsHitTestVisible = false;
        System.Windows.Automation.AutomationProperties.SetName(this, Text.ProductName);
    }

    private static Geometry LoadSymbol()
    {
        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Brand/symbol.path"))!.Stream;
        using var reader = new StreamReader(stream);
        var geometry = Geometry.Parse(reader.ReadToEnd()); geometry.Freeze(); return geometry;
    }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        var scale = Math.Min(ActualWidth, ActualHeight) / 64;
        context.PushTransform(new TranslateTransform((ActualWidth - scale * 64) / 2, (ActualHeight - scale * 64) / 2));
        context.PushTransform(new ScaleTransform(scale, scale));
        context.DrawGeometry(Fill, null, Symbol);
        context.Pop(); context.Pop();
    }
}
