using System;
using System.IO;

namespace sketchDeck;

public static class Log
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "bin", "app.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(LogPath, line);
        }
        catch { }
    }

    public static void Write(Exception ex)
    {
        Write($"UNHANDLED ERROR: {ex}");
    }
}