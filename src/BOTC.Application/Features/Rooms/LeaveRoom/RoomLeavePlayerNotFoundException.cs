using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed class RoomLeavePlayerNotFoundException : Exception
{
    public RoomLeavePlayerNotFoundException(RoomCode roomCode, PlayerId playerId)
        : base($"Player with id '{playerId.Value}' was not found in room '{roomCode.Value}'.")
    {
        RoomCode = roomCode;
        PlayerId = playerId;
    }

    public RoomCode RoomCode { get; }

    public PlayerId PlayerId { get; }
}

