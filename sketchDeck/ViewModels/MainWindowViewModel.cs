using System.IO;
using System.Linq;
using sketchDeck.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;

namespace sketchDeck.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public string[] Views { get; } = ["Gigantic", "Big", "Medium", "Small", "Details"];
    [ObservableProperty]
    public string _selectedView = "Details";

    private readonly ObservableCollection<ImageItem> _allImages = [];


    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ImageItem> _images;

    public MainWindowViewModel()
    {
        Images = new ObservableCollection<ImageItem>(_allImages);
    
        LoadFolder(@"D:\Workspace\C#\Projects\SketchDeck\test");


        this.PropertyChanged += OnSearchTermChanged;
    }

    private void OnSearchTermChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchTerm))
        {
            IEnumerable<ImageItem> filtered = string.IsNullOrWhiteSpace(SearchTerm)
                ? _allImages
                : _allImages.Where(item => 
                    item.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            Images.Clear();
            foreach (var item in filtered)
            {
                Images.Add(item);
            }
        }
    }

    private void LoadFolder(string folderPath)
    {
        Images.Clear();

        if (!Directory.Exists(folderPath)) return;

        foreach (var file in Directory.GetFiles(folderPath))
        {
            Images.Add(ImageItem.FromPath(file));
        }
    }
}
