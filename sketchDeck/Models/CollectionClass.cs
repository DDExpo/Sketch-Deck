using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;
using DynamicData.Aggregation;

using sketchDeck.GlobalHooks;

namespace sketchDeck.Models;

public partial class CollectionItem : ObservableObject
{
    public required SourceList<ImageItem> CollectionImages;
    private IDisposable? _countDisposable;
    [ObservableProperty] private int _leng;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private string _name    = string.Empty;
    [ObservableProperty] private string sortBy   = "Name";
    [ObservableProperty] private ListSortDirection sortDirection  = ListSortDirection.Ascending;
    public Dictionary<string, ImageItem> UniqueFoldersImagesPaths = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, FolderWatcher> Watchers    = new(StringComparer.OrdinalIgnoreCase);
    public static CollectionItem FromImages(SourceList<ImageItem> images, string name)
    {
        var item = new CollectionItem { Name = name, CollectionImages = new SourceList<ImageItem> { } };
        item.CollectionImages.AddRange(images.Items);
        item._countDisposable = item.CollectionImages.Connect().Count().Subscribe(count => item.Leng = count);
        return item;
    }
    public void AddWatcher(string folder)
    {
        if (Watchers.ContainsKey(folder)) return;

        var fw = new FolderWatcher(folder);

        fw.FileCreated += async path =>
        {
            if (!UniqueFoldersImagesPaths.ContainsKey(path))
            {
                var image = await ImageItem.FromPathAsync(path, null, null, null);
                UniqueFoldersImagesPaths[path] = image;
                Dispatcher.UIThread.Post(() => { CollectionImages.Add(image); });
            }
        };
        fw.FileDeleted += path =>
        {
            if (UniqueFoldersImagesPaths.TryGetValue(path, out var value))
            {
                ThumbnailRefs.ReleaseReference(value.ThumbnailPath);
                UniqueFoldersImagesPaths.Remove(path);
                Dispatcher.UIThread.Post(() =>{ CollectionImages.Remove(value);});
            }
        };
        fw.FileChanged += async path =>
        {
            if (UniqueFoldersImagesPaths.TryGetValue(path, out var oldImage))
            {
                var newImage = await ImageItem.FromPathAsync(path, null, null, null);
                ThumbnailRefs.ReleaseReference(oldImage.ThumbnailPath);
                UniqueFoldersImagesPaths[path] = newImage;
                Dispatcher.UIThread.Post(() =>
                {
                    CollectionImages.Remove(oldImage);
                    CollectionImages.Add(newImage);
                });
            }
        };
        fw.FileRenamed += (oldPath, newPath) =>
        {
            if (UniqueFoldersImagesPaths.TryGetValue(oldPath, out var img))
            {
                img.PathImage = newPath;
                img.Name = Path.GetFileName(newPath);
                UniqueFoldersImagesPaths[newPath] = img;
                Dispatcher.UIThread.Post(() => {UniqueFoldersImagesPaths.Remove(oldPath);});
            }
        };
        fw.Start();
        Watchers[folder] = fw;
    }
    public void RemoveWatcher(string folder)
    {
        if (Watchers.TryGetValue(folder, out var fw))
        {
            fw.Dispose();
            Watchers.Remove(folder);
        }
    }
    public void Dispose(bool deleteRefs)
    {
        _countDisposable?.Dispose();
        foreach (var kv in Watchers.Values) { kv.Dispose();}
        Watchers.Clear();
        if (deleteRefs)
        {
            foreach (var img in CollectionImages.Items) { ThumbnailRefs.ReleaseReference(img.ThumbnailPath); }
        }
        CollectionImages.Clear();
        UniqueFoldersImagesPaths.Clear();
    }
}
public class SerializableCollection
{
    private static readonly string saveFile = Path.Combine(AppContext.BaseDirectory, "bin", "save");
    private static readonly string backupFile = Path.Combine(AppContext.BaseDirectory, "bin", "backup");
    private static readonly Lock _saveLock = new();

    public string Name { get; set; } = "";
    public SerializableImageItem[] CollectionImages { get; set; } = [];
    public string[] FoldersPaths { get; set; } = [];
    [JsonIgnore]
    public HashSet<string> UniquePaths = [];

    public class SerializableImageItem
    {
        public string Name { get; set; } = "";
        public string PathImage { get; set; } = "";
        public string PathThumbnail { get; set; } = "";
        public string BgColorHex { get; set; } = "";
    }
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static IBrush ParseOrDefault(string hex)
    {
        try { return Brush.Parse(hex); }
        catch { return Brushes.Gray; }
    }
    public static SerializableCollection ToSerializable(CollectionItem collection)
    {
        return new SerializableCollection
        {
            Name = collection.Name,
            CollectionImages = [.. collection.CollectionImages.Items.Select(img => new SerializableImageItem
            {
                Name           = img.Name,
                PathImage      = img.PathImage,
                PathThumbnail  = img.ThumbnailPath,
                BgColorHex     = img.BgColor is SolidColorBrush scb ? scb.Color.ToString() : "#808080"
            })],
            FoldersPaths = [.. collection.Watchers.Keys]
        };
    }
    public static SerializableCollection[] LoadFromFile()
    {
        if (!File.Exists(saveFile)) return [];
        try
        {
            var json = File.ReadAllText(saveFile);
            return JsonSerializer.Deserialize<SerializableCollection[]>(json, _options) ?? [];
        }
        catch
        {
            if (File.Exists(backupFile))
            {
                var json = File.ReadAllText(backupFile);
                return JsonSerializer.Deserialize<SerializableCollection[]>(json, _options) ?? [];
            }
            return [];
        }
    }

    public static void SaveToFile(CollectionItem[] collections)
    {
        lock (_saveLock)
        {
            var json = JsonSerializer.Serialize(collections.Select(ToSerializable), _options);
            var tempFile = saveFile + ".tmp";
            File.WriteAllText(tempFile, json);

            if (File.Exists(saveFile)) { File.Copy(saveFile, backupFile, overwrite: true); }
            File.Move(tempFile, saveFile, overwrite: true);
        }
    }
}