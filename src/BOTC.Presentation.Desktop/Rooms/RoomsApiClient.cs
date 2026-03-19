using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Rooms;

public sealed class RoomsApiClient(HttpClient httpClient) : IRoomsApiClient
{
    private const string CreateRoomPath = "/api/rooms";
    private const string RoomLobbyPathFormat = "/api/rooms/{0}/lobby";

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

    public async Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode ?? string.Empty);
        var requestPath = string.Format(CultureInfo.InvariantCulture, RoomLobbyPathFormat, encodedRoomCode);

        using var response = await httpClient.GetAsync(requestPath, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GetRoomLobbyResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Get room lobby response payload was empty.");
        }

        return payload;
    }
}
