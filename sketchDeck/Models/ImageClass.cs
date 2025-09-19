using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using PhotoSauce.MagicScaler;

namespace sketchDeck.Models;

public partial class ImageItem : ObservableObject
{
    public string PathImage { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public IBrush BgColor { get; set; } = Brushes.Gray;
    [ObservableProperty]
    public bool _isSelected;
    public DateTime DateModified { get; set; }
    public long Size { get; set; }
    public string SizeDisplay =>
    Size switch
    {
        >= 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024 * 1024):F2} GB",
        >= 1024 * 1024 => $"{Size / (1024.0 * 1024):F2} MB",
        >= 1024 => $"{Size / 1024.0:F2} KB",
        _ => $"{Size} B"
    };

    public static async Task<ImageItem> FromPathAsync(string path)
    {
        var info = new FileInfo(path);

        var item = new ImageItem
        {
            PathImage = path,
            Name = info.Name,
            Type = string.IsNullOrEmpty(info.Extension) ? "File" : info.Extension.Trim('.').ToUpper(),
            Size = info.Length,
            DateModified = info.LastWriteTime,
            Thumbnail = await ThumbnailCache.LoadOrCreateThumbnailAsync(path, $"|{info.Length}|{info.LastWriteTimeUtc:O}")
        };

        ThumbnailRefs.AddReference(item.Thumbnail);
        return item;
    }
    public static class ThumbnailCache
    {
        private static readonly string exeDir = AppContext.BaseDirectory;
        private static readonly string CacheDir = Path.Combine(exeDir, "bin", "thumbs");
        static ThumbnailCache() {Directory.CreateDirectory(CacheDir); }
        private static string GetCachePath(string filePathDate)
        {
            long hash = 1469598103934665603L;
            foreach (var c in filePathDate)
            {
                hash = (hash ^ c) * 1099511628211;
            }
            return Path.Combine(CacheDir, hash + ".png");
        }

        public static async Task<string> LoadOrCreateThumbnailAsync(string filePath, string uniqueData)
        {
            var cachePath = GetCachePath(filePath+uniqueData);

            if (File.Exists(cachePath))
                return cachePath;

            using var stream = File.OpenRead(filePath);
            using var outputStream = File.Create(cachePath);

            await Task.Run(() => MagicImageProcessor.ProcessImage(stream, outputStream, new ProcessImageSettings { Width = 512, }));

            return cachePath;
        }
    }
}
public static class ThumbnailRefs
{
    private static Dictionary<string, int> ReferencesThumbnails { get; set; } = [];
    public static void AddReference(string thumbnailPath)
    {
        if (ReferencesThumbnails.TryGetValue(thumbnailPath, out int value))
            ReferencesThumbnails[thumbnailPath] = ++value;
        else
            ReferencesThumbnails[thumbnailPath] = 1;
    }
    public static void ReleaseReference(string thumbnailPath)
    {
        if (ReferencesThumbnails.TryGetValue(thumbnailPath, out var count))
        {
            count--;
            if (count <= 0)
            {
                ReferencesThumbnails.Remove(thumbnailPath);
                File.Delete(thumbnailPath);
            }
            else
            {
                ReferencesThumbnails[thumbnailPath] = count;
            }
        }
    }
}