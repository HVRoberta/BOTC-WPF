using System.Net.Http;
using System.Net.Http.Json;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Rooms;

public sealed class RoomsApiClient(HttpClient httpClient) : IRoomsApiClient
{
    private const string CreateRoomPath = "/api/rooms";

    public async Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(CreateRoomPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateRoomResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Create room response payload was empty.");
        }

        return payload;
    }
}
