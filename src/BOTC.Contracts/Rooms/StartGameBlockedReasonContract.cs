namespace BOTC.Contracts.Rooms;

public enum StartGameBlockedReasonContract
{
    StartedByNonHost = 1,
    RoomIsNotWaitingForPlayers = 2,
    NotEnoughPlayers = 3,
    NonHostPlayersNotReady = 4,
    StarterPlayerNotFound = 5
}

