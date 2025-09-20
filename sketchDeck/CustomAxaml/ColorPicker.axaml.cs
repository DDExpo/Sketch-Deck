
#define WINDOWS

using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

using sketchDeck.GlobalHooks;
using sketchDeck.ViewModels;

namespace sketchDeck.CustomAxaml;

public partial class ColorPickerWindow : Window
{
    public Color SelectedColor { get; private set; }
    public bool IsCancelled { get; private set; }
    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();
        this.Icon = new WindowIcon("Assets/avalonia-logo.ico");
        Picker.ColorChanged += (_, __) => SelectedColor = initialColor;
    }
}
