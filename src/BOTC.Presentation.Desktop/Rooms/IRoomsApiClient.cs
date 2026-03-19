using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Rooms;

public interface IRoomsApiClient
{
    Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken);
}

