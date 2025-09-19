using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using PhotoSauce.MagicScaler;
using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class RightPanelViewModel(MainWindowViewModel parent) : ObservableObject
{
    public MainWindowViewModel Parent { get; } = parent;
}
public class ThumbnailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int size))
            {
                Console.WriteLine(size);
                Console.WriteLine(1);
                return ConvertAsync(path, size);
            }
            return new Bitmap(path);
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static async Task<Bitmap> ConvertAsync(string path, int size)
    {
        using var inStream = File.OpenRead(path);
        using var outStream = new MemoryStream();

        await Task.Run(() =>
            MagicImageProcessor.ProcessImage(inStream, outStream, new ProcessImageSettings
            {
                Width = size,
                ResizeMode = CropScaleMode.Max
            }));

        outStream.Position = 0;
        return new Bitmap(outStream);
    }
}
public class PreviewWindow : BaseWindow
{
    private readonly TextBlock _statusText;
    private readonly DispatcherTimer _statusTimer;

    public PreviewWindow(ImageItem im)
    {
        Title = "Image";

        LoadImage(im.PathImage, im.BgColor);

        _statusText = new TextBlock
        {
            Text = "",
            Foreground = Brushes.White,
            Background = Brushes.Black,
            Opacity = 0.6,
            Margin = new Thickness(5),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom
        };

        LayoutGrid.Children.Add(_statusText);
        Content = LayoutGrid;

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _statusTimer.Tick += (_, _) =>
        {
            _statusText.IsVisible = false;
            _statusTimer.Stop();
        };

        PickerColor.Color = (im.BgColor as SolidColorBrush)?.Color ?? Colors.Gray;
        PickerColor.ColorChanged += (_, __) =>
        {
            PanAndZoomBorder.Background = new SolidColorBrush(PickerColor.Color);
            im.BgColor = new SolidColorBrush(PickerColor.Color);
        };
        PanAndZoomBorder.ZoomChanged += (_, _) => UpdateStatus();
    }
    private void UpdateStatus()
    {
        _statusText.IsVisible = true;
        var m = PanAndZoomBorder.Matrix;
        var bmp = (Bitmap)Picture.Source!;

        double imageViewportW = PanAndZoomBorder.Bounds.Width / m.M11;
        double imageViewportH = PanAndZoomBorder.Bounds.Height / m.M11;

        _statusText.Text = $"View:{imageViewportW:F2}×{imageViewportH:F2}\nImage:{bmp.PixelSize.Width}×{bmp.PixelSize.Height}";

        _statusTimer.Stop();
        _statusTimer.Start();
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (Picture.Source is IDisposable disposable)
        {
            disposable.Dispose();
        }
        Picture.Source = null;
    }
}
public static class SelectionExtensions
{
    public static readonly AttachedProperty<bool> IsSelectedProperty =
        AvaloniaProperty.RegisterAttached<Border, bool>("IsSelected", typeof(SelectionExtensions));
    public static void SetIsSelected(AvaloniaObject element, bool value)
    {
        element.SetValue(IsSelectedProperty, value);
        if (element is IPseudoClasses pseudoClasses)
        {
            pseudoClasses.Set(":selected", value);
        }
    }
    public static bool GetIsSelected(AvaloniaObject element) =>
        element.GetValue(IsSelectedProperty);
}