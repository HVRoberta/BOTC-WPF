using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.SetPlayerReady;

public sealed class SetPlayerReadyHandler
{
    private readonly IRoomSetPlayerReadyRepository roomSetPlayerReadyRepository;

    public SetPlayerReadyHandler(IRoomSetPlayerReadyRepository roomSetPlayerReadyRepository)
    {
        this.roomSetPlayerReadyRepository = roomSetPlayerReadyRepository
            ?? throw new ArgumentNullException(nameof(roomSetPlayerReadyRepository));
    }

    public async Task<SetPlayerReadyResult> HandleAsync(SetPlayerReadyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var playerId = ParsePlayerId(command.PlayerId);
        var room = await roomSetPlayerReadyRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomSetPlayerReadyRoomNotFoundException(roomCode);
        }

        try
        {
            room.SetPlayerReady(playerId, command.IsReady);
        }
        catch (BOTC.Domain.Rooms.RoomSetPlayerReadyPlayerNotFoundException)
        {
            throw new RoomSetPlayerReadyPlayerNotFoundException(roomCode, playerId);
        }
        catch (RoomSetPlayerReadyRejectedException exception)
        {
            throw new RoomSetPlayerReadyConflictException(exception.Message, exception);
        }

        try
        {
            var saved = await roomSetPlayerReadyRepository.TrySaveAsync(room, cancellationToken);
            if (!saved)
            {
                throw new RoomSetPlayerReadyConflictException("Unable to update player readiness due to a conflicting room state.");
            }
        }
        catch (RoomSetPlayerReadySaveRoomMissingException)
        {
            throw new RoomSetPlayerReadyRoomNotFoundException(roomCode);
        }

        return new SetPlayerReadyResult(roomCode, playerId, command.IsReady);
    }

    private static RoomPlayerId ParsePlayerId(string playerId)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
        {
            throw new ArgumentException("PlayerId must be a valid GUID.", nameof(playerId));
        }

        return new RoomPlayerId(parsedPlayerId);
    }
}

