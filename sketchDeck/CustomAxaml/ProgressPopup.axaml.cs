using Avalonia.Controls;

namespace sketchDeck.CustomAxaml;

public partial class ProgressPopup : Window
{
    public ProgressPopup()
    {
        InitializeComponent();
        this.Icon = new WindowIcon("Assets/avalonia-logo.ico");
    }
}