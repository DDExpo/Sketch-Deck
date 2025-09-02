using CommunityToolkit.Mvvm.ComponentModel;


namespace sketchDeck.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    
    public MiddlePanelViewModel MiddlePanel { get; } = new();
    public MainWindowViewModel()
    {
    }
}
