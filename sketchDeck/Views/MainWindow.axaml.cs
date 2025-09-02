using Avalonia.Controls;
using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}