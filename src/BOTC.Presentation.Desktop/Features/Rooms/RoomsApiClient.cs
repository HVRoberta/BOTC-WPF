using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Features.Rooms;

public sealed class RoomsApiClient(HttpClient httpClient) : IRoomsApiClient
{
    private const string CreateRoomPath = "/api/rooms";
    private const string JoinRoomPathFormat = "/api/rooms/{0}/join";
    private const string LeaveRoomPathFormat = "/api/rooms/{0}/leave";
    private const string SetPlayerReadyPathFormat = "/api/rooms/{0}/ready";
    private const string StartGamePathFormat = "/api/rooms/{0}/start";
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

    public async Task<JoinRoomResponse> JoinRoomAsync(string roomCode, JoinRoomRequest request, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode);
        var requestPath = string.Format(CultureInfo.InvariantCulture, JoinRoomPathFormat, encodedRoomCode);

        using var response = await httpClient.PostAsJsonAsync(requestPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JoinRoomResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Join room response payload was empty.");
        }

        return payload;
    }

    public async Task<LeaveRoomResponse> LeaveRoomAsync(string roomCode, LeaveRoomRequest request, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode);
        var requestPath = string.Format(CultureInfo.InvariantCulture, LeaveRoomPathFormat, encodedRoomCode);

        using var response = await httpClient.PostAsJsonAsync(requestPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LeaveRoomResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Leave room response payload was empty.");
        }

        return payload;
    }

    public async Task<SetPlayerReadyResponse> SetPlayerReadyAsync(string roomCode, SetPlayerReadyRequest request, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode);
        var requestPath = string.Format(CultureInfo.InvariantCulture, SetPlayerReadyPathFormat, encodedRoomCode);

        using var response = await httpClient.PostAsJsonAsync(requestPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SetPlayerReadyResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Set player ready response payload was empty.");
        }

        return payload;
    }

    public async Task<StartGameResponse> StartGameAsync(string roomCode, StartGameRequest request, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode);
        var requestPath = string.Format(CultureInfo.InvariantCulture, StartGamePathFormat, encodedRoomCode);

        using var response = await httpClient.PostAsJsonAsync(requestPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StartGameResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Start game response payload was empty.");
        }

        return payload;
    }

    public async Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken)
    {
        var encodedRoomCode = Uri.EscapeDataString(roomCode);
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
