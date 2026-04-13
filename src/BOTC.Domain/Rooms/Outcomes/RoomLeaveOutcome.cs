namespace BOTC.Domain.Rooms.Outcomes;

public sealed record RoomLeaveOutcome(
    bool RoomWasRemoved,
    PlayerId? NewHostPlayerId)
{
    public bool HostWasTransferred => NewHostPlayerId is not null;
}
