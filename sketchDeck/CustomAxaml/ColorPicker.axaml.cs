
using System;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

#if WINDOWS
using System.Drawing;
using System.Runtime.InteropServices;
#endif

namespace sketchDeck.CustomAxaml;

public partial class ColorPickerWindow : Window
{
    public Color SelectedColor { get; private set; }
    public bool IsCancelled { get; private set; }

    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();
        this.Icon = new WindowIcon("Assets/avalonia-logo.ico");
        Picker.ColorChanged += (_, __) => SelectedColor = initialColor;
    }
}

public class ScreenPipette
{
    private DispatcherTimer? _timer;
    private IBrush _onColorPicked;
    private readonly Border _colorPickerPreview;

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
#endif

    public ScreenPipette(IBrush initialBrush)
    {
        _onColorPicked = initialBrush;
        _colorPickerPreview = new Border
        {
            Width = 25,
            Height = 25,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Black,
            BorderThickness = new Avalonia.Thickness(1)
        };
    }

    public void Start()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var brush = PickColorUnderCursor();
        _onColorPicked = brush;

#if WINDOWS
        GetCursorPos(out POINT point);
        Canvas.SetLeft(_colorPickerPreview, point.X + 10);
        Canvas.SetTop(_colorPickerPreview, point.Y + 10);
#endif
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public IBrush GetCurrentBrush() => _onColorPicked;

    public Border GetPreviewBorder() => _colorPickerPreview;

    public static IBrush PickColorUnderCursor()
    {
#if WINDOWS
        GetCursorPos(out POINT point);
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(point.X, point.Y, 0, 0, new System.Drawing.Size(1, 1));
        var color = bmp.GetPixel(0, 0);
        return new SolidColorBrush(Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
#else
        return Brushes.Transparent;
#endif
    }
}
