namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed class RoomJoinConflictException : Exception
{
    public RoomJoinConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

