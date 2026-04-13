namespace BOTC.Domain.Rooms.Exceptions;

public abstract class RoomSetPlayerReadyRejectedException : InvalidOperationException
{
    protected RoomSetPlayerReadyRejectedException(string message)
        : base(message)
    {
    }
}

public sealed class RoomSetPlayerReadyNotAllowedException : RoomSetPlayerReadyRejectedException
{
    public RoomSetPlayerReadyNotAllowedException()
        : base("Player readiness can only be changed while waiting for players.")
    {
    }
}

public sealed class RoomSetPlayerReadyPlayerNotFoundException : RoomSetPlayerReadyRejectedException
{
    public RoomSetPlayerReadyPlayerNotFoundException(PlayerId playerId)
        : base($"Player with id '{playerId.Value}' was not found in this room.")
    {
        PlayerId = playerId;
    }

    public PlayerId PlayerId { get; }
}
