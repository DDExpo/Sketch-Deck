using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Media;
using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using PhotoSauce.MagicScaler;

using sketchDeck.GlobalHooks;

namespace sketchDeck.Models;

public partial class ImageItem : ObservableObject
{
    public string PathImage { get; set; }     = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    public string Type { get; set; }          = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public Task<Bitmap?> Thumbnail => GetThumbnailAsync();
    public IBrush BgColor { get; set; }           = Brushes.Gray;
    [ObservableProperty] private bool _isEditing  = false;
    [ObservableProperty] private bool _isSelected = false;
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

    public static async Task<ImageItem> FromPathAsync(string path, string? name, string? thumb, IBrush? bgColor)
    {
        var info = new FileInfo(path);
        if (!File.Exists(thumb)) { thumb = null; }

        var item = new ImageItem
        {
            PathImage     = path,
            Name          = name ?? info.Name,
            Type          = string.IsNullOrEmpty(info.Extension) ? "File" : info.Extension.Trim('.').ToUpper(),
            Size          = info.Length,
            DateModified  = info.LastWriteTime,
            BgColor       = bgColor ?? Brushes.Gray,
            ThumbnailPath = thumb ?? await ThumbnailCache.LoadOrCreateThumbnailAsync(path, $"{info.Name}|{info.Extension.Trim('.')}|{info.LastWriteTimeUtc:O}")
        };
        ThumbnailRefs.AddReference(item.ThumbnailPath);
        return item;
    }
    public Task<Bitmap?> GetThumbnailAsync()
    {
        int thumbSize = ThumbnailHelper.CurrentThumbSize;
        if (ThumbnailPath is null || !File.Exists(ThumbnailPath)) { return Task.FromResult<Bitmap?>(null); }
        return Task.Run<Bitmap?>(() =>
        {
            using var stream = File.OpenRead(ThumbnailPath);
            return thumbSize > 0
                 ? Bitmap.DecodeToWidth(stream, thumbSize)
                 : new Bitmap(stream);
        });
    }
    public static class ThumbnailCache
    {
        private static readonly string CacheDir = Path.Combine(AppContext.BaseDirectory, "bin", "thumbs");
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
        
        static ThumbnailCache() => Directory.CreateDirectory(CacheDir);
        private static string GetCachePath(string NameTypeDate)
        {
            long hash = 1469598103934665603L;
            foreach (var c in NameTypeDate)
                hash = (hash ^ c) * 1099511628211;
            return Path.Combine(CacheDir, hash + ".png");
        }

        public static async Task<string> LoadOrCreateThumbnailAsync(string filePath, string uniqueData)
        {
            var cachePath = GetCachePath(uniqueData);

            if (File.Exists(cachePath))
                return cachePath;

            var fileLock = Locks.GetOrAdd(cachePath, _ => new SemaphoreSlim(1, 1));

            await fileLock.WaitAsync();
            try
            {
                if (File.Exists(cachePath)) return cachePath;

                using var stream       = File.OpenRead(filePath);
                using var outputStream = File.Create(cachePath);

                await Task.Run(() =>
                    MagicImageProcessor.ProcessImage(stream, outputStream,
                        new ProcessImageSettings { Width = 512 })
                );
            }
            finally
            {
                fileLock.Release();
                Locks.TryRemove(cachePath, out _);
            }
            return cachePath;
        }
    }
}
public static class ThumbnailRefs
{
    private static readonly ConcurrentDictionary<string, int> ReferencesThumbnails = new();

    public static void AddReference(string thumbnailPath)
    {
        ReferencesThumbnails.AddOrUpdate(
            thumbnailPath,
            1,
            (key, oldValue) => oldValue + 1 
        );
    }

    public static void ReleaseReference(string thumbnailPath)
    {
        bool removed = false;

        ReferencesThumbnails.AddOrUpdate(thumbnailPath, 0, (key, oldValue) =>
            {
                int newValue = oldValue - 1;
                if (newValue <= 0)
                {
                    removed = true;
                    return 0;
                }
                return newValue;
            }
        );

        if (removed)
        {
            ReferencesThumbnails.TryRemove(thumbnailPath, out _);

            if (File.Exists(thumbnailPath))
            {
                try { File.Delete(thumbnailPath); }
                catch { }
            }
        }
    }
    public static int GetReferenceCount(string thumbnailPath) =>
        ReferencesThumbnails.TryGetValue(thumbnailPath, out var count) ? count : 0;
}