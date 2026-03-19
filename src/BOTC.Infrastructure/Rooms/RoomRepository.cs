using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Rooms;

public sealed class RoomRepository : IRoomRepository
{
    private const int SqliteConstraintUnique = 2067;
    private const int SqliteConstraintPrimaryKey = 1555;

    private readonly BotcDbContext dbContext;

    public RoomRepository(BotcDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);
        
        var entity = MapToEntity(room);
        dbContext.Rooms.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            // Room code already exists; caller should retry with a different code.
            dbContext.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    private static RoomEntity MapToEntity(Room room)
    {
        return new RoomEntity
        {
            Id = room.Id.Value,
            Code = room.Code.Value,
            HostDisplayName = room.HostDisplayName,
            Status = (int)room.Status,
            CreatedAtUtc = room.CreatedAtUtc
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException
               && sqliteException.SqliteExtendedErrorCode is SqliteConstraintUnique or SqliteConstraintPrimaryKey;
    }
}

