using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.Threading.Tasks;

namespace sketchDeck.Models;

public class ImageItem
{
    public string PathImage { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Size { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public DateTime DateModified { get; set; }

    public static async Task<ImageItem> FromPathAsync(string path)
    {
        var info = new FileInfo(path);

        var item = new ImageItem
        {
            PathImage = path,
            Name = info.Name,
            Type = string.IsNullOrEmpty(info.Extension) ? "File" : info.Extension.Trim('.').ToUpper(),
            Size = FormatSize(info.Length),
            DateModified = info.LastWriteTime,
            Thumbnail = await ThumbnailCache.LoadOrCreateThumbnailAsync(path)
        };

        return item;
    }
    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    public static class ThumbnailCache
    {
        private static readonly string exeDir = AppContext.BaseDirectory;
        private static readonly string CacheDir = Path.Combine(exeDir, "bin", "thumbs");
        static ThumbnailCache()
        {
            Directory.CreateDirectory(CacheDir);
        }
        private static string GetCachePath(string filePath)
        {
            var hash = filePath.GetHashCode().ToString("X8");
            return Path.Combine(CacheDir, hash + ".png");
        }
        
        public static async Task<string> LoadOrCreateThumbnailAsync(string filePath)
        {
            var cachePath = GetCachePath(filePath);

            if (File.Exists(cachePath))
                return cachePath;

            using var image = await Image.LoadAsync<Rgba32>(new DecoderOptions {TargetSize = new Size(512, 0), SkipMetadata = true}, filePath);

            await image.SaveAsync(cachePath, new PngEncoder {CompressionLevel = PngCompressionLevel.DefaultCompression, SkipMetadata = true});

            return cachePath;
        }
    }
}