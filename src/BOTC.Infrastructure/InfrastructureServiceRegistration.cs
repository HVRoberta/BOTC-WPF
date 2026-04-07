using BOTC.Application.Abstractions.Events;
using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Domain.Rooms.Events;
using BOTC.Infrastructure.Eventing;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BotcDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomJoinRepository, RoomRepository>();
        services.AddScoped<IRoomLeaveRepository, RoomRepository>();
        services.AddScoped<IRoomSetPlayerReadyRepository, RoomRepository>();
        services.AddScoped<IRoomStartGameRepository, RoomRepository>();
        services.AddScoped<IRoomLobbyQueryService, RoomLobbyQueryService>();
        services.AddSingleton<IRoomCodeGenerator, RandomRoomCodeGenerator>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IDomainEventHandler<RoomCreatedDomainEvent>, RoomCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerJoinedRoomDomainEvent>, PlayerJoinedRoomEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerLeftRoomDomainEvent>, PlayerLeftRoomEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerReadyStateChangedDomainEvent>, PlayerReadyStateChangedEventHandler>();
        services.AddScoped<IDomainEventHandler<GameStartedDomainEvent>, GameStartedEventHandler>();

        return services;
    }
}