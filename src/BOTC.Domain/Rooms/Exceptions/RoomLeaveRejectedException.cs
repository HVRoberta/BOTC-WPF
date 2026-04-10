using BOTC.Domain.Rooms.Players;

namespace BOTC.Domain.Rooms;

public abstract class RoomLeaveRejectedException : InvalidOperationException
{
    protected RoomLeaveRejectedException(string message)
        : base(message)
    {
    }
}

public sealed class RoomLeavePlayerNotFoundException : RoomLeaveRejectedException
{
    public RoomLeavePlayerNotFoundException(PlayerId playerId)
        : base($"Player with id '{playerId.Value}' was not found in this room.")
    {
        PlayerId = playerId;
    }

    public PlayerId PlayerId { get; }
}
