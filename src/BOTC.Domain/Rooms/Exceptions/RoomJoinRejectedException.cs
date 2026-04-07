namespace BOTC.Domain.Rooms;

public abstract class RoomJoinRejectedException : InvalidOperationException
{
    protected RoomJoinRejectedException(string message)
        : base(message)
    {
    }
}

public sealed class RoomJoinNotAllowedException : RoomJoinRejectedException
{
    public RoomJoinNotAllowedException()
        : base("Room does not accept new players in its current state.")
    {
    }
}

public sealed class RoomJoinDisplayNameAlreadyInUseException : RoomJoinRejectedException
{
    public RoomJoinDisplayNameAlreadyInUseException()
        : base("Display name is already in use for this room.")
    {
    }
}

public sealed class RoomJoinCapacityReachedException : RoomJoinRejectedException
{
    public RoomJoinCapacityReachedException(int maxPlayers)
        : base($"Room cannot exceed {maxPlayers} participants.")
    {
    }
}

