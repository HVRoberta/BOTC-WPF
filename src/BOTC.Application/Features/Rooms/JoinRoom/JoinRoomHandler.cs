using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Exceptions;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class JoinRoomHandler
{
    private readonly IRoomJoinRepository _roomJoinRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public JoinRoomHandler(
        IRoomJoinRepository roomJoinRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _roomJoinRepository = roomJoinRepository ?? throw new ArgumentNullException(nameof(roomJoinRepository));
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    public async Task<JoinRoomResult> HandleAsync(JoinRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _roomJoinRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomJoinNotFoundException(roomCode);
        }

        Player joinedPlayer;
        try
        {
            joinedPlayer = room.JoinPlayer(command.UserId, DateTime.UtcNow);
        }
        catch (RoomJoinRejectedException exception)
        {
            throw new RoomJoinConflictException(exception.Message, exception);
        }

        try
        {
            var saved = await _roomJoinRepository.TrySaveAsync(room, cancellationToken);
            if (!saved)
            {
                throw new RoomJoinConflictException("Unable to join room due to a conflicting room state.");
            }
        }
        catch (RoomJoinSaveRoomMissingException)
        {
            throw new RoomJoinNotFoundException(roomCode);
        }

        // Dispatch domain events after successful persistence.
        // This ensures notifications are only sent if the room state has been persisted.
        try
        {
            await _domainEventDispatcher.DispatchAsync(room.UncommittedEvents, cancellationToken);
        }
        finally
        {
            // Clear events to prevent re-dispatch if the handler is called again.
            room.ClearUncommittedEvents();
        }

        return new JoinRoomResult(roomCode, joinedPlayer.Id, command.Name);
    }
}
