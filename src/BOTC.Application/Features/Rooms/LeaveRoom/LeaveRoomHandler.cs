using BOTC.Domain.Rooms;
namespace BOTC.Application.Features.Rooms.LeaveRoom;
public sealed class LeaveRoomHandler
{
    private readonly IRoomLeaveRepository _roomLeaveRepository;
    public LeaveRoomHandler(IRoomLeaveRepository roomLeaveRepository)
    {
        _roomLeaveRepository = roomLeaveRepository ?? throw new ArgumentNullException(nameof(roomLeaveRepository));
    }
    public async Task<LeaveRoomResult> HandleAsync(LeaveRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var roomCode = new RoomCode(command.RoomCode);
        var playerId = ParsePlayerId(command.PlayerId);
        var room = await _roomLeaveRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            throw new RoomLeaveRoomNotFoundException(roomCode);
        }
        RoomLeaveOutcome outcome;
        try
        {
            outcome = room.LeavePlayer(playerId);
        }
        catch (BOTC.Domain.Rooms.RoomLeavePlayerNotFoundException)
        {
            throw new RoomLeavePlayerNotFoundException(roomCode, playerId);
        }
        if (outcome.RoomWasRemoved)
        {
            var deleted = await _roomLeaveRepository.TryDeleteAsync(room.Id, cancellationToken);
            if (!deleted)
            {
                throw new RoomLeaveRoomNotFoundException(roomCode);
            }
        }
        else
        {
            try
            {
                var saved = await _roomLeaveRepository.TrySaveAsync(room, cancellationToken);
                if (!saved)
                {
                    throw new RoomLeaveConflictException("Unable to leave room due to a conflicting room state.");
                }
            }
            catch (RoomLeaveSaveRoomMissingException)
            {
                throw new RoomLeaveRoomNotFoundException(roomCode);
            }
        }
        return new LeaveRoomResult(roomCode, playerId, outcome.RoomWasRemoved, outcome.NewHostPlayerId);
    }
    private static RoomPlayerId ParsePlayerId(string playerId)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
        {
            throw new ArgumentException("PlayerId must be a valid GUID.", nameof(playerId));
        }
        return new RoomPlayerId(parsedPlayerId);
    }
}