using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using PhotoSauce.MagicScaler;

using SkiaSharp;

namespace sketchDeck.ImageOptimiztion;

public class TiledImage : Control
{
    public int TileSize;
    public string? PathImage;
    private Bitmap? _tiledBitmap;
    public Bitmap? LowResBitmap;
    public Bitmap? MediumResBitmap;
    public Bitmap? HiResBitmap;
    private bool _isBiggerThenScreen = false;
    private int _imageWidth;
    private int _imageHeight;
    private int _imageOriginalWidth;
    private int _imageOriginalHeight;
    private int _imageMediumOriginalWidth;
    private int _imageMediumOriginalHeight;
    public float ZoomLevel;
    public float PanOffsetX;
    public float PanOffsetY;


    public TiledImage() { }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (LowResBitmap is null) return;

        // float curTileSize = TileSize;
        // int curImageWidth = _imageWidth;
        // int curImageheight = _imageHeight;

        // if (ZoomLevel <= 1.3)
        // {
        //     _tiledBitmap = LowResBitmap;
        // }
        // else if (ZoomLevel > 1.3 && _isBiggerThenScreen)
        // {
        //     curTileSize = TileSize * _imageToScreenRatio;
        //     curImageWidth = _imageOriginalWidth;
        //     curImageheight = _imageOriginalHeight;
        //     _tiledBitmap = HiResBitmap;
        // }

        // float scale = (float)(Math.Min(Bounds.Width / curImageWidth, Bounds.Height / curImageheight) * ZoomLevel);
        // float offsetX = (float)((Bounds.Width - curImageWidth * scale) / 2.0);
        // float offsetY = (float)((Bounds.Height - curImageheight * scale) / 2.0);
        // float curScaledTile = curTileSize * scale;

        // Rect viewportScreen = new(-PanOffsetX / ZoomLevel, -PanOffsetY / ZoomLevel, Bounds.Width / ZoomLevel, Bounds.Height / ZoomLevel);
        // int minTileX = Math.Max(0, (int)Math.Floor((viewportScreen.Left - offsetX) / curScaledTile));
        // int minTileY = Math.Max(0, (int)Math.Floor((viewportScreen.Top - offsetY) / curScaledTile));
        // int maxTileX = (int)Math.Min(curImageWidth / curTileSize + 1, (int)Math.Ceiling((viewportScreen.Right - offsetX) / curScaledTile));
        // int maxTileY = (int)Math.Min(curImageheight / curTileSize + 1, (int)Math.Ceiling((viewportScreen.Bottom - offsetY) / curScaledTile));

        // for (int ty = minTileY; ty < maxTileY; ty++)
        // {
        //     for (int tx = minTileX; tx < maxTileX; tx++)
        //     {
        //         Rect srcRect = new(
        //             tx * curTileSize,
        //             ty * curTileSize,
        //             curTileSize,
        //             curTileSize
        //         );
        //         Rect destRect = new(
        //             tx * curScaledTile + offsetX,
        //             ty * curScaledTile + offsetY,
        //             curScaledTile,
        //             curScaledTile
        //         );
        //         context.DrawImage(_tiledBitmap!, srcRect, destRect);
        //     }
        // }

        int curImageWidth = _imageWidth;
        int curImageHeight = _imageHeight;
        Console.WriteLine(ZoomLevel);
        if (ZoomLevel <= 1.4)
        {
            _tiledBitmap   = LowResBitmap;
            curImageWidth  = _imageWidth;
            curImageHeight = _imageHeight;
        }
        else if (ZoomLevel > 1.4 && ZoomLevel <= 2.1 && _isBiggerThenScreen)
        {
            _tiledBitmap   = MediumResBitmap;
            curImageWidth  = _imageMediumOriginalWidth;
            curImageHeight = _imageMediumOriginalHeight;
        }
        else if (ZoomLevel > 2.1 && _isBiggerThenScreen)
        {
            _tiledBitmap   = HiResBitmap;
            curImageWidth  = _imageOriginalWidth;
            curImageHeight = _imageOriginalHeight;
        }
        float scale = (float)(Math.Min(Bounds.Width / curImageWidth, Bounds.Height / curImageHeight) * ZoomLevel);

        float offsetX = (float)((Bounds.Width  - curImageWidth  * scale) / 2.0);
        float offsetY = (float)((Bounds.Height - curImageHeight * scale) / 2.0);

        var srcRect  = new Rect(0, 0, curImageWidth, curImageHeight);
        var destRect = new Rect(offsetX, offsetY, curImageWidth * scale,curImageHeight * scale);

        context.DrawImage(_tiledBitmap!, srcRect, destRect);
    }
    public async Task LoadLowResAsync(string path, int screenWidth, int screenHeight)
    {
        LowResBitmap = await Task.Run(() =>
        {
            using var fs    = File.OpenRead(path);
            using var codec = SKCodec.Create(fs);
            fs.Position     = 0;

            _imageWidth  = _imageOriginalWidth  = codec.Info.Width;
            _imageHeight = _imageOriginalHeight = codec.Info.Height;

            ProcessImageSettings? settings = null;
            if (codec.Info.Width >= screenWidth * 1.5 || codec.Info.Height >= screenHeight * 1.5)
            {
                _isBiggerThenScreen = true;
                settings = new ProcessImageSettings
                {
                    Width      = (int)(screenWidth  * 1.3),
                    Height     = (int)(screenHeight * 1.3),
                    ResizeMode = CropScaleMode.Pad
                };
                _imageWidth  = settings.Width;
                _imageHeight = settings.Height;
            }

            using var ms = new MemoryStream();

            if (settings is null) { fs.CopyTo(ms); }
            else { MagicImageProcessor.ProcessImage(fs, ms, settings); }

            ms.Position = 0;
            return new Bitmap(ms);
        });
        MediumResBitmap = await Task.Run(() =>
        {
            using var fs    = File.OpenRead(path);
            using var codec = SKCodec.Create(fs);
            fs.Position     = 0;
            _imageMediumOriginalWidth  = _imageOriginalWidth  = codec.Info.Width;
            _imageMediumOriginalHeight = _imageOriginalHeight = codec.Info.Height;

            ProcessImageSettings? settings = null;
            if (codec.Info.Width >= screenWidth * 2.5 || codec.Info.Height >= screenHeight * 2.5)
            {
                settings = new ProcessImageSettings
                {
                    Width      = screenWidth  * 2,
                    Height     = screenHeight * 2,
                    ResizeMode = CropScaleMode.Pad
                };
                _imageMediumOriginalWidth  = settings.Width;
                _imageMediumOriginalHeight = settings.Height;
            }

            using var ms = new MemoryStream();

            if (settings is null) { fs.CopyTo(ms); }
            else { MagicImageProcessor.ProcessImage(fs, ms, settings); }

            ms.Position = 0;
            return new Bitmap(ms);
        });
        HiResBitmap = new Bitmap(path);
        InvalidateVisual();
    }
}