using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.StartGame;

public sealed record StartGameResult(
    RoomCode RoomCode,
    PlayerId StarterPlayerId,
    bool IsStarted,
    RoomStartGameBlockedReason? BlockedReason,
    RoomStatus RoomStatus);

