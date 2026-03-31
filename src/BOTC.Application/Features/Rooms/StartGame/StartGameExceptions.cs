using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.StartGame;

public sealed class RoomStartGameRoomNotFoundException : Exception
{
    public RoomStartGameRoomNotFoundException(RoomCode roomCode)
        : base($"Room with code '{roomCode.Value}' was not found.")
    {
        RoomCode = roomCode;
    }

    public RoomCode RoomCode { get; }
}

public sealed class RoomStartGameConflictException : Exception
{
    public RoomStartGameConflictException(string message)
        : base(message)
    {
    }
}

public sealed class RoomStartGameSaveRoomMissingException : Exception
{
    public RoomStartGameSaveRoomMissingException(RoomId roomId)
        : base($"Room with id '{roomId.Value}' no longer exists.")
    {
        RoomId = roomId;
    }

    public RoomId RoomId { get; }
}

