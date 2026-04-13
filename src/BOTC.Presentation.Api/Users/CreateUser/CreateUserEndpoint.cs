using BOTC.Application.Features.Users.CreateUser;
using BOTC.Contracts.Users;
using BOTC.Presentation.Api.Users.Common;

namespace BOTC.Presentation.Api.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static RouteGroupBuilder MapCreateUserEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/", HandleAsync)
            .WithName("CreateUser")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateUserRequest request,
        CreateUserHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return UserProblemResults.BadRequest(
                "Invalid create user request.",
                "Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return UserProblemResults.BadRequest(
                "Invalid create user request.",
                "Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NickName))
        {
            return UserProblemResults.BadRequest(
                "Invalid create user request.",
                "NickName is required.");
        }

        try
        {
            var command = CreateUserMappings.ToCommand(request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = CreateUserMappings.ToResponse(result);

            return Results.Created($"/api/users/{response.UserId}", response);
        }
        catch (UserAlreadyExistsException exception)
        {
            return UserProblemResults.Conflict(
                "Unable to create user.",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return UserProblemResults.BadRequest(
                "Invalid create user request.",
                exception.Message);
        }
    }
}