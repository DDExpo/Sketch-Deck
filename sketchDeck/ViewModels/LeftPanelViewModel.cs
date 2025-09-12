using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using sketchDeck.CustomAxaml;
using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class LeftPanelViewModel(MainWindowViewModel parent) : ObservableObject
{
    public MainWindowViewModel Parent { get; } = parent;
    public string[] Views { get; } = ["Gigantic", "Big", "Medium", "Small", "Details"];
    [ObservableProperty]
    private string? _timeImage = "0";
    [ObservableProperty]
    private bool _isShuffled = false;
    public async Task LoadFolderAsync(string[] files, CancellationToken token)
    {
        Parent.ClearImages();

        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            var item = await ImageItem.FromPathAsync(file);
            Parent.CurrentImagePath = file;
            Parent.AddImage(item);
        }
    }
    public async Task ImagesLoader(string[] paths)
    {
        if (paths is null || paths.Length == 0) return;
        using var cts = new CancellationTokenSource();

        var popup = new ProgressPopup { DataContext = Parent };

        popup.Closed += (_, __) => cts.Cancel();

        popup.Show();

        try
        {
            await LoadFolderAsync(paths, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (popup.IsVisible)
                popup.Close();
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
public class SessionWindow : BaseWindow
{
    private readonly Window mainWindow;
    private readonly Button _pausePlayButton;
    private readonly TextBlock _timeText;
    private readonly TextBlock _counterText;
    private readonly TextBlock _playButton = new() { Text = "▶", TextAlignment = TextAlignment.Center, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) };
    private readonly DispatcherTimer? _slideshowTimer;
    private int[] _shuffledIndices;
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

        var overlayDock = new DockPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(10, 0, 20, 10) };
        var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Spacing = 5, IsVisible = true};

        var prevButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = "<", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) }};
        prevButton.Click += (_, _) => ShowPrevious();
        _pausePlayButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = _playButton, };
        _pausePlayButton.Click += (_, _) => TogglePause();
        var nextButton = new Button { Width = 24, Height = 19, Padding = new Thickness(0), Content = new TextBlock { Text = ">", TextAlignment = TextAlignment.Center, FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -1, 0, 0) }};
        nextButton.Click += (_, _) => ShowNext();

        _counterText = new TextBlock { Text = $"{_currentIndex + 1} / {_imArray.Count}", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontWeight = FontWeight.Bold, };
        _timeText = new TextBlock { Text = _timePerImage > 0 ? FormatTime(_timePerImage) : "", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontWeight = FontWeight.Bold, Margin = new Thickness(10, 0, 0, 0)};

        controlsPanel.Children.Add(prevButton);
        if (_timePerImage > 0) { controlsPanel.Children.Add(_pausePlayButton); }
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
        this.PointerEntered += (_, _) => controlsPanel.IsVisible = true;
        this.PointerExited += (_, _) => controlsPanel.IsVisible = false;
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
        _pausePlayButton.Content =  new Path{ Data = Geometry.Parse("M 0 0 H 4 V 16 H 0 Z M 8 0 H 12 V 16 H 8 Z"), Fill = Brushes.White, Stretch = Stretch.Uniform,
            Width = 11, Height = 11, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin= new Thickness(3,0,0,0)};
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

        if (Picture.Source is IDisposable disposable)
            disposable.Dispose();
        Picture.Source = null;
        _slideshowTimer?.Stop();
        mainWindow.Show();
    }
}
