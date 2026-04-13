using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence.Entities;
using BOTC.Infrastructure.Persistence.Mappers;

namespace BOTC.Infrastructure.Persistence.Synchronizers;

internal static class RoomEntitySynchronizer
{
    public static void Synchronize(RoomEntity entity, Room room, ICollection<PlayerEntity> trackedPlayers)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(trackedPlayers);

        if (!string.Equals(entity.Code, room.Code.Value, StringComparison.Ordinal))
        {
            entity.Code = room.Code.Value;
        }

        if (entity.Name != room.Name)
        {
            entity.Name = room.Name;
        }

        if (entity.Status != (int)room.Status)
        {
            entity.Status = (int)room.Status;
        }

        if (!RoomPersistenceMapper.AreSameUtcInstant(entity.CreatedAtUtc, room.CreatedAtUtc))
        {
            entity.CreatedAtUtc = room.CreatedAtUtc;
        }

        SynchronizePlayers(entity, room, trackedPlayers);
    }

    private static void SynchronizePlayers(RoomEntity entity, Room room, ICollection<PlayerEntity> trackedPlayers)
    {
        var desiredPlayersById = room.Players.ToDictionary(player => player.Id.Value);

        foreach (var persistedPlayer in trackedPlayers
                     .Where(player => !desiredPlayersById.ContainsKey(player.Id))
                     .ToArray())
        {
            entity.Players.Remove(persistedPlayer);
        }

        foreach (var desiredPlayer in room.Players)
        {
            var persistedPlayer = trackedPlayers.SingleOrDefault(player => player.Id == desiredPlayer.Id.Value);

            if (persistedPlayer is null)
            {
                entity.Players.Add(RoomPersistenceMapper.ToEntity(desiredPlayer, room.Id));
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

            if (!RoomPersistenceMapper.AreSameUtcInstant(persistedPlayer.JoinedAtUtc, desiredPlayer.JoinedAtUtc))
            {
                persistedPlayer.JoinedAtUtc = desiredPlayer.JoinedAtUtc;
            }

            if (persistedPlayer.IsReady != desiredPlayer.IsReady)
            {
                persistedPlayer.IsReady = desiredPlayer.IsReady;
            }
        }
    }
}