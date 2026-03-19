using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed class RoomLobbyNotFoundException : Exception
{
    public RoomLobbyNotFoundException(RoomCode roomCode)
        : base($"Room with code '{roomCode.Value}' was not found.")
    {
        RoomCode = roomCode;
    }

    public RoomCode RoomCode { get; }
}

