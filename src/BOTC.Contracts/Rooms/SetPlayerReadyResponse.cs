namespace BOTC.Contracts.Rooms;

public sealed record SetPlayerReadyResponse(string RoomCode, string PlayerId, bool IsReady);

