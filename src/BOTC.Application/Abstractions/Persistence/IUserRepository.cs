using BOTC.Domain.Users;

namespace BOTC.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<bool> TrySaveAsync(User user, CancellationToken cancellationToken);

    Task<bool> TryDeleteAsync(UserId userId, CancellationToken cancellationToken);
}