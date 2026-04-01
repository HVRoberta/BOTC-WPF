using BOTC.Presentation.Desktop.Configuration;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Rooms.JoinRoom;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;
using BOTC.Presentation.Desktop.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop;

public static class DesktopPresentationServiceRegistration
{
    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services)
    {
        throw new InvalidOperationException("Desktop configuration is required. Use AddDesktopPresentation(IServiceCollection, IConfiguration).");
    }

    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ApiOptions>(configuration.GetSection("Api"));
        services.Configure<SignalROptions>(configuration.GetSection("SignalR"));
        services.AddSingleton<IEndpointConfigurationService, EndpointConfigurationService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<CreateRoomViewModel>();
        services.AddTransient<JoinRoomViewModel>();
        services.AddTransient<RoomLobbyViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IClientSessionService, ClientSessionService>();
        services.AddSingleton<IRoomLobbyRealtimeClient>(provider =>
        {
            var endpointConfiguration = provider.GetRequiredService<IEndpointConfigurationService>();
            return new RoomLobbyRealtimeClient(endpointConfiguration.GetSignalRHubUri());
        });

        services.AddHttpClient<IRoomsApiClient, RoomsApiClient>((provider, client) =>
        {
            var endpointConfiguration = provider.GetRequiredService<IEndpointConfigurationService>();
            client.BaseAddress = endpointConfiguration.GetApiBaseUri();
        });

        return services;
    }
}
