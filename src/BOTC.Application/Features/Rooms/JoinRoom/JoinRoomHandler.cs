using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class JoinRoomHandler
{
    private readonly IRoomJoinRepository roomJoinRepository;

    public JoinRoomHandler(IRoomJoinRepository roomJoinRepository)
    {
        this.roomJoinRepository = roomJoinRepository ?? throw new ArgumentNullException(nameof(roomJoinRepository));
    }

    public async Task<JoinRoomResult> HandleAsync(JoinRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await roomJoinRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomJoinNotFoundException(roomCode);
        }

        RoomPlayer joinedPlayer;
        try
        {
            joinedPlayer = room.JoinPlayer(command.DisplayName, DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new RoomJoinConflictException(exception.Message, exception);
        }

        var saved = await roomJoinRepository.TrySaveAsync(room, cancellationToken);
        if (!saved)
        {
            throw new RoomJoinConflictException("Player display name is already in use for this room.");
        }

        return new JoinRoomResult(roomCode, joinedPlayer.Id, joinedPlayer.DisplayName);
    }
}

