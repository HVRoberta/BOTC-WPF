using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Errors;
using BOTC.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly BotcDbContext _dbContext;

    public UserRepository(BotcDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entity = UserPersistenceMapper.ToEntity(user);
        entity.CreatedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.Users.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (DatabaseExceptionClassifier.IsUniqueConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var entity = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);

        return entity is null
            ? null
            : UserPersistenceMapper.ToDomain(entity);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var trimmedUsername = username.Trim();

        var entity = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Username == trimmedUsername, cancellationToken);

        return entity is null
            ? null
            : UserPersistenceMapper.ToDomain(entity);
    }

    public async Task<bool> TrySaveAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        _dbContext.ChangeTracker.Clear();

        var entity = await _dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Id == user.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException(
                $"User '{user.Id.Value}' was not found.");
        }

        entity.Username = user.Username;
        entity.NickName = user.NickName;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (DatabaseExceptionClassifier.IsUniqueConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception) when (DatabaseExceptionClassifier.IsForeignKeyConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                $"User '{user.Id.Value}' could not be saved because a related entity is missing.",
                exception);
        }
    }

    public async Task<bool> TryDeleteAsync(UserId userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        _dbContext.ChangeTracker.Clear();

        var entity = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Users.Remove(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (DatabaseExceptionClassifier.IsForeignKeyConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                $"User '{userId.Value}' could not be deleted because it is still referenced by another entity.",
                exception);
        }
    }
}