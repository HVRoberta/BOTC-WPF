using CommunityToolkit.Mvvm.ComponentModel;

namespace BOTC.Presentation.Desktop;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;
}
