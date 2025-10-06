using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace sketchDeck.CustomAxaml;

public partial class SaveEditCollectionWindow : Window
{
    public string EnteredText => InputBox.Text ?? string.Empty; 
    public ObservableCollection<string> FolderPaths { get; } = [];
    public HashSet<string> DeletePaths { get; } = [];
    public string? CollectionName;
    public string[]? CollectionFolders;
    
    public SaveEditCollectionWindow()
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
        DataContext = this;
    }
    protected override void OnOpened(EventArgs e)
    {
        InputBox.Text = CollectionName;
        if (CollectionFolders is not null) { foreach (var key in CollectionFolders) { FolderPaths.Add(key); } }
        FolderBox.SelectedIndex = 0;
    }
     private void RemovePath_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string path && !string.IsNullOrEmpty(path))
        {
            FolderPaths.Remove(path);
            DeletePaths.Add(path);
        }
    }
    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Add Folders",
            AllowMultiple = true,
        });

        if (folders?.Count > 0)
        {
            foreach (var folder in folders)
            {
                if (folder.TryGetLocalPath() is { } path)
                {
                    DeletePaths.Remove(path);
                    FolderPaths.Add(path);
                }
            }
        }
        FolderBox.SelectedIndex = 0;
    }
    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        Close((EnteredText, FolderPaths.Where(Directory.Exists).ToArray(), DeletePaths.Where(Directory.Exists).ToArray()));
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}