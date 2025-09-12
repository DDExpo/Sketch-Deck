using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class LeftPanel : UserControl
{
    public ObservableCollection<CollectionItem> Collections { get; } = [];
    public LeftPanel()
    {
        InitializeComponent();
        CollectionsList.ItemsSource = Collections;

        Collections.Add(new CollectionItem { Name = "collection_A", Count = 44 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_A", Count = 44 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });
        Collections.Add(new CollectionItem { Name = "collection_B", Count = 55 });

        this.AttachedToVisualTree += async (_, __) =>
        {
            if (DataContext is not LeftPanelViewModel vm) return;

            await Task.Yield();
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg"
            };
            var imagePaths = Directory.EnumerateFiles(@"D:\Workspace\C#\Projects\Sketch-Deck\test_light", "*.*", SearchOption.AllDirectories)
                                      .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
                                      .ToArray();
            await vm.ImagesLoader(imagePaths);
        };
    }

    private void StartSessionButton_Clicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is not LeftPanelViewModel vm) return;

        if (!int.TryParse(vm.TimeImage, out int time)) { time = 0; vm.TimeImage = "0"; }
        if (time > 300000) { time = 300000; vm.TimeImage = "300000"; }
        else if (time < 0) { time = 0; vm.TimeImage = "0"; }
        var session = new SessionWindow(vm.Parent.Images, vm.IsShuffled, time);
        session.Show();
    }
    private async void OpenFilesButton_Clicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is not LeftPanelViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Add images",
            AllowMultiple = true,
            FileTypeFilter = [
                new FilePickerFileType("Image files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg"],
                    MimeTypes = ["image/png", "image/jpeg"]
                }
            ]
        });

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => p is not null)
            .ToArray()!;

        await vm.ImagesLoader(paths!);
    }
    private async void OpenFolderButton_Clicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is not LeftPanelViewModel vm) return;
        var topLevel = TopLevel.GetTopLevel(this);

        var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Add images",
            AllowMultiple = true,
        });
        var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg"
        };

        var paths = new List<string>();

        foreach (var folder in folders)
        {
            var folderPath = folder.TryGetLocalPath();
            if (folderPath is not null && Directory.Exists(folderPath))
            {
                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                     .Where(f => imageExtensions.Contains(Path.GetExtension(f)));

                paths.AddRange(files);
            }
        }

        await vm.ImagesLoader([.. paths]);
    }
    private void AlwaysOnTop_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (window is not null)
        {
            window.Topmost = !window.Topmost;
        }
    }

    private void CollectionsList_OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Text))
        {
            var dropped = e.Data.GetText();
            if (!string.IsNullOrWhiteSpace(dropped))
            {
                Collections.Add(new CollectionItem { Name = dropped, Count = 1 });
            }
        }
    }

    private void DeleteCollection_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is CollectionItem item)
        {
            Collections.Remove(item);
        }
    }
}

public class CollectionItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}
