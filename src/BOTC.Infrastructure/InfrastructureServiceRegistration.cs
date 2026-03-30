using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<BotcDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomJoinRepository, RoomRepository>();
        services.AddScoped<IRoomLeaveRepository, RoomRepository>();
        services.AddScoped<IRoomLobbyQueryService, RoomLobbyQueryService>();
        services.AddSingleton<IRoomCodeGenerator, RandomRoomCodeGenerator>();

        return services;
    }
}
