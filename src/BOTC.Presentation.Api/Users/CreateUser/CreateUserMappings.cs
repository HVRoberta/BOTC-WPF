using BOTC.Application.Features.Users.CreateUser;
using BOTC.Contracts.Users;

namespace BOTC.Presentation.Api.Users.CreateUser;

public static class CreateUserMappings
{
    public static CreateUserCommand ToCommand(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateUserCommand(
            request.Username,
            request.NickName);
    }

    public static CreateUserResponse ToResponse(CreateUserResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CreateUserResponse(
            result.UserId.ToString(),
            result.Username,
            result.NickName);
    }
}