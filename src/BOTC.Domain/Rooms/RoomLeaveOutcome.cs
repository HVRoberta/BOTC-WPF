namespace BOTC.Domain.Rooms;

public sealed record RoomLeaveOutcome(
    bool RoomWasRemoved,
    RoomPlayerId? NewHostPlayerId)
{
    public bool HostWasTransferred => NewHostPlayerId.HasValue;
}

