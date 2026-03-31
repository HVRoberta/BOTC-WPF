namespace BOTC.Application.Features.Rooms.SetPlayerReady;

public sealed record SetPlayerReadyCommand(string RoomCode, string PlayerId, bool IsReady);

