using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using sketchDeck.Models;
using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class MainWindow : Window
{
    private bool _isDragOver = false;
    private readonly DispatcherTimer _saveTimer;
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SaveSettings();
                SerializableCollection.SaveToFile([.. vm.Collections]);
            }
        };
        _saveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(15)
        };
        _saveTimer.Tick += (_, __) => AutoSave();
        _saveTimer.Start();
    }
    private void AutoSave()
    {
        try { if (DataContext is MainWindowViewModel vm) { SerializableCollection.SaveToFile([.. vm.Collections]); } }
        catch (Exception) { }
    }
    private void DropWindow_DragEnter(object sender, DragEventArgs e)
    {
        if (_isDragOver || e.Data.GetDataFormats().Contains("ImageItems")) return;

        _isDragOver = true;
        Overlay.IsVisible = true;
        var files = e.Data.GetFiles();
        if (e.Data.Contains(DataFormats.Files)) { e.DragEffects = DragDropEffects.Copy; }
        else { e.DragEffects = DragDropEffects.None; }
        e.Handled = true;
    }
    private void DropWindow_DragLeave(object? sender, RoutedEventArgs e)
    {
        if (!_isDragOver) return;

        _isDragOver = false;
        Overlay.IsVisible = false;
        e.Handled = true;
    }

    private async void DropWindow_Drop(object sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files == null || DataContext is not MainWindowViewModel vm) return;
        _isDragOver = false;
        Overlay.IsVisible = false;
        var pathFiltered = FilterImagePaths(files);
        await vm.LeftPanel.ImagesLoader(pathFiltered, (Window)this.GetVisualRoot()!);
    }
    private static string[] FilterImagePaths(IEnumerable<IStorageItem> files)
    {
        var result = new List<string>();

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path == null) continue;

            if (File.Exists(path))
            {
                if (FileFilters.AllowedExtensions.Contains(Path.GetExtension(path).ToLower()))
                    result.Add(path);
            }
            else if (Directory.Exists(path))
            {
                var folderFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(f => FileFilters.AllowedExtensions.Contains(Path.GetExtension(f).ToLower()));
                result.AddRange(folderFiles);
            }
        }
        return [.. result];
    }
}
