using BOTC.Domain.Rooms;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence.Entities;

namespace BOTC.Infrastructure.Persistence.Mappers;

internal static class RoomPersistenceMapper
{
    public static RoomEntity ToEntity(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomEntity
        {
            Id = room.Id.Value,
            Code = room.Code.Value,
            Name = room.Name,
            Status = (int)room.Status,
            CreatedAtUtc = room.CreatedAtUtc,
            Players = room.Players
                .Select(player => ToEntity(player, room.Id))
                .ToList()
        };
    }

    public static PlayerEntity ToEntity(Player player, RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(player);

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

    public static Room ToDomain(RoomEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(RoomStatus), entity.Status))
        {
            throw new InvalidOperationException(
                $"Room '{entity.Code}' contains invalid persisted status value '{entity.Status}'.");
        }

        var players = entity.Players
            .Select(ToDomain)
            .ToArray();

        return Room.Rehydrate(
            new RoomId(entity.Id),
            new RoomCode(entity.Code),
            entity.Name,
            players,
            (RoomStatus)entity.Status,
            NormalizePersistedUtc(entity.CreatedAtUtc));
    }

    public static Player ToDomain(PlayerEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

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

    public static bool AreSameUtcInstant(DateTime left, DateTime right)
    {
        return NormalizePersistedUtc(left) == NormalizePersistedUtc(right);
    }
}