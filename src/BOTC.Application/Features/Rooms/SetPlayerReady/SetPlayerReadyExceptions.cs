using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.SetPlayerReady;

public sealed class RoomSetPlayerReadyRoomNotFoundException : Exception
{
    public RoomSetPlayerReadyRoomNotFoundException(RoomCode roomCode)
        : base($"Room with code '{roomCode.Value}' was not found.")
    {
        RoomCode = roomCode;
    }

    public RoomCode RoomCode { get; }
}

public sealed class RoomSetPlayerReadyPlayerNotFoundException : Exception
{
    public RoomSetPlayerReadyPlayerNotFoundException(RoomCode roomCode, PlayerId playerId)
        : base($"Player with id '{playerId.Value}' was not found in room '{roomCode.Value}'.")
    {
        RoomCode = roomCode;
        PlayerId = playerId;
    }

    public RoomCode RoomCode { get; }

    public PlayerId PlayerId { get; }
}

public sealed class RoomSetPlayerReadyConflictException : Exception
{
    public RoomSetPlayerReadyConflictException(string message)
        : base(message)
    {
    }

    public RoomSetPlayerReadyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class RoomSetPlayerReadySaveRoomMissingException : Exception
{
    public RoomSetPlayerReadySaveRoomMissingException(RoomId roomId)
        : base($"Room with id '{roomId.Value}' no longer exists.")
    {
        RoomId = roomId;
    }

    public RoomId RoomId { get; }
}

