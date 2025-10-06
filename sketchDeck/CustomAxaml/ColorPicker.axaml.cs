using Avalonia.Controls;

namespace sketchDeck.CustomAxaml;

public partial class ColorPickerWindow : Window
{
    public ColorPickerWindow()
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
    }
}
