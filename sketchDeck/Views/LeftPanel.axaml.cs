using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

using DynamicData;

using sketchDeck.CustomAxaml;
using sketchDeck.Models;
using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class LeftPanel : UserControl
{
    public LeftPanel()
    {
        InitializeComponent();
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
            await vm.ImagesLoader(imagePaths, (Window)this.GetVisualRoot()!);
        };
    }
    
    private void StartSessionButton_Clicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is not LeftPanelViewModel vm || vm.Parent.Images == null || vm.Parent.Images.Count == 0) return;

        if (!int.TryParse(vm.TimeImage, out int time)) { time = 0; vm.TimeImage = "0"; }
        if (time > 86400) { time = 86400; vm.TimeImage = "86400"; }
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

        await vm.ImagesLoader(paths!, (Window)this.GetVisualRoot()!);
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
        await vm.ImagesLoader([.. paths], (Window)this.GetVisualRoot()!);
    }
    
    private void OnNameDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is CollectionItem item) { item.IsEditing = true; }
    }
    private void EditTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CollectionItem item) {item.IsEditing = false;}
    }
    private void EditTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CollectionItem item)
        {
            if (e.Key == Key.Enter) { item.IsEditing = false;}
            else if (e.Key == Key.Escape) {item.IsEditing = false;}
        }
    }

    private async void CreateCollection_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        var dialog = new SaveCollectionWindow();
        var result = await dialog.ShowDialog<string?>(window!);

        if (result != null)
        {
            if (DataContext is LeftPanelViewModel vm)
            {
                vm.SaveCollection(result);
            }
        }
    }
    private async void DeleteCollection_Click(object? sender, RoutedEventArgs e)
    {
        Window dialog;
        if (DataContext is not LeftPanelViewModel vm) return;
        var btn = (Button)sender!;
        var item = (CollectionItem)btn.DataContext!;

        if (vm.Parent.Collections.Count < 2)
        {
            dialog = new YesNoCancelDialog("", false);
        } else {
            dialog = new YesNoCancelDialog(item.Name, true);
        }
        var window = this.VisualRoot as Window;
        var result = await dialog.ShowDialog<DialogResult>(window!);

        switch (result)
        {
            case DialogResult.Yes:
                vm.Parent.Collections.Remove(item);
                if (vm.Parent.Collections.Count > 0) vm.Parent.SelectedCollection = 0; 
                item.Dispose();
                break;
            case DialogResult.No:
                vm.Parent.Collections.Remove(item);
                vm.Parent.Collections[0].CollectionImages.AddRange(item.CollectionImages.Items);
                vm.Parent.SelectedCollection = 0;
                break;
            case DialogResult.Cancel:
                break;
        }
    }

    private void AlwaysOnTop_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        window!.Topmost = !window.Topmost;
    }
}
