using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.SetPlayerReady;

public sealed record SetPlayerReadyResult(
    RoomCode RoomCode,
    PlayerId PlayerId,
    bool IsReady);

