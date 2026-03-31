namespace BOTC.Contracts.Rooms;

public sealed record SetPlayerReadyRequest(string PlayerId, bool IsReady);

