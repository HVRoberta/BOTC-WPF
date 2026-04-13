using CommunityToolkit.Mvvm.ComponentModel;

namespace BOTC.Presentation.Desktop.App;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;
}
