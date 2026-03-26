using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class JoinRoomHandler
{
    private readonly IRoomJoinRepository _roomJoinRepository;

    public JoinRoomHandler(IRoomJoinRepository roomJoinRepository)
    {
        _roomJoinRepository = roomJoinRepository ?? throw new ArgumentNullException(nameof(roomJoinRepository));
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

        RoomPlayer joinedPlayer;
        try
        {
            joinedPlayer = room.JoinPlayer(command.DisplayName, DateTime.UtcNow);
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

        return new JoinRoomResult(roomCode, joinedPlayer.Id, joinedPlayer.DisplayName);
    }
}
