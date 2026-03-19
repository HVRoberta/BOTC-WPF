using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Rooms;

public sealed class RoomLobbyReadRepository : IRoomLobbyReadRepository
{
    private readonly BotcDbContext dbContext;

    public RoomLobbyReadRepository(BotcDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Rooms
            .AsNoTracking()
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

        return new GetRoomLobbyResult(
            new RoomCode(entity.Code),
            entity.HostDisplayName,
            (RoomStatus)entity.Status);
    }
}
