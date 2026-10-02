using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace Celsius.App.Controls;

/// <summary>
/// A lightweight, dependency-free line chart that plots a numeric series with
/// a fixed Y range. Used to show live CPU temperature during a stress test.
/// </summary>
public sealed class SimpleChart : FrameworkElement
{
    /// <summary>Identifies the <see cref="Values"/> dependency property.</summary>
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values),
        typeof(IEnumerable),
        typeof(SimpleChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(SimpleChart),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(SimpleChart),
        new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Threshold"/> dependency property.</summary>
    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold),
        typeof(double),
        typeof(SimpleChart),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="LineBrush"/> dependency property.</summary>
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush),
        typeof(Brush),
        typeof(SimpleChart),
        new FrameworkPropertyMetadata(
            new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)),
            FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The series to plot; the most recent value is on the right.</summary>
    public IEnumerable? Values
    {
        get => (IEnumerable?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Bottom of the Y axis.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Top of the Y axis.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Optional horizontal threshold line (e.g. the stop threshold).</summary>
    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <summary>Colour of the plotted line.</summary>
    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SimpleChart chart)
        {
            return;
        }

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= chart.OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += chart.OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        // Background.
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
            null,
            new Rect(0, 0, width, height));

        // Grid lines at 25% intervals.
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)), 1);
        gridPen.Freeze();
        for (var i = 1; i < 4; i++)
        {
            var y = height * i / 4.0;
            dc.DrawLine(gridPen, new Point(0, y), new Point(width, y));
        }

        var values = Values?.Cast<object>().Select(ToDouble).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var min = Minimum;
        var max = Maximum;
        var range = max - min;
        if (range <= 0)
        {
            range = 1;
        }

        // Threshold line.
        if (!double.IsNaN(Threshold) && Threshold >= min && Threshold <= max)
        {
            var ty = height - ((Threshold - min) / range * height);
            var thresholdPen = new Pen(new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), 1)
            {
                DashStyle = DashStyles.Dash,
            };
            thresholdPen.Freeze();
            dc.DrawLine(thresholdPen, new Point(0, ty), new Point(width, ty));
        }

        if (values is null || values.Count < 2)
        {
            return;
        }

        // Scale X so the latest sample sits at the right edge.
        var step = width / Math.Max(1, values.Count - 1);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var started = false;
            for (var i = 0; i < values.Count; i++)
            {
                var x = i * step;
                var normalized = Math.Clamp((values[i] - min) / range, 0, 1);
                var y = height - (normalized * height);
                var point = new Point(x, y);

                if (!started)
                {
                    ctx.BeginFigure(point, false, false);
                    started = true;
                }
                else
                {
                    ctx.LineTo(point, true, false);
                }
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(LineBrush, 2), geometry);
    }

    private static double? ToDouble(object? value) => value switch
    {
        float f when !float.IsNaN(f) => f,
        double d when !double.IsNaN(d) => d,
        _ => null,
    };
}
