using System;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using sketchDeck.Models;

namespace sketchDeck.ViewModels;

public partial class RightPanelViewModel(MainWindowViewModel parent) : ObservableObject
{
    public MainWindowViewModel Parent { get; } = parent;
}

public class PreviewWindow : BaseWindow
{
    public PreviewWindow(ImageItem im)
    {
        LoadImage(im.PathImage, im.BgColor);
        ControlsImagePanel.Margin = new Avalonia.Thickness(10, 0, 0, 10);
        Content = LayoutGrid;

        PickerColor.Color = (im.BgColor as SolidColorBrush)?.Color ?? Colors.Gray;
        PickerColor.ColorChanged += (_, __) =>
        {
            PanAndZoomBorder.Background = new SolidColorBrush(PickerColor.Color);
            im.BgColor = new SolidColorBrush(PickerColor.Color);
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (Picture.Source is IDisposable disposable) { disposable.Dispose(); }
        Picture.Source = null;
    }
}