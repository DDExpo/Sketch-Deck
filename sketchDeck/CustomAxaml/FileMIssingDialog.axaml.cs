using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace sketchDeck.CustomAxaml;

public enum FileMissingResult
{
    None,
    SetNewPath,
}

public partial class FileMissingDialog : Window
{
    public string FilePath { get; }
    public FileMissingResult Result { get; private set; } = FileMissingResult.None;
    public string? NewPath { get; private set; }

    public FileMissingDialog(string filePath)
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
        FilePath = filePath;
        DataContext = this;
    }

    private async void SetNewPath_Click(object? sender, RoutedEventArgs e)
    {
        if (StorageProvider is null)
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select new file",
            AllowMultiple = false
        });

        if (files != null && files.Count > 0)
        {
            NewPath = files[0].Path.LocalPath;
            Result = FileMissingResult.SetNewPath;
            Close(Result);
        }
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = FileMissingResult.None;
        Close(Result);
    }
}