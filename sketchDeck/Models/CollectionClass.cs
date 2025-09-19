using System;
using System.ComponentModel;
using System.Reactive.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;
using DynamicData.Aggregation;


namespace sketchDeck.Models;

public partial class CollectionItem : ObservableObject
{
    public required SourceList<ImageItem> CollectionImages { get; set; }
    [ObservableProperty] private int _leng;
    private IDisposable? _countDisposable;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private string sortBy = "Name";
    [ObservableProperty] private ListSortDirection sortDirection = ListSortDirection.Ascending;
    public static CollectionItem FromImages(SourceList<ImageItem> images, string name)
    {
        var item = new CollectionItem { Name = name, CollectionImages = new SourceList<ImageItem> { } };
        item.CollectionImages.AddRange(images.Items);
        item._countDisposable = item.CollectionImages.Connect().Count().Subscribe(count => item.Leng = count);
        return item;
    }
    public void Dispose()
    {
        _countDisposable?.Dispose();

        foreach (var img in CollectionImages.Items)
        {
            ThumbnailRefs.ReleaseReference(img.Thumbnail);
        }

        CollectionImages.Clear();
    }
}