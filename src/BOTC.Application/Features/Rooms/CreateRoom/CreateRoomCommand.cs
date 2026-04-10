using BOTC.Domain.Users;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed record CreateRoomCommand(
    UserId HostUserId,
    string RoomName);

