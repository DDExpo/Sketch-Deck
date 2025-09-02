using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Data.Converters;
using sketchDeck.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace sketchDeck.ViewModels;

public partial class MiddlePanelViewModel : ObservableObject
{
    public string[] Views { get; } = ["Gigantic", "Big", "Medium", "Small", "Details"];

    [ObservableProperty]
    public string _selectedView = "Details";

    [ObservableProperty]
    private ObservableCollection<ImageItem> _images;
    private readonly ObservableCollection<ImageItem> _allImages = [];

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string _currentImagePath = string.Empty;

    public MiddlePanelViewModel()
    {
        Images = new ObservableCollection<ImageItem>(_allImages);
        PropertyChanged += OnSearchTermChanged;
    }
    private void OnSearchTermChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchTerm))
        {
            IEnumerable<ImageItem> filtered = string.IsNullOrWhiteSpace(SearchTerm)
                ? _allImages
                : _allImages.Where(item =>
                    item.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            Images.Clear();
            foreach (var item in filtered)
            {
                Images.Add(item);
            }
        }
    }
    public async Task LoadFolderAsync(string folderPath, CancellationToken token)
    {
        Images.Clear();

        if (!Directory.Exists(folderPath)) return;

        foreach (var file in Directory.GetFiles(folderPath))
        {
            token.ThrowIfCancellationRequested();
            var item = await ImageItem.FromPathAsync(file);
            CurrentImagePath = file;
            Images.Add(item);
        }
    }
}
public class ViewToTemplateConverter : IValueConverter
{
    public IDataTemplate? Details { get; set; }
    public IDataTemplate? Small { get; set; }
    public IDataTemplate? Medium { get; set; }
    public IDataTemplate? Big { get; set; }
    public IDataTemplate? Gigantic { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            "Details" => Details,
            "Small" => Small,
            "Medium" => Medium,
            "Big" => Big,
            "Gigantic" => Gigantic,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
public class ThumbnailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            return new Bitmap(path);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) 
        => throw new NotSupportedException();
}

public class PreviewWindow : Window
    {
        private readonly Image _image;
        private readonly ScrollViewer _scrollViewer;
        private double _zoom = 1.0;
        private bool _fitToWindow = true;

        public PreviewWindow(Bitmap bitmap)
        {
            Width = 800;
            Height = 600;
            Title = "Image Preview";

            _image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
                RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 }
            };

            _scrollViewer = new ScrollViewer
            {
                Content = _image
            };

            Content = _scrollViewer;

            PointerWheelChanged += OnPointerWheelChanged;
            DoubleTapped += OnDoubleTapped;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (_fitToWindow)
            {
                _fitToWindow = false;
                _image.Stretch = Stretch.None;
                _zoom = 1.0;
            }

            if (_image.RenderTransform is ScaleTransform scale)
            {

                var pos = e.GetPosition(_image);

                double oldZoom = _zoom;
                _zoom *= e.Delta.Y > 0 ? 1.1 : 0.9;
                _zoom = System.Math.Clamp(_zoom, 0.1, 10.0);

                scale.ScaleX = _zoom;
                scale.ScaleY = _zoom;

                var offsetX = (pos.X * _zoom / oldZoom) - pos.X;
                var offsetY = (pos.Y * _zoom / oldZoom) - pos.Y;

                _scrollViewer.Offset = new Avalonia.Vector(
                    _scrollViewer.Offset.X + offsetX,
                    _scrollViewer.Offset.Y + offsetY
                );
            }
        }

        private void OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_fitToWindow)
            {
                _fitToWindow = false;
                _image.Stretch = Stretch.None;
                _zoom = 1.0;

                if (_image.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1;
                    scale.ScaleY = 1;
                }
            }
            else
            {
                _fitToWindow = true;
                _image.Stretch = Stretch.Uniform;

                if (_image.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1;
                    scale.ScaleY = 1;
                }
            }
        }
    }

// public class SizeComparer : DataGridComparerSortDescription
// {
//     public int Compare(object x, object y)
//     {
//         if (x is not ImageItem a || y is not ImageItem b)
//             return 0;

//         long sizeA = ParseSize(a.Size);
//         long sizeB = ParseSize(b.Size);

//         return sizeA.CompareTo(sizeB);
//     }

//     private static long ParseSize(string size)
//     {
//         if (string.IsNullOrWhiteSpace(size)) return 0;
//         size = size.Trim().ToUpper();
//         double number = 0;
//         if (size.EndsWith("KB"))
//             number = double.Parse(size.Replace("KB", "").Trim()) * 1024;
//         else if (size.EndsWith("MB"))
//             number = double.Parse(size.Replace("MB", "").Trim()) * 1024 * 1024;
//         else if (size.EndsWith("GB"))
//             number = double.Parse(size.Replace("GB", "").Trim()) * 1024 * 1024 * 1024;
//         else
//             number = double.Parse(size);

//         return (long)number;
//     }
// }
