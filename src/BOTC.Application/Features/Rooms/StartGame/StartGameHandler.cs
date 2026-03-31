using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.StartGame;

public sealed class StartGameHandler
{
    private readonly IRoomStartGameRepository roomStartGameRepository;

    public StartGameHandler(IRoomStartGameRepository roomStartGameRepository)
    {
        this.roomStartGameRepository = roomStartGameRepository
            ?? throw new ArgumentNullException(nameof(roomStartGameRepository));
    }

    public async Task<StartGameResult> HandleAsync(StartGameCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var starterPlayerId = ParsePlayerId(command.StarterPlayerId);
        var room = await roomStartGameRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomStartGameRoomNotFoundException(roomCode);
        }

        var outcome = room.StartGame(starterPlayerId);
        if (!outcome.IsStarted)
        {
            return new StartGameResult(roomCode, starterPlayerId, false, outcome.BlockedReason, room.Status);
        }

        try
        {
            var saved = await roomStartGameRepository.TrySaveAsync(room, cancellationToken);
            if (!saved)
            {
                throw new RoomStartGameConflictException("Unable to start game due to a conflicting room state.");
            }
        }
        catch (RoomStartGameSaveRoomMissingException)
        {
            throw new RoomStartGameRoomNotFoundException(roomCode);
        }

        return new StartGameResult(roomCode, starterPlayerId, true, null, room.Status);
    }

    private static RoomPlayerId ParsePlayerId(string playerId)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
        {
            throw new ArgumentException("StarterPlayerId must be a valid GUID.", nameof(playerId));
        }

        return new RoomPlayerId(parsedPlayerId);
    }
}

