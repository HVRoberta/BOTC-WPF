using BOTC.Application.Abstractions.Events;
using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Outcomes;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed class LeaveRoomHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public LeaveRoomHandler(
        IRoomRepository roomRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    public async Task<LeaveRoomResult> HandleAsync(LeaveRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var playerId = ParsePlayerId(command.PlayerId);
        var room = await _roomRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomLeaveRoomNotFoundException(roomCode);
        }

        RoomLeaveOutcome outcome;
        try
        {
            outcome = room.LeavePlayer(playerId);
        }
        catch (BOTC.Domain.Rooms.RoomLeavePlayerNotFoundException)
        {
            throw new RoomLeavePlayerNotFoundException(roomCode, playerId);
        }

        if (outcome.RoomWasRemoved)
        {
            var deleted = await _roomRepository.TryDeleteAsync(room.Id, cancellationToken);
            if (!deleted)
            {
                throw new RoomLeaveRoomNotFoundException(roomCode);
            }
        }
        else
        {
            try
            {
                var saved = await _roomRepository.TrySaveAsync(room, cancellationToken);
                if (!saved)
                {
                    throw new RoomLeaveConflictException("Unable to leave room due to a conflicting room state.");
                }
            }
            catch (RoomLeaveSaveRoomMissingException)
            {
                throw new RoomLeaveRoomNotFoundException(roomCode);
            }
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

        return new LeaveRoomResult(roomCode, playerId, outcome.RoomWasRemoved, outcome.NewHostPlayerId);
    }

    private static PlayerId ParsePlayerId(string playerId)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
        {
            throw new ArgumentException("PlayerId must be a valid GUID.", nameof(playerId));
        }

        return new PlayerId(parsedPlayerId);
    }
}