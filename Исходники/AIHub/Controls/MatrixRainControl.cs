using System.Collections.Concurrent;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color;

namespace AIHub.Controls;

public sealed class MatrixRainControl : FrameworkElement
{
    private const double BoundaryMargin = 48;
    private readonly Stopwatch _clock = new();
    private double _previousSeconds;
    private readonly ConcurrentQueue<char> _characters = new();
    private readonly List<MatrixGlyph> _glyphs = [];
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private MediaColor _accentColor = MediaColor.FromRgb(42, 210, 108);
    private bool _active;
    private double _quietSeconds;

    public MatrixRainControl()
    {
        IsHitTestVisible = false;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _timer.Tick += (_, _) => Advance();
    }

    public void Start(MediaColor? accentColor = null)
    {
        _accentColor = accentColor ?? MediaColor.FromRgb(42, 210, 108);
        _active = true;
        _quietSeconds = 0;
        _glyphs.Clear();
        while (_characters.TryDequeue(out _))
        {
        }

        _previousSeconds = 0;
        _clock.Restart();
        _timer.Start();
        InvalidateVisual();
    }

    public void Feed(string text)
    {
        foreach (var character in text.Where(value => !char.IsControl(value) && !char.IsWhiteSpace(value)))
        {
            _characters.Enqueue(character);
        }


    }

    public void Stop()
    {
        _active = false;
        _timer.Stop();
        _clock.Stop();
        _glyphs.Clear();
        while (_characters.TryDequeue(out _)) { }
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        foreach (var glyph in _glyphs)
        {
            if (glyph.Y < -24 || glyph.Y > ActualHeight || glyph.X < -24 || glyph.X > ActualWidth) continue;
            var end = EndY(glyph);
            var progress = Math.Clamp((glyph.Y - glyph.StartY) / Math.Max(1, end - glyph.StartY), 0, 1);
            var opacity = glyph.FromStream ? 0.9 - progress * 0.35
                : 0.7 * Math.Clamp((1 - progress) / 0.25, 0, 1);
            if (glyph.Text is null || glyph.TextDpi != dpi)
            {
                var brush = new SolidColorBrush(_accentColor);
                glyph.Brush = brush;
                glyph.Text = new FormattedText(glyph.Character.ToString(), CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, new Typeface("Consolas"), glyph.FontSize, brush, dpi);
                glyph.TextDpi = dpi;
            }
            glyph.Brush!.Opacity = opacity;
            drawingContext.DrawText(glyph.Text, new System.Windows.Point(glyph.X, glyph.Y));
        }
    }

    private void Advance()
    {
        var now = _clock.Elapsed.TotalSeconds;
        var seconds = Math.Max(0, now - _previousSeconds);
        _previousSeconds = now;
        Step(seconds);
    }

    private double EndY(MatrixGlyph glyph) => glyph.FromStream
        ? ActualHeight + BoundaryMargin
        : (ActualHeight + BoundaryMargin) * glyph.PathFraction;

    private void Step(double seconds)
    {
        if (!_active || ActualWidth <= 1 || ActualHeight <= 1) return;
        // Compact in place so dense bursts do not require shifting the list per expired glyph.
        var retained = 0;
        for (var index = 0; index < _glyphs.Count; index++)
        {
            var glyph = _glyphs[index];
            glyph.Y += glyph.Speed * seconds;
            if (glyph.Y <= EndY(glyph)) _glyphs[retained++] = glyph;
        }
        if (retained < _glyphs.Count) _glyphs.RemoveRange(retained, _glyphs.Count - retained);

        var emitted = false;
        while (_characters.TryDequeue(out var character))
        {
            AddGlyph(character, fromStream: true);
            emitted = true;
        }
        if (emitted) _quietSeconds = 0;
        else
        {
            _quietSeconds += seconds;
            if (_quietSeconds >= 0.1)
            {
                _quietSeconds %= 0.1;
                AddGlyph(RandomAmbientCharacter(), fromStream: false);
            }
        }
        InvalidateVisual();
    }

    private void AddGlyph(char character, bool fromStream)
    {
        const double columnWidth = 17;
        var columns = Math.Max(1, (int)((ActualWidth + 2 * BoundaryMargin) / columnWidth));
        var x = _random.Next(columns) * columnWidth - BoundaryMargin + _random.NextDouble() * 3;
        var speed = fromStream ? 136 + _random.NextDouble() * 152 : 76 + _random.NextDouble() * 91;
        _glyphs.Add(new MatrixGlyph(character, x, -24 - _random.NextDouble() * BoundaryMargin,
            speed, fromStream, fromStream ? 1 : 0.15 + _random.NextDouble() * 0.85, 14 + _random.Next(0, 4)));
    }

    private char RandomAmbientCharacter()
    {
        const string alphabet = "01{}[]<>/\\AIHUB#@*+-=アイウエオカキクケコ";
        return alphabet[_random.Next(alphabet.Length)];
    }

    private sealed class MatrixGlyph(char character, double x, double y, double speed,
        bool fromStream, double pathFraction, double fontSize)
    {
        public char Character { get; } = character;
        public double X { get; } = x;
        public double StartY { get; } = y;
        public double Y { get; set; } = y;
        public double Speed { get; } = speed;
        public bool FromStream { get; } = fromStream;
        public double PathFraction { get; } = pathFraction;
        public double FontSize { get; } = fontSize;
        public FormattedText? Text { get; set; }
        public SolidColorBrush? Brush { get; set; }
        public double TextDpi { get; set; }
    }
}
