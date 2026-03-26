using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Rooms;

public sealed class RoomLobbyQueryService : IRoomLobbyQueryService
{
    private readonly BotcDbContext dbContext;

    public RoomLobbyQueryService(BotcDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Players)
            .SingleOrDefaultAsync(room => room.Code == roomCode.Value, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(RoomStatus), entity.Status))
        {
            throw new InvalidOperationException(
                $"Room '{entity.Code}' contains invalid persisted status value '{entity.Status}'.");
        }

        var players = entity.Players
            .OrderByDescending(player => player.Role == (int)RoomPlayerRole.Host)
            .ThenBy(player => player.JoinedAtUtc)
            .ThenBy(player => player.DisplayName, StringComparer.Ordinal)
            .Select(MapPlayer)
            .ToArray();

        return new GetRoomLobbyResult(
            new RoomCode(entity.Code),
            players,
            (RoomStatus)entity.Status);
    }

    private static LobbyPlayerResult MapPlayer(Persistence.Rooms.RoomPlayerEntity entity)
    {
        if (!Enum.IsDefined(typeof(RoomPlayerRole), entity.Role))
        {
            throw new InvalidOperationException(
                $"Room player '{entity.Id}' contains invalid persisted role value '{entity.Role}'.");
        }

        return new LobbyPlayerResult(
            new RoomPlayerId(entity.Id),
            entity.DisplayName,
            (RoomPlayerRole)entity.Role);
    }
}
