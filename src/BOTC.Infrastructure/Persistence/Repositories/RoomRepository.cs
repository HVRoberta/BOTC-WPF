using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Errors;
using BOTC.Infrastructure.Persistence.Mappers;
using BOTC.Infrastructure.Persistence.Synchronizers;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly BotcDbContext _dbContext;

    public RoomRepository(BotcDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);

        var entity = RoomPersistenceMapper.ToEntity(room);
        _dbContext.Rooms.Add(entity);

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

    public async Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roomCode);

        var entity = await _dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Players)
            .SingleOrDefaultAsync(room => room.Code == roomCode.Value, cancellationToken);

        return entity is null
            ? null
            : RoomPersistenceMapper.ToDomain(entity);
    }

    public async Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (room.Players.Count == 0)
        {
            throw new ArgumentException(
                "Room must contain at least one player when being saved.",
                nameof(room));
        }

        _dbContext.ChangeTracker.Clear();

        var entity = await _dbContext.Rooms
            .Include(existingRoom => existingRoom.Players)
            .SingleOrDefaultAsync(existingRoom => existingRoom.Id == room.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException(
                $"Room '{room.Id.Value}' was not found.");
        }

        RoomEntitySynchronizer.Synchronize(entity, room, entity.Players);

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
                $"Room '{room.Id.Value}' could not be saved because a related entity is missing.",
                exception);
        }
    }

    public async Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        _dbContext.ChangeTracker.Clear();

        var entity = await _dbContext.Rooms
            .SingleOrDefaultAsync(room => room.Id == roomId.Value, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Rooms.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return true;
    }
}