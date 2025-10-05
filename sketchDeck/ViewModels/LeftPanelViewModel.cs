using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;

using sketchDeck.CustomAxaml;
using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class LeftPanelViewModel(MainWindowViewModel parent) : ObservableObject
{
    public MainWindowViewModel Parent { get; } = parent;
    public string[] Views { get; } = ["Gigantic", "Big", "Medium", "Small", "Details"];
    [ObservableProperty] private string? _timeImage = parent._settings.TimeImage;
    [ObservableProperty] private bool _isShuffled = parent._settings.IsShuffled;
    public async Task EditCollectionAsync(CollectionItem item, string name, string[]? fPaths, string[]? deletedPaths, Window window)
    {
        item.Name = name;
        if (fPaths is null) return;
        if (deletedPaths is not null) { foreach (var dp in deletedPaths) { item.RemoveWatcher(dp);}}

        List<string> paths = [];
        foreach (var folder in fPaths)
        {
            if (!item.Watchers.ContainsKey(folder))
            {
                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                     .Where(f => FileFilters.AllowedExtensions.Contains(Path.GetExtension(f)));
                paths.AddRange(files);
            }
            item.AddWatcher(folder);
        }
        await ImagesLoader([.. paths], window, true);
    }
    public async Task CreateCollectionAsync(string name, string[]? fPaths, Window window)
    {
        var newCollection = CollectionItem.FromImages(new SourceList<ImageItem>(), name);
        List<string> paths = [];
        if (fPaths is not null)
        {
            foreach (var folder in fPaths)
            {
                newCollection.AddWatcher(folder);
                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                     .Where(f => FileFilters.AllowedExtensions.Contains(Path.GetExtension(f)));
                paths.AddRange(files);
            }
        }
        Parent.Collections.Add(newCollection);
        Parent.SelectedCollection = Parent.Collections.Count - 1;
        await ImagesLoader([.. paths], window, true);
    }

    public async Task LoadPathsAsync(string[] files, CancellationToken ct, bool mustBeInUnique = false)
    {
        var tasks = files.Select(file => {
            ct.ThrowIfCancellationRequested();
            return ImageItem.FromPathAsync(file, null, null, null);
        });
        var newCollectionImages = await Task.WhenAll(tasks);
        var curCollection = Parent.Collections[Parent.SelectedCollection!.Value];

        if (mustBeInUnique)
        {
            foreach (var item in newCollectionImages)
            {
                curCollection.UniqueFoldersImagesPaths[item.PathImage] = item;
            }
        }
        await Dispatcher.UIThread.InvokeAsync(() => { curCollection.CollectionImages.AddRange(newCollectionImages);});
    }
    public async Task ImagesLoader(string[] paths, Window window, bool mustBeInUnique=false)
    {
        if (Parent.SelectedCollection is null ||
            Parent.SelectedCollection < 0 ||
            Parent.SelectedCollection >= Parent.Collections.Count)
            return;

        using var cts = new CancellationTokenSource();

        var popup = new ProgressPopup();
        popup.Closed += (_, __) => cts.Cancel();
        _ = popup.ShowDialog(window);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        
        try { await Task.Run(() => LoadPathsAsync(paths, cts.Token, mustBeInUnique)); }
        catch (OperationCanceledException) { }
        finally { if (popup.IsVisible) popup.Close(); }
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
public class SessionWindow : BaseWindow
{
    private readonly Window mainWindow;
    private readonly Button _pausePlayButton;
    private readonly TextBlock _timeText;
    private readonly TextBlock _counterText;
    private readonly TextBlock _playButton = new() { Text = "▶", TextAlignment = TextAlignment.Center, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) };
    private readonly DispatcherTimer? _slideshowTimer;
    private readonly int[] _shuffledIndices;
    private readonly ReadOnlyObservableCollection<ImageItem> _imArray;
    private int _currentIndex;
    private readonly int _timePerImage;
    private int _remainingSeconds;
    public SessionWindow(ReadOnlyObservableCollection<ImageItem> imArray, bool isShuffle, int timePerImage)
    {
        Title = "Session";

        var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
        mainWindow = lifetime.MainWindow!;
        mainWindow.Hide();
        _shuffledIndices = [.. Enumerable.Range(0, imArray.Count)];
        _imArray = imArray;
        if (isShuffle) { new Random().Shuffle(_shuffledIndices.AsSpan()); }
        _timePerImage = timePerImage;

        LoadImage(_imArray[_shuffledIndices[_currentIndex]].PathImage, _imArray[_shuffledIndices[_currentIndex]].BgColor);

        var overlayDock = new DockPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(10, 0, 0, 10) };
        var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, };
        var resetTimerButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "↻", TextAlignment = TextAlignment.Center, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0) } };
        resetTimerButton.Click += (_, _) => ResetTimer();
        var prevButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "<", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) } };
        prevButton.Click += (_, _) => ShowPrevious();
        _pausePlayButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = _playButton, };
        _pausePlayButton.Click += (_, _) => TogglePause();
        var nextButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = ">", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) } };
        nextButton.Click += (_, _) => ShowNext();

        _counterText = new TextBlock { Text = $"{_currentIndex + 1} / {_imArray.Count}", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontWeight = FontWeight.Bold, Margin = new Thickness(29,0,0,0), Effect = new DropShadowDirectionEffect{ Color = Colors.Black, BlurRadius = 3, ShadowDepth = 0, Direction = 0, Opacity = 1}};
        _timeText = new TextBlock { Text = _timePerImage > 0 ? FormatTime(_timePerImage) : "", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontWeight = FontWeight.Bold, Margin = new Thickness(10, 0, 0, 0),  Effect = new DropShadowDirectionEffect{ Color = Colors.Black, BlurRadius = 3, ShadowDepth = 0, Direction = 0, Opacity = 1}};

        controlsPanel.Children.Add(prevButton);
        if (_timePerImage > 0) { controlsPanel.Children.Add(resetTimerButton); controlsPanel.Children.Add(_pausePlayButton); _counterText.Margin = new Thickness(0); }
        controlsPanel.Children.Add(nextButton);
        controlsPanel.Children.Add(_counterText);
        overlayDock.Children.Add(controlsPanel);
        if (_timePerImage > 0) { overlayDock.Children.Add(_timeText); }
        LayoutGrid.Children.Add(overlayDock);
        Content = LayoutGrid;

        if (_timePerImage > 0)
        {
            _slideshowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _remainingSeconds = timePerImage;
            _slideshowTimer.Stop();
            _slideshowTimer.Tick += (_, _) => UpdateTimer();
        }

        PickerColor.Color = (_imArray[_shuffledIndices[_currentIndex]].BgColor as SolidColorBrush)?.Color ?? Colors.Gray;
        PickerColor.ColorChanged += (_, __) =>
        {
            PanAndZoomBorder.Background = new SolidColorBrush(PickerColor.Color);
            _imArray[_shuffledIndices[_currentIndex]].BgColor = new SolidColorBrush(PickerColor.Color);
        };
        this.PointerEntered += (_, _) => { controlsPanel.IsVisible = true; };
        this.PointerExited += (_, _) => { controlsPanel.IsVisible = false; };
    }
    private void UpdateTimer()
    {
        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
            _timeText.Text = FormatTime(_remainingSeconds);
        }
        else
        {
            ShowNext();
            ResetTimer();
        }
    }
    private void ResetTimer()
    {
        _remainingSeconds = _timePerImage;
        _timeText.Text = FormatTime(_remainingSeconds);
    }
    private void ShowNext()
    {
        _currentIndex = (_currentIndex + 1) % _shuffledIndices.Length;
        _counterText.Text = $"{_currentIndex + 1} / {_imArray.Count}";
        LoadImage(_imArray[_shuffledIndices[_currentIndex]].PathImage, _imArray[_shuffledIndices[_currentIndex]].BgColor);
        ResetTimer();
    }
    private void ShowPrevious()
    {
        _currentIndex = (_currentIndex - 1 + _shuffledIndices.Length) % _shuffledIndices.Length;
        _counterText.Text = $"{_currentIndex + 1} / {_imArray.Count}";
        LoadImage(_imArray[_shuffledIndices[_currentIndex]].PathImage, _imArray[_shuffledIndices[_currentIndex]].BgColor);
        ResetTimer();
    }
    private void TogglePause()
    {
        if (_slideshowTimer!.IsEnabled)
        {
            _slideshowTimer.Stop();
            _pausePlayButton.Content = _playButton;
        }
        else
        {
            _slideshowTimer.Start();
            _pausePlayButton.Content = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse("M 0 0 H 4 V 16 H 0 Z M 8 0 H 12 V 16 H 8 Z"),
                Fill = Brushes.White,
                Stretch = Stretch.Uniform,
                Width = 11,
                Height = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(3, 0, 0, 0)
            };
        }
    }
    private static string FormatTime(int seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (Picture.Source is IDisposable disposable) disposable.Dispose();
        Picture.Source = null;
        _slideshowTimer?.Stop();
        mainWindow.Show();
    }
}
public enum DialogResult
{
    Cancel,
    Yes,
    No,
}