using Avalonia.Controls;
using Avalonia.Interactivity;

namespace sketchDeck.CustomAxaml;

public partial class SaveCollectionWindow : Window
{
    public SaveCollectionWindow()
    {
        InitializeComponent();
        this.Icon = new WindowIcon("Assets/avalonia-logo.ico");
    }

    public string EnteredText => InputBox.Text ?? string.Empty;

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        Close(EnteredText);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}