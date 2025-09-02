using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using System.IO;
using sketchDeck.Models;
using sketchDeck.ViewModels;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace sketchDeck.Views;

public partial class MiddlePanel : UserControl
{
    public MiddlePanel()
    {
        InitializeComponent();
        this.AttachedToVisualTree += async (_, __) =>
        {
            await Task.Yield();

            if (DataContext is MiddlePanelViewModel)
            {
                await OpenFolder(@"D:\Workspace\C#\Projects\Sketch-Deck\test");
            }
        };
    }

    private async Task OpenFolder(string path)
    {
        if (DataContext is not MiddlePanelViewModel vm)
            return;

        using var cts = new CancellationTokenSource();

        var popup = new ProgressPopup { DataContext = vm };

        // When the popup closes, cancel the load
        popup.Closed += (_, __) => cts.Cancel();

        popup.Show();

        try
        {
            await vm.LoadFolderAsync(path, cts.Token);
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
    private void OnImageDoubleTapped(object sender, TappedEventArgs args)
    {
        if (sender is Control control && control.DataContext is ImageItem item)
        {
            using var stream = File.OpenRead(item.PathImage);
            var bitmap = new Bitmap(stream);

            var preview = new PreviewWindow(bitmap);
            preview.Show();
        }
    }
}