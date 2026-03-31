namespace BOTC.Contracts.Rooms;

public sealed record StartGameResponse(
    string RoomCode,
    string StarterPlayerId,
    bool IsStarted,
    StartGameBlockedReasonContract? BlockedReason,
    RoomStatusContract RoomStatus);

