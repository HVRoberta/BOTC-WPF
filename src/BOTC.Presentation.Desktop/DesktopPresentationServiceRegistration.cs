using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop;

public static class DesktopPresentationServiceRegistration
{
    private static readonly Uri RoomsApiBaseAddress = new("http://localhost:5000");

    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<CreateRoomViewModel>();
        services.AddTransient<RoomLobbyViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();

        services.AddHttpClient<IRoomsApiClient, RoomsApiClient>(client =>
        {
            client.BaseAddress = RoomsApiBaseAddress;
        });

        return services;
    }
}

