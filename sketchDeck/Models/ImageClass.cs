using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace sketchDeck.Models;

public class ImageItem
{
    public string PathImage { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Size { get; set; } = "";
    public DateTime DateModified { get; set; }
    public Bitmap? Thumbnail { get; set; }
    public int Order { get; set; } 

    public static ImageItem FromPath(string path)
    {
        var info = new FileInfo(path);
        return new ImageItem
        {
            PathImage = path,
            Name = info.Name,
            Type = string.IsNullOrEmpty(info.Extension) ? "File" : info.Extension.Trim('.').ToUpper(),
            Size = FormatSize(info.Length),
            DateModified = info.LastWriteTime,
            Thumbnail = ThumbnailCache.LoadOrCreateThumbnail(path),
        };
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

        public static Bitmap CreateThumbnail(Bitmap original)
        {
            var widthRatio = (double)512 / original.PixelSize.Width;
            var heightRatio = (double)512 / original.PixelSize.Height;

            var scale = Math.Min(widthRatio, heightRatio);

            var newWidth = (int)(original.PixelSize.Width * scale);
            var newHeight = (int)(original.PixelSize.Height * scale);

            return original.CreateScaledBitmap(
                new Avalonia.PixelSize(newWidth, newHeight),
                BitmapInterpolationMode.HighQuality);
        }
        public static string GetCachePath(string filePath)
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(filePath)));
            return Path.Combine(CacheDir, $"{hash}_{512}.png");
        }
        public static Bitmap LoadOrCreateThumbnail(string filePath)
        {
            var cachePath = ThumbnailCache.GetCachePath(filePath);

            if (File.Exists(cachePath))
            {
                return new Bitmap(cachePath);
            }

            using var bmp = new Bitmap(filePath);
            var thumb = CreateThumbnail(bmp);

            using (var fs = File.OpenWrite(cachePath))
            {
                thumb.Save(fs);
            }

            return thumb;
        }
    }
}