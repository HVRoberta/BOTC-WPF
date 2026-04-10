using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BOTC.Infrastructure.Rooms;

public sealed class RoomRepository :
    IRoomRepository,
    IRoomJoinRepository,
    IRoomLeaveRepository,
    IRoomSetPlayerReadyRepository,
    IRoomStartGameRepository
{
    private const int SqliteConstraintErrorCode = 19;
    private const int SqliteConstraintUniqueExtendedErrorCode = 2067;
    private const int SqliteConstraintForeignKeyExtendedErrorCode = 787;
    private const string PostgresUniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;
    private const string PostgresForeignKeyViolationSqlState = PostgresErrorCodes.ForeignKeyViolation;

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
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Players)
            .SingleOrDefaultAsync(room => room.Code == roomCode.Value, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    Task<Room?> IRoomJoinRepository.GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken) =>
        GetByCodeAsync(roomCode, cancellationToken);

    Task<Room?> IRoomLeaveRepository.GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken) =>
        GetByCodeAsync(roomCode, cancellationToken);

    Task<Room?> IRoomSetPlayerReadyRepository.GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken) =>
        GetByCodeAsync(roomCode, cancellationToken);

    Task<Room?> IRoomStartGameRepository.GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken) =>
        GetByCodeAsync(roomCode, cancellationToken);

    Task<bool> IRoomJoinRepository.TrySaveAsync(Room room, CancellationToken cancellationToken) =>
        TrySaveForJoinAsync(room, cancellationToken);

    Task<bool> IRoomLeaveRepository.TrySaveAsync(Room room, CancellationToken cancellationToken) =>
        TrySaveAsyncCore(room, cancellationToken, roomId => new RoomLeaveSaveRoomMissingException(roomId));

    Task<bool> IRoomSetPlayerReadyRepository.TrySaveAsync(Room room, CancellationToken cancellationToken) =>
        TrySaveAsyncCore(room, cancellationToken, roomId => new RoomSetPlayerReadySaveRoomMissingException(roomId));

    Task<bool> IRoomStartGameRepository.TrySaveAsync(Room room, CancellationToken cancellationToken) =>
        TrySaveAsyncCore(room, cancellationToken, roomId => new RoomStartGameSaveRoomMissingException(roomId));

    Task<bool> IRoomLeaveRepository.TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken) =>
        TryDeleteAsyncCore(roomId, cancellationToken);

    private async Task<bool> TrySaveForJoinAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);

        _dbContext.ChangeTracker.Clear();

        var roomExists = await _dbContext.Rooms
            .AsNoTracking()
            .AnyAsync(existingRoom => existingRoom.Id == room.Id.Value, cancellationToken);

        if (!roomExists)
        {
            throw new RoomJoinSaveRoomMissingException(room.Id);
        }

        var persistedPlayerIds = await _dbContext.RoomPlayers
            .AsNoTracking()
            .Where(player => player.RoomId == room.Id.Value)
            .Select(player => player.Id)
            .ToHashSetAsync(cancellationToken);

        var playersToInsert = room.Players
            .Where(player => !persistedPlayerIds.Contains(player.Id.Value))
            .Select(player => MapToEntity(player, room.Id))
            .ToArray();

        if (playersToInsert.Length == 0)
        {
            return true;
        }

        _dbContext.RoomPlayers.AddRange(playersToInsert);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception) when (IsForeignKeyConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            throw new RoomJoinSaveRoomMissingException(room.Id);
        }
    }

    private async Task<bool> TrySaveAsyncCore(
        Room room,
        CancellationToken cancellationToken,
        Func<RoomId, Exception> createMissingRoomException)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (room.Players.Count == 0)
        {
            throw new ArgumentException("Room must contain at least one player when being saved.", nameof(room));
        }

        _dbContext.ChangeTracker.Clear();

        var entity = await _dbContext.Rooms
            .Include(existingRoom => existingRoom.Players)
            .SingleOrDefaultAsync(existingRoom => existingRoom.Id == room.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw createMissingRoomException(room.Id);
        }

        if (!string.Equals(entity.Code, room.Code.Value, StringComparison.Ordinal))
        {
            entity.Code = room.Code.Value;
        }

        if (entity.Status != (int)room.Status)
        {
            entity.Status = (int)room.Status;
        }

        if (!AreSameUtcInstant(entity.CreatedAtUtc, room.CreatedAtUtc))
        {
            entity.CreatedAtUtc = room.CreatedAtUtc;
        }

        SynchronizePlayers(entity, room);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception) when (IsForeignKeyConstraintViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            throw createMissingRoomException(room.Id);
        }
    }

    private async Task<bool> TryDeleteAsyncCore(RoomId roomId, CancellationToken cancellationToken)
    {
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

    private void SynchronizePlayers(RoomEntity entity, Room room)
    {
        var desiredPlayersById = room.Players.ToDictionary(player => player.Id.Value);

        foreach (var persistedPlayer in entity.Players.Where(player => !desiredPlayersById.ContainsKey(player.Id)).ToArray())
        {
            _dbContext.RoomPlayers.Remove(persistedPlayer);
        }

        foreach (var desiredPlayer in room.Players)
        {
            var persistedPlayer = entity.Players.SingleOrDefault(player => player.Id == desiredPlayer.Id.Value);
            if (persistedPlayer is null)
            {
                entity.Players.Add(MapToEntity(desiredPlayer, room.Id));
                continue;
            }

            if (persistedPlayer.UserId != desiredPlayer.UserId.Value)
            {
                persistedPlayer.UserId = desiredPlayer.UserId.Value;
            }

            if (persistedPlayer.Role != (int)desiredPlayer.Role)
            {
                persistedPlayer.Role = (int)desiredPlayer.Role;
            }

            if (!AreSameUtcInstant(persistedPlayer.JoinedAtUtc, desiredPlayer.JoinedAtUtc))
            {
                persistedPlayer.JoinedAtUtc = desiredPlayer.JoinedAtUtc;
            }

            if (persistedPlayer.IsReady != desiredPlayer.IsReady)
            {
                persistedPlayer.IsReady = desiredPlayer.IsReady;
            }
        }
    }

    private static RoomEntity MapToEntity(Room room)
    {
        return new RoomEntity
        {
            Id = room.Id.Value,
            Code = room.Code.Value,
            Name = room.Name,
            Status = (int)room.Status,
            CreatedAtUtc = room.CreatedAtUtc,
            Players = room.Players
                .Select(player => MapToEntity(player, room.Id))
                .ToList()
        };
    }

    private static PlayerEntity MapToEntity(Player player, RoomId roomId)
    {
        return new PlayerEntity
        {
            Id = player.Id.Value,
            RoomId = roomId.Value,
            UserId = player.UserId.Value,
            Role = (int)player.Role,
            JoinedAtUtc = player.JoinedAtUtc,
            IsReady = player.IsReady
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
            entity.Name,
            players,
            (RoomStatus)entity.Status,
            NormalizePersistedUtc(entity.CreatedAtUtc));
    }

    private static Player MapToDomain(PlayerEntity entity)
    {
        if (!Enum.IsDefined(typeof(PlayerRole), entity.Role))
        {
            throw new InvalidOperationException(
                $"Room player '{entity.Id}' contains invalid persisted role value '{entity.Role}'.");
        }

        return Player.Rehydrate(
            new PlayerId(entity.Id),
            new UserId(entity.UserId),
            (PlayerRole)entity.Role,
            NormalizePersistedUtc(entity.JoinedAtUtc),
            entity.IsReady);
    }

    private static DateTime NormalizePersistedUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported DateTime kind.")
        };
    }

    private static bool AreSameUtcInstant(DateTime left, DateTime right)
    {
        var normalizedLeft = NormalizePersistedUtc(left);
        var normalizedRight = NormalizePersistedUtc(right);
        return normalizedLeft == normalizedRight;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            PostgresException postgresException => postgresException.SqlState == PostgresUniqueViolationSqlState,
            not null when IsSqliteConstraintViolation(
                exception.InnerException,
                SqliteConstraintErrorCode,
                SqliteConstraintUniqueExtendedErrorCode) => true,
            _ => false
        };
    }

    private static bool IsForeignKeyConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            PostgresException postgresException => postgresException.SqlState == PostgresForeignKeyViolationSqlState,
            not null when IsSqliteConstraintViolation(
                exception.InnerException,
                SqliteConstraintErrorCode,
                SqliteConstraintForeignKeyExtendedErrorCode) => true,
            _ => false
        };
    }

    private static bool IsSqliteConstraintViolation(Exception exception, int errorCode, int extendedErrorCode)
    {
        var exceptionType = exception.GetType();
        if (!string.Equals(exceptionType.FullName, "Microsoft.Data.Sqlite.SqliteException", StringComparison.Ordinal))
        {
            return false;
        }

        var sqliteErrorCode = exceptionType.GetProperty("SqliteErrorCode")?.GetValue(exception) as int?;
        var sqliteExtendedErrorCode = exceptionType.GetProperty("SqliteExtendedErrorCode")?.GetValue(exception) as int?;

        return sqliteErrorCode == errorCode
               && sqliteExtendedErrorCode == extendedErrorCode;
    }
}