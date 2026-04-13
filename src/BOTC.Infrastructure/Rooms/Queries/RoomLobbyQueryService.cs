using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Rooms.Queries;

public sealed class RoomLobbyQueryService : IRoomLobbyQueryService
{
    private readonly BotcDbContext _dbContext;

    public RoomLobbyQueryService(BotcDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetRoomLobbyResult?> GetByRoomCodeAsync(
        RoomCode roomCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roomCode);

        var roomData = await _dbContext.Rooms
            .AsNoTracking()
            .Where(room => room.Code == roomCode.Value)
            .Select(room => new
            {
                room.Code,
                room.Status,
                Players = room.Players
                    .OrderByDescending(player => player.Role == (int)PlayerRole.Host)
                    .ThenBy(player => player.JoinedAtUtc)
                    .Select(player => new
                    {
                        player.Id,
                        player.Role,
                        player.IsReady,
                        player.User,
                        Name = player.User.NickName ?? player.User.Username
                    })
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (roomData is null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(RoomStatus), roomData.Status))
        {
            throw new InvalidOperationException(
                $"Room '{roomData.Code}' contains invalid persisted status value '{roomData.Status}'.");
        }

        var players = roomData.Players
            .Select(player =>
            {
                if (!Enum.IsDefined(typeof(PlayerRole), player.Role))
                {
                    throw new InvalidOperationException(
                        $"Room player '{player.Id}' contains invalid persisted role value '{player.Role}'.");
                }

                return new LobbyPlayerResult(
                    new PlayerId(player.Id),
                    player.Name,
                    (PlayerRole)player.Role,
                    player.IsReady);
            })
            .ToArray();

        return new GetRoomLobbyResult(
            new RoomCode(roomData.Code),
            players,
            (RoomStatus)roomData.Status);
    }
}