using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Rooms;

public sealed class RoomRepository : IRoomRepository, IRoomJoinRepository
{
    private const int SqliteConstraintErrorCode = 19;
    private const int SqliteConstraintUniqueExtendedErrorCode = 2067;

    private readonly BotcDbContext _dbContext;

    public RoomRepository(BotcDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);

        var entity = MapToEntity(room);
        _dbContext.Rooms.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Rooms
            .Include(room => room.Players)
            .SingleOrDefaultAsync(room => room.Code == roomCode.Value, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);

        var entity = await _dbContext.Rooms
            .Include(existingRoom => existingRoom.Players)
            .SingleOrDefaultAsync(existingRoom => existingRoom.Id == room.Id.Value, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Status = (int)room.Status;

        var persistedPlayerIds = entity.Players
            .Select(player => player.Id)
            .ToHashSet();

        foreach (var player in room.Players.Where(player => !persistedPlayerIds.Contains(player.Id.Value)))
        {
            entity.Players.Add(MapToEntity(player, room.Id));
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return false;
        }
    }

    private static RoomEntity MapToEntity(Room room)
    {
        return new RoomEntity
        {
            Id = room.Id.Value,
            Code = room.Code.Value,
            Status = (int)room.Status,
            CreatedAtUtc = room.CreatedAtUtc,
            Players = room.Players
                .Select(player => MapToEntity(player, room.Id))
                .ToList()
        };
    }

    private static RoomPlayerEntity MapToEntity(RoomPlayer player, RoomId roomId)
    {
        return new RoomPlayerEntity
        {
            Id = player.Id.Value,
            RoomId = roomId.Value,
            DisplayName = player.DisplayName,
            NormalizedDisplayName = player.NormalizedDisplayName,
            Role = (int)player.Role,
            JoinedAtUtc = player.JoinedAtUtc
        };
    }

    private static Room MapToDomain(RoomEntity entity)
    {
        if (!Enum.IsDefined(typeof(RoomStatus), entity.Status))
        {
            throw new InvalidOperationException(
                $"Room '{entity.Code}' contains invalid persisted status value '{entity.Status}'.");
        }

        var players = entity.Players
            .Select(MapToDomain)
            .ToArray();

        return Room.Rehydrate(
            new RoomId(entity.Id),
            new RoomCode(entity.Code),
            players,
            (RoomStatus)entity.Status,
            entity.CreatedAtUtc);
    }

    private static RoomPlayer MapToDomain(RoomPlayerEntity entity)
    {
        if (!Enum.IsDefined(typeof(RoomPlayerRole), entity.Role))
        {
            throw new InvalidOperationException(
                $"Room player '{entity.Id}' contains invalid persisted role value '{entity.Role}'.");
        }

        return RoomPlayer.Rehydrate(
            new RoomPlayerId(entity.Id),
            entity.DisplayName,
            entity.NormalizedDisplayName,
            (RoomPlayerRole)entity.Role,
            entity.JoinedAtUtc);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException
               && sqliteException.SqliteErrorCode == SqliteConstraintErrorCode
               && sqliteException.SqliteExtendedErrorCode == SqliteConstraintUniqueExtendedErrorCode;
    }
}
