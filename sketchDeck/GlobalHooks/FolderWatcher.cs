using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace sketchDeck.GlobalHooks;
public class FolderWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Dictionary<string, DateTime> _debounce = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _debounceMs;

    public event Func<string, Task>? FileCreated;
    public event Func<string, Task>? FileChanged;
    public event Action<string>? FileDeleted;
    public event Action<string, string>? FileRenamed;

    public FolderWatcher(string folder, int debounceMs = 500)
    {
        _debounceMs = debounceMs;

        _watcher = new FileSystemWatcher(folder, "*.*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.DirectoryName |
                           NotifyFilters.CreationTime |
                           NotifyFilters.LastWrite
        };
        _watcher.Created += async (s, e) => await HandleCreated(e.FullPath);
        _watcher.Changed += async (s, e) => await HandleChanged(e.FullPath);
        _watcher.Deleted += (s, e) => HandleDeleted(e.FullPath);
        _watcher.Renamed += (s, e) => HandleRenamed(e.OldFullPath, e.FullPath);
    }
    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;
    public static async Task<bool> WaitForFileReadyAsync(string path, int stableSeconds = 2, int checkIntervalMs = 1000, int maxWaitMinutes = 5)
    {
        var maxWaitMs = maxWaitMinutes * 60 * 1000;
        var waitedMs = 0;

        long lastSize = -1;
        int stableTime = 0;

        while (waitedMs < maxWaitMs)
        {
            if (!File.Exists(path))
            {
                await Task.Delay(checkIntervalMs);
                waitedMs += checkIntervalMs;
                continue;
            }

            long currentSize = 0;
            bool readable = false;

            try
            {
                currentSize = new FileInfo(path).Length;
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                readable = true;
            }
            catch { }

            if (readable && currentSize == lastSize && currentSize > 0)
            {
                stableTime += checkIntervalMs;
                if (stableTime >= stableSeconds * 1000) return true;
            }
            else
            {
                stableTime = 0;
                lastSize = currentSize;
            }
            await Task.Delay(checkIntervalMs);
            waitedMs += checkIntervalMs;
            }
        return false;
    }
    private bool ShouldDebounce(string path)
    {
        var now = DateTime.UtcNow;
        if (_debounce.TryGetValue(path, out var lastTime) && (now - lastTime).TotalMilliseconds < _debounceMs) { return true; }
        _debounce[path] = now;
        return false;
    }
    private async Task HandleCreated(string path)
    {
        if (!FileFilters.AllowedExtensions.Contains(Path.GetExtension(path)) || ShouldDebounce(path)) return;
        if (FileCreated != null) await FileCreated(path);
    }
    private async Task HandleChanged(string path)
    {
        if (!FileFilters.AllowedExtensions.Contains(Path.GetExtension(path)) || ShouldDebounce(path)) return;
        if (FileChanged != null) await FileChanged(path);
    }
    private void HandleDeleted(string path)
    {
        if (!FileFilters.AllowedExtensions.Contains(Path.GetExtension(path))) return;
        FileDeleted?.Invoke(path);
    }
    private void HandleRenamed(string oldPath, string newPath)
    {
        if (!FileFilters.AllowedExtensions.Contains(Path.GetExtension(newPath))) return;
        FileRenamed?.Invoke(oldPath, newPath);
    }
    public void Dispose()
    {
        _watcher.Dispose();
        GC.SuppressFinalize(this);
    }
}