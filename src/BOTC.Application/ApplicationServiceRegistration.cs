﻿using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<GetRoomLobbyHandler>();
        services.AddScoped<JoinRoomHandler>();

        return services;
    }
}
