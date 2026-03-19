using System.IO;
using BOTC.Presentation.Desktop.Navigation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop;

public partial class App
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = BuildConfiguration(e.Args);

        var services = new ServiceCollection();
        services.AddDesktopPresentation(configuration);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        navigationService.NavigateToCreateRoom();

        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static IConfiguration BuildConfiguration(string[] args)
    {
        var commandLineSwitchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--api-base-address"] = "Api:BaseAddress"
        };

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        return new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "BOTC_")
            .AddCommandLine(args, commandLineSwitchMappings)
            .Build();
    }
}
