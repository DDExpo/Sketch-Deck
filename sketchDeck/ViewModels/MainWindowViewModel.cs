using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;

using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using CommunityToolkit.Mvvm.ComponentModel;

using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public RightPanelViewModel RightPanel { get; }
    public LeftPanelViewModel LeftPanel { get; }
    [ObservableProperty]
    private string _selectedView = "Details";
    [ObservableProperty]
    private string _searchTerm = string.Empty;
    [ObservableProperty]
    private string _currentImagePath = string.Empty;
    public SourceList<ImageItem> _source = new();
    [ObservableProperty]
    private ReadOnlyObservableCollection<ImageItem> _images;
    [ObservableProperty]
    private string _sortBy = "Name";
    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    public MainWindowViewModel()
    {
        RightPanel = new RightPanelViewModel(this);
        LeftPanel = new LeftPanelViewModel(this);

        var filter = this.WhenAnyValue(vm => vm.SearchTerm)
        .DistinctUntilChanged()
        .Select(term => new Func<ImageItem, bool>(item =>
            string.IsNullOrWhiteSpace(term) ||
            item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));

        var sort = this.WhenAnyPropertyChanged(nameof(SortBy), nameof(SortDirection))
                       .Select(_ => GetComparer(SortBy, SortDirection))
                       .StartWith(GetComparer(SortBy, SortDirection));

        _source.Connect()
                .Filter(filter)
                .Sort(sort, resetThreshold: 32)
                .Bind(out _images)
                .Subscribe();
    }
    private static SortExpressionComparer<ImageItem> GetComparer(string sortBy, ListSortDirection dir)
    {
        var asc = dir == ListSortDirection.Ascending;
        return sortBy switch
        {
            "Name" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i.Name ?? string.Empty)
                : SortExpressionComparer<ImageItem>.Descending(i => i.Name ?? string.Empty),
            "Type" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i.Type ?? string.Empty)
                : SortExpressionComparer<ImageItem>.Descending(i => i.Type ?? string.Empty),
            "Size" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i.Size)
                : SortExpressionComparer<ImageItem>.Descending(i => i.Size),
            "DateModified" => asc
                ? SortExpressionComparer<ImageItem>.Ascending(i => i.DateModified)
                : SortExpressionComparer<ImageItem>.Descending(i => i.DateModified),
            _ => SortExpressionComparer<ImageItem>.Ascending(i => i.Name ?? string.Empty)
        };
    }
    public void UpdateSort(string column, ListSortDirection direction)
    {
        SortBy = column ?? "Name";
        SortDirection = direction;
    }
    public void AddImage(ImageItem item) => _source.Add(item);
    public void RemoveImage(ImageItem item) => _source.Remove(item);
    public void ClearImages() => _source.Clear();
}
public class BaseWindow : Window
{
    protected readonly ZoomBorder PanAndZoomBorder;
    protected readonly Image Picture;
    protected readonly MenuItem AlwaysOnTopItem;
    protected readonly ColorPicker PickerColor;
    protected readonly Grid LayoutGrid = new();
    public BaseWindow()
    {
        Width = 800;
        Height = 600;
        Icon = new WindowIcon("Assets/avalonia-logo.ico");

        Picture = new Image
        {
            Stretch = Stretch.Uniform
        };

        PanAndZoomBorder = new ZoomBorder
        {
            Child = Picture,
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

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var alwaysOnTopLabel = new TextBlock
        {
            Text = Topmost ? "✓ Always on top" : "Always on top",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var shortcutHint = new TextBlock
        {
            Text = "Ctrl+T",
            Opacity = 0.7,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };     
        headerGrid.Children.Add(alwaysOnTopLabel);
        headerGrid.Children.Add(shortcutHint);
        Grid.SetColumn(shortcutHint, 1);

        AlwaysOnTopItem = new MenuItem { Header = headerGrid, StaysOpenOnClick = true };
        AlwaysOnTopItem.Click += (_, _) =>
        {
            Topmost = !Topmost;
            alwaysOnTopLabel.Text = Topmost ? "✓ Always on top" : "Always on top";
            AlwaysOnTopItem.Close();
        };

        PickerColor = new ColorPicker { Width = 155, Height = 15};
        var colorPickerItem = new MenuItem{ Header = PickerColor};

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(AlwaysOnTopItem);
        contextMenu.Items.Add(colorPickerItem);

        PanAndZoomBorder.ContextMenu = contextMenu;
    }
    protected void LoadImage(string path, IBrush color)
    {
        if (Picture.Source is IDisposable disposable)
            disposable.Dispose();

        using var stream = File.OpenRead(path);
        Picture.Source = new Bitmap(stream);
        PanAndZoomBorder.Background = color;
        PickerColor.Color = (color as SolidColorBrush)?.Color ?? Colors.Gray;
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