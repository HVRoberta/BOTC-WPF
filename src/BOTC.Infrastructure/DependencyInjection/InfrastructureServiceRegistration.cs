using BOTC.Application.Abstractions.Events;
using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Domain.Rooms.Events;
using BOTC.Domain.Users.Events;
using BOTC.Infrastructure.Eventing;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Repositories;
using BOTC.Infrastructure.Rooms;
using BOTC.Infrastructure.Rooms.Queries;
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

        services.AddRepositories();
        services.AddQueries();
        services.AddDomainEventing();
        services.AddInfrastructureServices();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IRoomLobbyQueryService, RoomLobbyQueryService>();
        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IRoomCodeGenerator, RandomRoomCodeGenerator>();
        return services;
    }

    private static IServiceCollection AddDomainEventing(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IDomainEventHandler<RoomCreatedDomainEvent>, RoomCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerJoinedRoomDomainEvent>, PlayerJoinedRoomEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerLeftRoomDomainEvent>, PlayerLeftRoomEventHandler>();
        services.AddScoped<IDomainEventHandler<PlayerReadyStateChangedDomainEvent>, PlayerReadyStateChangedEventHandler>();
        services.AddScoped<IDomainEventHandler<GameStartedDomainEvent>, GameStartedEventHandler>();
        
        services.AddScoped<IDomainEventHandler<UserCreatedDomainEvent>, UserCreatedEventHandler>();

        return services;
    }
}