using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed class RoomLeaveRoomNotFoundException : Exception
{
    public RoomLeaveRoomNotFoundException(RoomCode roomCode)
        : base($"Room with code '{roomCode.Value}' was not found.")
    {
        RoomCode = roomCode;
    }

    public RoomCode RoomCode { get; }
}

