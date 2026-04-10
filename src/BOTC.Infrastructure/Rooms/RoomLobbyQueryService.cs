using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
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
            .ThenInclude(player => player.User)
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
            .OrderByDescending(player => player.Role == (int)PlayerRole.Host)
            .ThenBy(player => player.JoinedAtUtc)
            .Select(MapPlayer)
            .ToArray();

        return new GetRoomLobbyResult(
            new RoomCode(entity.Code),
            players,
            (RoomStatus)entity.Status);
    }

    private static LobbyPlayerResult MapPlayer(PlayerEntity entity)
    {
        if (!Enum.IsDefined(typeof(PlayerRole), entity.Role))
        {
            throw new InvalidOperationException(
                $"Room player '{entity.Id}' contains invalid persisted role value '{entity.Role}'.");
        }

        var name = entity.User?.NickName 
                   ?? entity.User?.Username 
                   ?? "Unknown";

        return new LobbyPlayerResult(
            new PlayerId(entity.Id),
            name,
            (PlayerRole)entity.Role,
            entity.IsReady);
    }
}
