#define WINDOWS

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using CommunityToolkit.Mvvm.ComponentModel;

using sketchDeck.GlobalHooks;
using sketchDeck.ImageOptimiztion;
using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public RightPanelViewModel RightPanel { get; }
    public LeftPanelViewModel LeftPanel { get; }
    [ObservableProperty] private GridLength  _widthLeftPanel;
    [ObservableProperty] private GridLength  _heightLeftPanelChild;
    [ObservableProperty] private float  _mainWindowWidth;
    [ObservableProperty] private float  _mainWindowHeight;
    internal readonly AppSettings _settings;

    [ObservableProperty] private string _selectedView = "Details";
    [ObservableProperty] private string _searchTerm = string.Empty;

    [ObservableProperty] internal ObservableCollection<CollectionItem> _collections = [];
    [ObservableProperty] private int? _selectedCollection;
    internal readonly BehaviorSubject<CollectionItem?> _activeCollection;
    [ObservableProperty] internal ReadOnlyObservableCollection<ImageItem>? _images;
    [ObservableProperty] internal ReadOnlyObservableCollection<ImageItem>? _selectedImages;


    public MainWindowViewModel()
    {
        _settings            = SettingsService.Load();
        WidthLeftPanel       = new(_settings.LeftPanelWidth);
        HeightLeftPanelChild = new(_settings.LeftPanelChildHeight, GridUnitType.Star);
        MainWindowWidth      = _settings.Width;
        MainWindowHeight     = _settings.Height;
        SelectedView         = _settings.SelectedView;
        SelectedCollection   = _settings.SelectedCollection;

        _ = FromSerializable(SerializableCollection.LoadFromFile());

        RightPanel = new RightPanelViewModel(this);
        LeftPanel  = new LeftPanelViewModel(this);

        if (Collections.Count > 0)
        {
            SelectedCollection = SelectedCollection is null || SelectedCollection > Collections.Count || SelectedCollection < 0 ? Collections.Count - 1 : SelectedCollection;
            _activeCollection = new(Collections[SelectedCollection.Value]);
        }
        else { _activeCollection = new(CollectionItem.FromImages(new SourceList<ImageItem> { }, "")); SelectedCollection = null; }

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
            .Bind(out _images)
            .Subscribe();

        _images.ToObservableChangeSet()
               .AutoRefresh(x => x.IsSelected)
               .Filter(x => x.IsSelected)
               .Bind(out _selectedImages)
               .Subscribe();
    }
    partial void OnSelectedViewChanged(string value)
    {
        ThumbnailHelper.CurrentSelectedView = value;
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
    private async Task FromSerializable(SerializableCollection[] sCollections)
    {
        foreach (var sCollection in sCollections)
        {
            var newCollection = CollectionItem.FromImages(new SourceList<ImageItem>(), sCollection.Name);
            var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in sCollection.FoldersPaths)
            {
                newCollection.AddWatcher(folder);
                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                     .Where(f => FileFilters.AllowedExtensions.Contains(System.IO.Path.GetExtension(f)));
                uniquePaths.UnionWith(files);
            }

            var tasks = sCollection.CollectionImages
                                       .Select(img => ImageItem.FromPathAsync(
                                           img.PathImage,
                                           img.Name,
                                           img.PathThumbnail,
                                           SerializableCollection.ParseOrDefault(img.BgColorHex)))
                                       .ToList();

            var items = (await Task.WhenAll(tasks)).ToList();
            foreach (var item in items) { if (uniquePaths.Contains(item.PathImage)) { newCollection.UniqueFoldersImagesPaths[item.PathImage] = item; } }

            var newUnique = uniquePaths.Except(sCollection.CollectionImages.Select(img => img.PathImage).ToHashSet());
            var newTasks = newUnique.Select(path => ImageItem.FromPathAsync(path, null, null, null)).ToList();
            var newItems = await Task.WhenAll(newTasks);

            foreach (var item in newItems) { newCollection.UniqueFoldersImagesPaths[item.PathImage] = item; }

            items.AddRange(newItems);
            newCollection.CollectionImages.AddRange(items);
            Collections.Add(newCollection);
        }
        SelectedCollection = 0;
    }
    
    public void SaveSettings()
    {
        _settings.IsShuffled           = LeftPanel.IsShuffled;
        _settings.TimeImage            = LeftPanel.TimeImage ?? "0";
        _settings.SelectedView         = SelectedView;
        _settings.SelectedCollection   = SelectedCollection ?? 0;
        _settings.LeftPanelWidth       = (float)WidthLeftPanel.Value;
        _settings.LeftPanelChildHeight = (float)HeightLeftPanelChild.Value;
        _settings.Width                = MainWindowWidth;
        _settings.Height               = MainWindowHeight;

        SettingsService.Save(_settings);
    }
}

public class BaseWindow : Window
{
    protected readonly MenuItem AlwaysOnTopItem;
    protected readonly Grid LayoutGrid = new();
    protected readonly ZoomBorder PanAndZoomBorder;
    protected readonly Image Picture = new () { Stretch = Stretch.Uniform};
    public readonly TiledImage BaseWindowTiledImage = new();
    private readonly int _tileSize = 256;
    private static PixelRect screenSize;
    protected StackPanel ControlsImagePanel = new() { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(10, 0, 0, 34) };
    private readonly TextBlock _missingFileText;
    public readonly ColorPicker PickerColor;
    private Rectangle? _pipettPreview;
    private MouseKeyboardHook? _mouseKeyboardHook;
    private DispatcherTimer? _pipettTimer;
    private Color _currentColor;
    private float _currentRotation = 0;
    private bool _isFlippedHorizontal = false;
    private bool _isFlippedVertical = false;
    public BaseWindow()
    {
        Title                             = "";
        MinWidth                          = 120;
        MinHeight                         = 160;
        Width                             = 800;
        Height                            = 600;
        Icon                              = new WindowIcon(AppResources.AppIconPath);
        Background                        = Brushes.Transparent;
        SystemDecorations                 = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;

        var titleBar = new Grid { Height = 32, VerticalAlignment = VerticalAlignment.Top, Background = Brushes.Transparent, Opacity = 0 };
        titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, HorizontalAlignment = HorizontalAlignment.Right, };
        var btnMinimize = CreateWindows10Button("\uE921");
        var btnMaximize = CreateWindows10Button("\uE922");
        var btnClose    = CreateWindows10Button("\uE8BB");
        btnMinimize.Click += (_, __) => WindowState = WindowState.Minimized;
        btnMaximize.Click += (_, __) => { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; };
        btnClose.Click    += (_, __) => Close();

        buttonStack.Children.Add(btnMinimize);
        buttonStack.Children.Add(btnMaximize);
        buttonStack.Children.Add(btnClose);

        titleBar.Children.Add(buttonStack);
        titleBar.DoubleTapped   += (_, _) => { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; };
        titleBar.PointerEntered += (_, _) => { titleBar.Opacity = 1; };
        titleBar.PointerExited  += (_, _) => { titleBar.Opacity = 0; };

        this.PointerMoved       += (s, e) =>
        {
            var edge = GetResizeEdge(e.GetPosition(this));
            this.Cursor = edge.HasValue ? GetCursorForEdge(edge.Value) : new Cursor(StandardCursorType.Arrow);
        };
        this.PointerPressed     += (s, e) =>
        {
            var edge = GetResizeEdge(e.GetPosition(this));
            if (edge.HasValue && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginResizeDrag(edge.Value, e);
        };
        titleBar.PointerPressed += (s, e) =>
        {
            var pos = e.GetPosition(titleBar);
            bool onResizeBorder = pos.X <= 6 || pos.X >= Width - 6 || pos.Y <= 6 || pos.Y >= Height - 6;
            if (!onResizeBorder && e.GetCurrentPoint(titleBar).Properties.IsLeftButtonPressed) { BeginMoveDrag(e); }
        };

        _missingFileText = new TextBlock
        {
            Text                = "",
            FontSize            = 10,
            Foreground          = Brushes.White,
            FontWeight          = FontWeight.Bold,
            Background          = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            TextAlignment       = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            MinWidth            = 88, 
            Padding             = new Thickness(4),
            IsHitTestVisible    = false,
            IsVisible           = false
        };

        var pictureOverlay = new Grid();
        pictureOverlay.Children.Add(Picture);
        pictureOverlay.Children.Add(_missingFileText);

        PanAndZoomBorder = new ZoomBorder
        {
            Child        = pictureOverlay,
            Background   = Brushes.Gray,
            ClipToBounds = true,
            Focusable    = true,
            EnablePan    = true,
            EnableZoom   = true,
            PanButton    = ButtonName.Left
        };

        PanAndZoomBorder.KeyDown += ZoomBorder_KeyDown;

        LayoutGrid.Children.Add(PanAndZoomBorder);
        LayoutGrid.Children.Add(titleBar);

        Content              = LayoutGrid;
        var headerGrid       = new Grid { ColumnDefinitions = new ColumnDefinitions("15,*,Auto"), VerticalAlignment = VerticalAlignment.Center, ColumnSpacing = 15 };
        var checkmark        = new TextBlock { Text = Topmost ? "✓" : string.Empty, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 16 };
        var alwaysOnTopLabel = new TextBlock { Text = "Always on top", VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Margin = new Thickness(1, 0, 0, 0) };
        var shortcutHint     = new TextBlock { Text = "Ctrl+T", Opacity = 0.7, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };

        headerGrid.Children.Add(checkmark);
        headerGrid.Children.Add(alwaysOnTopLabel);
        headerGrid.Children.Add(shortcutHint);
        Grid.SetColumn(checkmark, 0);
        Grid.SetColumn(alwaysOnTopLabel, 1);
        Grid.SetColumn(shortcutHint, 2);

        AlwaysOnTopItem       = new MenuItem { Header = headerGrid, StaysOpenOnClick = true };
        AlwaysOnTopItem.Click += (_, _) => { Topmost = !Topmost; checkmark.Text = Topmost ? "✓" : string.Empty; };

        var pickerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("15,*,Auto"), VerticalAlignment = VerticalAlignment.Center, ColumnSpacing = 15 };
        PickerColor    = new ColorPicker { Width = 65, Height = 23, VerticalAlignment = VerticalAlignment.Center };

        var pipettButton = new Button { Width = 25, Height = 25, CornerRadius = new CornerRadius(6), VerticalAlignment = VerticalAlignment.Center, };
        var canvas       = new Canvas { Width = 15, Height = 15, };
        var pathSVG      = new Avalonia.Controls.Shapes.Path { Stroke = Brushes.White, StrokeThickness = 1.3, StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, Data = Geometry.Parse("M12.401 5.568l-.743 1.3 3.6187 2.1855L16.859 6.538c.6035-.9993.2827-2.2986-.8713-2.7358-.9993-.6035-2.5967-.5462-3.592 1.775ZM10 5.9A1.05 1.05 0 009.2 7.8l6.916 4.187a1.05 1.05 0 00.805-1.94L10 5.9Zm.5566 2.7241-4.6613 7.718c-.3427.5673-.482 1.2344-.3952 1.8915l.1337 1.0116c.1024.7745.7319 1.3722 1.5106 1.4344.6255.0499 1.2256-.2578 1.55-.795l5.4809-9.075-3.6187-2.1855Z") };
        Canvas.SetLeft(pathSVG, -4);
        Canvas.SetTop(pathSVG, -4);
        canvas.Children.Add(pathSVG);

        pipettButton.Content = canvas;
        pipettButton.Click   += (_, __) => { StartPipett(); PanAndZoomBorder.ContextMenu!.Close(); };

        var pickerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        pickerStack.Children.Add(PickerColor);
        pickerStack.Children.Add(pipettButton);

        pickerGrid.Children.Add(pickerStack);
        Grid.SetColumn(pickerStack, 1);

        var shortcutPipettHint = new TextBlock { Text = "Alt", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
        pickerGrid.Children.Add(shortcutPipettHint);
        Grid.SetColumn(shortcutPipettHint, 2);

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(AlwaysOnTopItem);
        contextMenu.Items.Add(pickerGrid);

        PanAndZoomBorder.ContextMenu = contextMenu;
        PanAndZoomBorder.ZoomChanged += ZoomBorder_ZoomChanged;

        var mirrorPanel            = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 0, 0, 5) };
        var flipVerticalButton     = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "⇅", FontSize = 18, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2)}};
        flipVerticalButton.Click   += FlipVertical_Click;
        var flipHorizontalButton   = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "⇋", FontSize = 18, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 3) }};
        flipHorizontalButton.Click += FlipHorizontal_Click;
        mirrorPanel.Children.Add(flipVerticalButton);
        mirrorPanel.Children.Add(flipHorizontalButton);

        var rotatePanel         = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0) };
        var rotateLeftButton    = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "⟲", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 3) }};
        rotateLeftButton.Click  += RotateLeft_Click;
        var rotateRightButton   = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "⟳", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 3) }};
        rotateRightButton.Click += RotateRight_Click;
        var resetImageButton    = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "↺", TextAlignment = TextAlignment.Center, FontSize = 13, VerticalAlignment = VerticalAlignment.Center } };
        resetImageButton.Click  += ResetTransform_Click;
        rotatePanel.Children.Add(rotateLeftButton);
        rotatePanel.Children.Add(resetImageButton);
        rotatePanel.Children.Add(rotateRightButton);
        ControlsImagePanel.Children.Add(mirrorPanel);
        ControlsImagePanel.Children.Add(rotatePanel);

        LayoutGrid.Children.Add(ControlsImagePanel);

        this.PointerEntered += (_, _) => { ControlsImagePanel.IsVisible = true; };
        this.PointerExited  += (_, _) => { ControlsImagePanel.IsVisible = false; };

        screenSize = Screens.Primary != null ? Screens.Primary.Bounds : new PixelRect(0, 0, 1920, 1080);
    }
    public void StartPipett()
    {
        if (_pipettPreview != null) return;
#if WINDOWS
        _mouseKeyboardHook              = new MouseKeyboardHook();
        _mouseKeyboardHook.LeftClick    += () => { PickerColor.Color = _currentColor;};
        _mouseKeyboardHook.MiddleClick  += StopPipett;
        _mouseKeyboardHook.EnterPressed += StopPipett;
        _mouseKeyboardHook.EscPressed   += StopPipett;
#endif
        _pipettPreview = new Rectangle
        {
            Width               = 50,
            Height              = 50,
            Margin              = new Thickness(5),
            Fill                = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Bottom,
        };

        LayoutGrid.Children.Add(_pipettPreview);

        _pipettTimer      = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _pipettTimer.Tick += (_, __) => UpdatePipett();
        _pipettTimer.Start();
    }

    private void UpdatePipett()
    {
        if (_mouseKeyboardHook == null || _pipettPreview == null) return;

        var (x, y)          = MouseKeyboardHook.GetCursorPosition();
        var (r, g, b)       = MouseKeyboardHook.GetPixelColor(x, y);
        _currentColor       = Color.FromRgb(r, g, b);
        _pipettPreview.Fill = new SolidColorBrush(_currentColor);
    }
    public void StopPipett()
    {
        _pipettTimer?.Stop();
        _pipettTimer = null;

        if (_pipettPreview != null)
        {
            LayoutGrid.Children.Remove(_pipettPreview);
            _pipettPreview = null;
        }
        _mouseKeyboardHook?.Dispose();
        _mouseKeyboardHook = null;
    }

    protected void LoadImage(string path, IBrush color)
    {
        if (Picture.Source is IDisposable disposable) { disposable.Dispose(); Picture.Source = null; }

        if (File.Exists(path))
        {
            _missingFileText.IsVisible = true;
            _missingFileText.Text = "Loading...";
            try
            {
                Picture.Source = new Bitmap(path);
                PanAndZoomBorder.Background = color;
                PickerColor.Color = (color as SolidColorBrush)?.Color ?? Colors.Gray;
                _missingFileText.Text = "";
                _missingFileText.IsVisible = false;
            }
            catch (Exception ex)
            {
                Picture.Source = new Bitmap("Assets/MissingImage.png");
                Picture.Opacity = 0.5;
                _missingFileText.Text = $"Failed to load:\n{path}\n{ex.Message}";
            }
        }
        else
        {
            Picture.Source = new Bitmap("Assets/MissingImage.png");
            Picture.Opacity = 0.5;
            _missingFileText.Text = $"File not found:\n{path}\nIt may have been deleted, renamed or moved.";
        }
    }
    private void ApplyTransform()
    {
        var transformGroup = new TransformGroup();

        var scaleX = _isFlippedHorizontal ? -1 : 1;
        var scaleY = _isFlippedVertical ? -1 : 1;
        transformGroup.Children.Add(new ScaleTransform(scaleX, scaleY));
        transformGroup.Children.Add(new RotateTransform(_currentRotation));
        BaseWindowTiledImage.RenderTransform = transformGroup;
        BaseWindowTiledImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    }
    private void FlipHorizontal_Click(object? sender, RoutedEventArgs e)
    {
        _isFlippedHorizontal = !_isFlippedHorizontal;
        ApplyTransform();
    }

    private void FlipVertical_Click(object? sender, RoutedEventArgs e)
    {
        _isFlippedVertical = !_isFlippedVertical;
        ApplyTransform();
    }

    private void RotateLeft_Click(object? sender, RoutedEventArgs e)
    {
        _currentRotation -= 90;
        ApplyTransform();
    }

    private void RotateRight_Click(object? sender, RoutedEventArgs e)
    {
        _currentRotation += 90;
        ApplyTransform();
    }

    private void ResetTransform_Click(object? sender, RoutedEventArgs e)
    {
        _currentRotation     = 0;
        _isFlippedHorizontal = false;
        _isFlippedVertical   = false;
        ApplyTransform();
    }
    private void ZoomBorder_ZoomChanged(object? sender, ZoomChangedEventArgs e)
    {
        BaseWindowTiledImage.ZoomLevel  = (float)e.ZoomX;
        BaseWindowTiledImage.PanOffsetX = (float)e.OffsetX;
        BaseWindowTiledImage.PanOffsetY = (float)e.OffsetY;
        BaseWindowTiledImage.InvalidateVisual();
    }

    private void ZoomBorder_KeyDown(object? sender, KeyEventArgs e)
    {
        if      (e.Key == Key.R)
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
        else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) { StartPipett(); }
        else if (e.Key == Key.Escape) this.Close();
    }
    private static Button CreateWindows10Button(string text)
    {
        var btn = new Button
        {
            Width                      = 45,
            Height                     = 32,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            Padding                    = new Thickness(0),
            Content                    = new TextBlock { Text = text, FontFamily = new FontFamily("Segoe MDL2 Assets"), RenderTransform = new ScaleTransform(0.7, 0.7), FontSize = 16, }
        };
        btn.Classes.Add("win-caption");
        return btn;
    }
    private WindowEdge? GetResizeEdge(Point p)
    {
        const float border = 6;
        bool left   = p.X <= border;
        bool right  = p.X >= Width - border;
        bool top    = p.Y <= border;
        bool bottom = p.Y >= Height - border;

        if (top && left)     return WindowEdge.NorthWest;
        if (top && right)    return WindowEdge.NorthEast;
        if (bottom && left)  return WindowEdge.SouthWest;
        if (bottom && right) return WindowEdge.SouthEast;
        if (top)             return WindowEdge.North;
        if (bottom)          return WindowEdge.South;
        if (left)            return WindowEdge.West;
        if (right)           return WindowEdge.East;

        return null;
    }
    private static Cursor GetCursorForEdge(WindowEdge edge)
    {
        return edge switch
        {
            WindowEdge.North     => new Cursor(StandardCursorType.TopSide),
            WindowEdge.South     => new Cursor(StandardCursorType.BottomSide),
            WindowEdge.West      => new Cursor(StandardCursorType.LeftSide),
            WindowEdge.East      => new Cursor(StandardCursorType.RightSide),
            WindowEdge.NorthWest => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast => new Cursor(StandardCursorType.TopRightCorner),
            WindowEdge.SouthWest => new Cursor(StandardCursorType.BottomLeftCorner),
            WindowEdge.SouthEast => new Cursor(StandardCursorType.BottomRightCorner),
            _ => new Cursor(StandardCursorType.Arrow)
        };
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        StopPipett();
    }
}
