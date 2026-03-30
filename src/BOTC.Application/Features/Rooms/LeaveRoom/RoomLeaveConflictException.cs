namespace BOTC.Application.Features.Rooms.LeaveRoom;
public sealed class RoomLeaveConflictException : Exception
{
    public RoomLeaveConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}