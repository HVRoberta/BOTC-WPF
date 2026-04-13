using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Features.Rooms;

public interface IRoomsApiClient
{
    Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken);

    Task<JoinRoomResponse> JoinRoomAsync(string roomCode, JoinRoomRequest request, CancellationToken cancellationToken);

    Task<LeaveRoomResponse> LeaveRoomAsync(string roomCode, LeaveRoomRequest request, CancellationToken cancellationToken);

    Task<SetPlayerReadyResponse> SetPlayerReadyAsync(string roomCode, SetPlayerReadyRequest request, CancellationToken cancellationToken);

    Task<StartGameResponse> StartGameAsync(string roomCode, StartGameRequest request, CancellationToken cancellationToken);

    Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken);
}
