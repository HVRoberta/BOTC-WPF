using BOTC.Presentation.Api.Users.CreateUser;

namespace BOTC.Presentation.Api.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/users")
            .WithTags("Users");

        group.MapCreateUserEndpoint();

        return endpoints;
    }
}