using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class RoomJoinSaveRoomMissingException : Exception
{
    public RoomJoinSaveRoomMissingException(RoomId roomId)
        : base($"Room '{roomId.Value}' no longer exists.")
    {
        RoomId = roomId;
    }

    public RoomId RoomId { get; }
}

