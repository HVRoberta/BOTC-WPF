using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed class RoomLeavePlayerNotFoundException : Exception
{
    public RoomLeavePlayerNotFoundException(RoomCode roomCode, RoomPlayerId playerId)
        : base($"Player with id '{playerId.Value}' was not found in room '{roomCode.Value}'.")
    {
        RoomCode = roomCode;
        PlayerId = playerId;
    }

    public RoomCode RoomCode { get; }

    public RoomPlayerId PlayerId { get; }
}

