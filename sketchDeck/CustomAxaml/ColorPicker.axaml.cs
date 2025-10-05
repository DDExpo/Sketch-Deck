using Avalonia.Controls;
using Avalonia.Media;

namespace sketchDeck.CustomAxaml;

public partial class ColorPickerWindow : Window
{
    public Color SelectedColor { get; private set; }
    public bool IsCancelled { get; private set; }
    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
        Picker.ColorChanged += (_, __) => SelectedColor = initialColor;
    }
}
