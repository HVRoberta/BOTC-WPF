using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.StartGame;

public sealed class StartGameHandler
{
    private readonly IRoomStartGameRepository _roomStartGameRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public StartGameHandler(
        IRoomStartGameRepository roomStartGameRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _roomStartGameRepository = roomStartGameRepository
            ?? throw new ArgumentNullException(nameof(roomStartGameRepository));
        _domainEventDispatcher = domainEventDispatcher
            ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    public async Task<StartGameResult> HandleAsync(StartGameCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var starterPlayerId = ParsePlayerId(command.StarterPlayerId);
        var room = await _roomStartGameRepository.GetByCodeAsync(roomCode, cancellationToken);
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
            var saved = await _roomStartGameRepository.TrySaveAsync(room, cancellationToken);
            if (!saved)
            {
                throw new RoomStartGameConflictException("Unable to start game due to a conflicting room state.");
            }
        }
        catch (RoomStartGameSaveRoomMissingException)
        {
            throw new RoomStartGameRoomNotFoundException(roomCode);
        }

        // Dispatch domain events after successful persistence.
        try
        {
            await _domainEventDispatcher.DispatchAsync(room.UncommittedEvents, cancellationToken);
        }
        finally
        {
            room.ClearUncommittedEvents();
        }

        return new StartGameResult(roomCode, starterPlayerId, true, null, room.Status);
    }

    private static PlayerId ParsePlayerId(string playerId)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
        {
            throw new ArgumentException("StarterPlayerId must be a valid GUID.", nameof(playerId));
        }

        return new PlayerId(parsedPlayerId);
    }
}

