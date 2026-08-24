using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color;

namespace AIHub.Controls;

public sealed class MatrixRainControl : FrameworkElement
{
    private const int MaxQueuedCharacters = 4096;
    private readonly ConcurrentQueue<char> _characters = new();
    private readonly List<MatrixGlyph> _glyphs = [];
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private MediaColor _accentColor = MediaColor.FromRgb(42, 210, 108);
    private bool _active;
    private int _quietFrames;

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
        _quietFrames = 0;
        _glyphs.Clear();
        while (_characters.TryDequeue(out _))
        {
        }

        _timer.Start();
        InvalidateVisual();
    }

    public void Feed(string text)
    {
        foreach (var character in text.Where(value => !char.IsControl(value) && !char.IsWhiteSpace(value)))
        {
            _characters.Enqueue(character);
        }

        while (_characters.Count > MaxQueuedCharacters && _characters.TryDequeue(out _))
        {
        }
    }

    public void Stop()
    {
        _active = false;
        _timer.Stop();
        _glyphs.Clear();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        foreach (var glyph in _glyphs)
        {
            var life = Math.Clamp(glyph.Life, 0, 1);
            var intensity = glyph.Head ? 0.86 : 0.34 + 0.6 * life;
            var brush = new SolidColorBrush(MediaColor.FromRgb(
                BlendChannel(_accentColor.R, intensity, glyph.Head),
                BlendChannel(_accentColor.G, intensity, glyph.Head),
                BlendChannel(_accentColor.B, intensity, glyph.Head)));
            var text = new FormattedText(
                glyph.Character.ToString(),
                CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                glyph.FontSize,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(text, new System.Windows.Point(glyph.X, glyph.Y));
        }
    }

    private void Advance()
    {
        if (!_active || ActualWidth <= 1 || ActualHeight <= 1)
        {
            return;
        }

        for (var index = _glyphs.Count - 1; index >= 0; index--)
        {
            var glyph = _glyphs[index];
            glyph.Y += glyph.Speed;
            glyph.Life -= 0.017;
            glyph.Head = false;
            if (glyph.Y > ActualHeight || glyph.Life <= 0)
            {
                _glyphs.RemoveAt(index);
            }
        }

        var emitted = 0;
        while (emitted < 18 && _characters.TryDequeue(out var character))
        {
            AddGlyph(character, fromStream: true);
            emitted++;
        }

        if (emitted == 0)
        {
            _quietFrames++;
            if (_quietFrames % 3 == 0)
            {
                AddGlyph(RandomAmbientCharacter(), fromStream: false);
            }
        }
        else
        {
            _quietFrames = 0;
        }

        InvalidateVisual();
    }

    private void AddGlyph(char character, bool fromStream)
    {
        var columnWidth = 17d;
        var columns = Math.Max(1, (int)(ActualWidth / columnWidth));
        var column = _random.Next(columns);
        var x = column * columnWidth + _random.NextDouble() * 3;
        var speed = fromStream ? 4.5 + _random.NextDouble() * 5 : 2.5 + _random.NextDouble() * 3;
        _glyphs.Add(new MatrixGlyph(character, x, -20 - _random.NextDouble() * 60, speed, fromStream ? 1 : 0.72, 14 + _random.Next(0, 4), true));
        if (_glyphs.Count > 320)
        {
            _glyphs.RemoveRange(0, _glyphs.Count - 320);
        }
    }

    private char RandomAmbientCharacter()
    {
        const string alphabet = "01{}[]<>/\\AIHUB#@*+-=アイウエオカキクケコ";
        return alphabet[_random.Next(alphabet.Length)];
    }

    private static byte BlendChannel(byte channel, double intensity, bool head)
    {
        var value = channel * intensity + (head ? 255 * 0.14 : 0);
        return (byte)Math.Clamp(value, 0, 255);
    }

    private sealed class MatrixGlyph(
        char character,
        double x,
        double y,
        double speed,
        double life,
        double fontSize,
        bool head)
    {
        public char Character { get; } = character;
        public double X { get; } = x;
        public double Y { get; set; } = y;
        public double Speed { get; } = speed;
        public double Life { get; set; } = life;
        public double FontSize { get; } = fontSize;
        public bool Head { get; set; } = head;
    }
}
