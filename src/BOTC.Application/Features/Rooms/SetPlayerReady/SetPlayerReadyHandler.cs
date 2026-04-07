using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.SetPlayerReady;

public sealed class SetPlayerReadyHandler
{
    private readonly IRoomSetPlayerReadyRepository _roomSetPlayerReadyRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public SetPlayerReadyHandler(
        IRoomSetPlayerReadyRepository roomSetPlayerReadyRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _roomSetPlayerReadyRepository = roomSetPlayerReadyRepository
            ?? throw new ArgumentNullException(nameof(roomSetPlayerReadyRepository));
        _domainEventDispatcher = domainEventDispatcher
            ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    public async Task<SetPlayerReadyResult> HandleAsync(SetPlayerReadyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var playerId = ParsePlayerId(command.PlayerId);
        var room = await _roomSetPlayerReadyRepository.GetByCodeAsync(roomCode, cancellationToken);
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
            var saved = await _roomSetPlayerReadyRepository.TrySaveAsync(room, cancellationToken);
            if (!saved)
            {
                throw new RoomSetPlayerReadyConflictException("Unable to update player readiness due to a conflicting room state.");
            }
        }
        catch (RoomSetPlayerReadySaveRoomMissingException)
        {
            throw new RoomSetPlayerReadyRoomNotFoundException(roomCode);
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

