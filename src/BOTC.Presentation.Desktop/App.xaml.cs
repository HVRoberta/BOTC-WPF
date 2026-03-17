using BOTC.Presentation.Desktop.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop;

public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        services.AddDesktopPresentation();
        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        navigationService.NavigateToCreateRoom();

        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
