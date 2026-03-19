﻿using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop;

public static class DesktopPresentationServiceRegistration
{
    private const string ApiBaseAddressConfigurationPath = "Api:BaseAddress";
    private static readonly Uri DefaultRoomsApiBaseAddress = new("http://localhost:5000");

    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services)
    {
        return AddDesktopPresentation(services, DefaultRoomsApiBaseAddress);
    }

    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var roomsApiBaseAddress = ResolveRoomsApiBaseAddress(configuration);
        return AddDesktopPresentation(services, roomsApiBaseAddress);
    }

    private static IServiceCollection AddDesktopPresentation(IServiceCollection services, Uri roomsApiBaseAddress)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<CreateRoomViewModel>();
        services.AddTransient<RoomLobbyViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();

        services.AddHttpClient<IRoomsApiClient, RoomsApiClient>(client =>
        {
            client.BaseAddress = roomsApiBaseAddress;
        });

        return services;
    }

    private static Uri ResolveRoomsApiBaseAddress(IConfiguration configuration)
    {
        var configuredBaseAddress = configuration[ApiBaseAddressConfigurationPath];
        if (string.IsNullOrWhiteSpace(configuredBaseAddress))
        {
            return DefaultRoomsApiBaseAddress;
        }

        if (!Uri.TryCreate(configuredBaseAddress, UriKind.Absolute, out var parsedBaseAddress))
        {
            throw new InvalidOperationException(
                $"Configuration value '{ApiBaseAddressConfigurationPath}' must be an absolute URI.");
        }

        return parsedBaseAddress;
    }
}
