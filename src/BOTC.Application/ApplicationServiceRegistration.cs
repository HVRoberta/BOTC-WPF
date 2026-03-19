using BOTC.Application.Features.Rooms.CreateRoom;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRoomHandler>();

        return services;
    }
}

