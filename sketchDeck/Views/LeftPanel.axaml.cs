using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        CollectionsList.ContainerPrepared += (s, e) =>
        {
            if (e.Container is ListBoxItem lbi)
            {
                DragDrop.SetAllowDrop(lbi, true);
                lbi.AddHandler(DragDrop.DragOverEvent, ListBoxItem_DragOver);
                lbi.AddHandler(DragDrop.DropEvent, ListBoxItem_Drop);
                lbi.AddHandler(DragDrop.DragLeaveEvent, ListBoxItem_DragLeave);
            }
        };
    }
    private void ListBoxItem_DragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not LeftPanelViewModel vm) return;
        if (sender is ListBoxItem lbi && lbi.DataContext is CollectionItem item && item != vm.Parent.Collections[vm.Parent.SelectedCollection!.Value])
        {
            lbi.Background = Brushes.DimGray;
            e.DragEffects = DragDropEffects.Copy;
        }
        else { e.DragEffects = DragDropEffects.None; }
        e.Handled = true;
    }
    private void ListBoxItem_DragLeave(object? sender, DragEventArgs e)
    {
        if (sender is ListBoxItem lbi) { lbi.Background = Brushes.Transparent; }
        e.Handled = true;
    }
    private void ListBoxItem_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not LeftPanelViewModel vm) return;
        var curCollection = vm.Parent.Collections[vm.Parent.SelectedCollection!.Value];
        if (sender is ListBoxItem lbi && lbi.DataContext is CollectionItem item && item != curCollection)
        {
            lbi.Background = Brushes.Transparent;

            Console.WriteLine(vm.Parent.SelectedImages);
            if (vm.Parent.SelectedImages != null)
            {
                item.CollectionImages.AddRange(vm.Parent.SelectedImages);
                curCollection.CollectionImages.RemoveMany(vm.Parent.SelectedImages);
            }
        }
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
        var paths = files.Select(f => f.TryGetLocalPath())
                         .Where(p => p is not null).ToArray();

        if (paths.Length > 0) await vm.ImagesLoader(paths!, (Window)this.GetVisualRoot()!);
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
        var paths = new List<string>();

        foreach (var folder in folders)
        {
            var folderPath = folder.TryGetLocalPath();
            if (folderPath is not null && Directory.Exists(folderPath))
            {
                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                     .Where(f => FileFilters.AllowedExtensions.Contains(Path.GetExtension(f)));

                paths.AddRange(files);
            }
        }
        if (paths.Count > 0) await vm.ImagesLoader([.. paths], (Window)this.GetVisualRoot()!);
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
    private async void EditCollection_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;

        var btn = (Button)sender!;
        var item = (CollectionItem)btn.DataContext!;
    
        var dialog = new SaveEditCollectionWindow(item);
        var result = await dialog.ShowDialog<(string, string[], string[])?>(window!);

        if (result is not null)
        {
            var (name, foldersPaths, deletedPaths) = result.Value;
            if (DataContext is LeftPanelViewModel vm)
            {
                _ = vm.EditCollectionAsync(item, name, foldersPaths, deletedPaths, window!);
            }
        }
    }
    private async void CreateCollection_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        var dialog = new SaveEditCollectionWindow(null);
        var result = await dialog.ShowDialog<(string, string[], string[])?>(window!);

        if (result is not null)
        {
            var (name, foldersPaths, _) = result.Value;
            if (DataContext is LeftPanelViewModel vm)
            {
                _ = vm.CreateCollectionAsync(name, foldersPaths, window!);
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
                item.Dispose(true);
                break;
            case DialogResult.No:
                vm.Parent.Collections.Remove(item);
                vm.Parent.Collections[0].CollectionImages.AddRange(item.CollectionImages.Items);
                vm.Parent.SelectedCollection = 0;
                item.Dispose(false);
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
