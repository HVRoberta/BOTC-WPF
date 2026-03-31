using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<GetRoomLobbyHandler>();
        services.AddScoped<JoinRoomHandler>();
        services.AddScoped<LeaveRoomHandler>();
        services.AddScoped<SetPlayerReadyHandler>();
        services.AddScoped<StartGameHandler>();

        return services;
    }
}
