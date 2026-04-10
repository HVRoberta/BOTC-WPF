using BOTC.Domain.Users;

namespace BOTC.Domain.Rooms.Exceptions;

public class RoomUserAlreadyJoinedException: RoomJoinRejectedException
{
    public RoomUserAlreadyJoinedException(UserId userId)
        : base($"User '{userId.Value}' has already joined this room.")
    {
        UserId = userId;
    }

    public UserId UserId { get; }
}