using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Avalonia;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using sketchDeck.ViewModels;
using sketchDeck.Views;

namespace sketchDeck;


public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
public static class AppResources
{
    public const string AppIconPath = "Assets/avalonia-logo.ico";
}
public class AppSettings
{
    public float Width                = 741;
    public float Height               = 440;
    public bool IsShuffled            = false;
    public string TimeImage           = "0";
    public string SelectedView        = "Details";
    public int? SelectedCollection    = 0;
    public float LeftPanelWidth       = 179;
    public float LeftPanelChildHeight = 150;
}

public static class SettingsService
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private static readonly string pathSettings = Path.Combine(AppContext.BaseDirectory, "bin", "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(pathSettings)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(pathSettings)) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }
    public static void Save(AppSettings settings)
    {
        File.WriteAllText(pathSettings, JsonSerializer.Serialize(settings, jsonOptions));
    }
}

public static class FileFilters
{
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
    };
}