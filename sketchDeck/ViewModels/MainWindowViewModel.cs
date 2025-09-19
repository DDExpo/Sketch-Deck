using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using CommunityToolkit.Mvvm.ComponentModel;

using sketchDeck.Models;
using sketchDeck.CustomAxaml;
using System.Threading.Tasks;

namespace sketchDeck.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public RightPanelViewModel RightPanel { get; }
    public LeftPanelViewModel LeftPanel { get; }
    [ObservableProperty] private string _selectedView = "Details";
    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private string _currentImagePath = string.Empty;

    [ObservableProperty] internal ObservableCollection<CollectionItem> _collections = [];
    [ObservableProperty] private int? _selectedCollection;
    internal readonly BehaviorSubject<CollectionItem?> _activeCollection;
    [ObservableProperty] internal ReadOnlyObservableCollection<ImageItem>? _images;

    public MainWindowViewModel()
    {
        RightPanel = new RightPanelViewModel(this);
        LeftPanel = new LeftPanelViewModel(this);

        Collections.Add(CollectionItem.FromImages(new SourceList<ImageItem>(), "Current"));

        SelectedCollection = 0;
        _activeCollection = new(Collections[SelectedCollection.Value]);

        _activeCollection
            .WhereNotNull()
            .Select(c =>
                c.CollectionImages.Connect()
                    .Filter(this.WhenAnyValue(vm => vm.SearchTerm)
                        .DistinctUntilChanged()
                        .Select(term => new Func<ImageItem, bool>(item =>
                            string.IsNullOrWhiteSpace(term) ||
                            (item?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))))
                    .Sort(c.WhenAnyPropertyChanged(nameof(CollectionItem.SortBy), nameof(CollectionItem.SortDirection))
                        .Select(_ => GetComparer(c.SortBy ?? "Name", c.SortDirection))
                        .StartWith(GetComparer(c.SortBy ?? "Name", c.SortDirection)))
            )
            .Switch()
            .Bind(out _images!)
            .Subscribe();
    }

    partial void OnSelectedCollectionChanged(int? value)
    {
        if (_activeCollection is null) return;

        if (value is not null && value.Value >= 0 && value.Value < Collections.Count)
            _activeCollection.OnNext(Collections[value.Value]);
        else
            _activeCollection.OnNext(null);
    }

    private static SortExpressionComparer<ImageItem> GetComparer(string sortBy, ListSortDirection dir)
    {
        var asc = dir == ListSortDirection.Ascending;

        return sortBy switch
        {
            "Name" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i?.Name ?? string.Empty)
                : SortExpressionComparer<ImageItem>.Descending(i => i?.Name ?? string.Empty),

            "Type" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i?.Type ?? string.Empty)
                : SortExpressionComparer<ImageItem>.Descending(i => i?.Type ?? string.Empty),

            "Size" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i?.Size ?? 0)
                : SortExpressionComparer<ImageItem>.Descending(i => i?.Size ?? 0),

            "DateModified" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i?.DateModified ?? DateTime.MinValue)
                : SortExpressionComparer<ImageItem>.Descending(i => i?.DateModified ?? DateTime.MinValue),

            _ => SortExpressionComparer<ImageItem>.Ascending(i => i?.Name ?? string.Empty)
        };
    }
}

public class BaseWindow : Window
{
    protected readonly ZoomBorder PanAndZoomBorder;
    protected readonly Image Picture;
    protected readonly MenuItem AlwaysOnTopItem;
    protected readonly ColorPicker PickerColor;
    protected readonly Grid LayoutGrid = new();
    private readonly TextBlock _missingFileText;
    private ScreenPipette? _pipette;
    public BaseWindow()
    {
        Width = 800;
        Height = 600;
        Icon = new WindowIcon("Assets/avalonia-logo.ico");

        Picture = new Image { Stretch = Stretch.Uniform };

         _missingFileText = new TextBlock
        {
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20), 
            IsHitTestVisible = false,
            Text = ""
        };

        var pictureOverlay = new Grid();
        pictureOverlay.Children.Add(Picture);
        pictureOverlay.Children.Add(_missingFileText);

        PanAndZoomBorder = new ZoomBorder
        {
            Child = pictureOverlay,
            Background = Brushes.Gray,
            ClipToBounds = true,
            Focusable = true,
            EnablePan = true,
            EnableZoom = true,
            PanButton = ButtonName.Left
        };

        PanAndZoomBorder.KeyDown += ZoomBorder_KeyDown;

        LayoutGrid.Children.Add(PanAndZoomBorder);
        Content = LayoutGrid;
        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 2) };
        var alwaysOnTopLabel = new TextBlock { Text = Topmost ? "✓ Always on top" : "Always on top", VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Margin = new Thickness(1, 0, 0, 0) };
        var shortcutHint = new TextBlock { Text = "Ctrl+T", Opacity = 0.7, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };

        headerGrid.Children.Add(alwaysOnTopLabel);
        headerGrid.Children.Add(shortcutHint);
        Grid.SetColumn(shortcutHint, 1);

        AlwaysOnTopItem = new MenuItem { Header = headerGrid, StaysOpenOnClick = true };
        AlwaysOnTopItem.Click += (_, _) => { Topmost = !Topmost; alwaysOnTopLabel.Text = Topmost ? "✓ Always on top" : "Always on top"; AlwaysOnTopItem.Close(); };

        var pickerPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 2), Spacing = 28 };
        PickerColor = new ColorPicker { Width = 125, Height = 25, Margin = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };

        var dropperButton = new Button { Width = 25, Height = 25, Padding = new Thickness(0), CornerRadius = new CornerRadius(6), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var canvas = new Canvas { Width = 15, Height = 15, };
        var pathSVG = new Avalonia.Controls.Shapes.Path { Stroke = Brushes.White, StrokeThickness = 1.3, StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, Data = Geometry.Parse("M12.401 5.568l-.743 1.3 3.6187 2.1855L16.859 6.538c.6035-.9993.2827-2.2986-.8713-2.7358-.9993-.6035-2.5967-.5462-3.592 1.775ZM10 5.9A1.05 1.05 0 009.2 7.8l6.916 4.187a1.05 1.05 0 00.805-1.94L10 5.9Zm.5566 2.7241-4.6613 7.718c-.3427.5673-.482 1.2344-.3952 1.8915l.1337 1.0116c.1024.7745.7319 1.3722 1.5106 1.4344.6255.0499 1.2256-.2578 1.55-.795l5.4809-9.075-3.6187-2.1855Z") };
        Canvas.SetLeft(pathSVG, -4);
        Canvas.SetTop(pathSVG, -4);
        canvas.Children.Add(pathSVG);

        dropperButton.Content = canvas;
        dropperButton.Click += DropperButton_Click;
        pickerPanel.Children.Add(PickerColor);
        pickerPanel.Children.Add(dropperButton);

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(AlwaysOnTopItem);
        contextMenu.Items.Add(pickerPanel);

        var btn = new Button { Width = 25, Height = 25, Padding = new Thickness(0), CornerRadius = new CornerRadius(6), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        btn.Click += StopPipetteButton_Click;
        contextMenu.Items.Add(btn);
        PanAndZoomBorder.ContextMenu = contextMenu;
    }

    private void DropperButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _pipette = new ScreenPipette(PickerColor.Background!);
        LayoutGrid.Children.Add(_pipette.GetPreviewBorder());
        _pipette.Start();

    }
    private void StopPipetteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _pipette?.Stop();
        PickerColor.Background = _pipette?.GetCurrentBrush();
    }
    protected void LoadImage(string path, IBrush color)
    {
        if (Picture.Source is IDisposable disposable)
            disposable.Dispose();

        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            Picture.Source = new Bitmap(stream);
            _missingFileText.Text = ""; 
            PanAndZoomBorder.Background = color;
            PickerColor.Color = (color as SolidColorBrush)?.Color ?? Colors.Gray;
        }
        else
        {
            Picture.Source = new Bitmap("Assets/MissingImage.png");
            Picture.Opacity = 0.5;
            _missingFileText.Text = $"File not found:\n{path}\nIt may have been deleted, renamed or moved.";
            PanAndZoomBorder.Background = color;
            PickerColor.Color = Colors.Gray;
        }
    }
    private void ZoomBorder_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.R)
            PanAndZoomBorder?.ResetMatrix();
        else if (e.Key == Key.OemPlus || e.Key == Key.Add)
            PanAndZoomBorder?.ZoomIn();
        else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            PanAndZoomBorder?.ZoomOut();
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.T)
        {
            AlwaysOnTopItem.IsChecked = !AlwaysOnTopItem.IsChecked;
            Topmost = AlwaysOnTopItem.IsChecked;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
            this.Close();
    }
}