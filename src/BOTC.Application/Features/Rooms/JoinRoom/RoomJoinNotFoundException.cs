using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class RoomJoinNotFoundException : Exception
{
    public RoomJoinNotFoundException(RoomCode roomCode)
        : base($"Room with code '{roomCode.Value}' was not found.")
    {
        RoomCode = roomCode;
    }

    public RoomCode RoomCode { get; }
}

