using Avalonia.Controls;
using Avalonia.Interactivity;

using sketchDeck.ViewModels;

namespace sketchDeck.CustomAxaml;


public partial class YesNoCancelDialog : Window
{
    public string MessagePrefix { get; }
    public string ItemName { get; }
    public string MessageSuffix { get; }
    public YesNoCancelDialog(string itemName, bool showNoButton)
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
        if (showNoButton)
        {
            MessagePrefix = "Do you wanna delete collection: ";
            ItemName = itemName;
            MessageSuffix = " and all images?";
        }
        else
        {
            MessagePrefix = "Do you wanna delete collection?";
            ItemName = "";
            MessageSuffix = "";
            NoButton.IsVisible = false;
        }
        DataContext = this;
    }
    private void Yes_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.Yes);
    private void No_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.No);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.Cancel);
}