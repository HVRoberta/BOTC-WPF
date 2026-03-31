namespace BOTC.Domain.Rooms;

public enum RoomStartGameBlockedReason
{
    StartedByNonHost = 1,
    RoomIsNotWaitingForPlayers = 2,
    NotEnoughPlayers = 3,
    NonHostPlayersNotReady = 4,
    StarterPlayerNotFound = 5
}

public sealed record RoomStartGameOutcome(
    bool IsStarted,
    RoomStartGameBlockedReason? BlockedReason)
{
    public static RoomStartGameOutcome Started() => new(true, null);

    public static RoomStartGameOutcome Blocked(RoomStartGameBlockedReason reason) => new(false, reason);
}

