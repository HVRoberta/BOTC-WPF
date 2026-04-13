using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Users;

namespace BOTC.Application.Features.Users.CreateUser;

public sealed class CreateUserHandler
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<CreateUserResult> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = User.Create(
            UserId.New(),
            command.Username,
            command.NickName);

        var added = await _userRepository.TryAddAsync(user, cancellationToken);
        if (!added)
        {
            throw new UserAlreadyExistsException(command.Username);
        }

        return new CreateUserResult(
            user.Id,
            user.Username,
            user.NickName);
    }
}