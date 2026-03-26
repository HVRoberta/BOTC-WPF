using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR.Client;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public sealed class RoomLobbyRealtimeClient : IRoomLobbyRealtimeClient, IDisposable
{
    private readonly HubConnection hubConnection;
    private string? subscribedRoomCode;
    private bool isDisposed;

    public RoomLobbyRealtimeClient(Uri roomsApiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(roomsApiBaseAddress);

        var hubUri = new Uri(roomsApiBaseAddress, RoomLobbyHubContract.HubRoute);
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<string>(RoomLobbyHubContract.LobbyUpdatedEvent, OnLobbyUpdatedAsync);
        hubConnection.Reconnected += OnReconnectedAsync;
    }

    public event Func<string, Task>? LobbyUpdated;

    public async Task SubscribeAsync(string roomCode, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var previousRoomCode = subscribedRoomCode;
        subscribedRoomCode = normalizedRoomCode;

        await EnsureStartedAsync(cancellationToken);

        if (hubConnection.State != HubConnectionState.Connected)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(previousRoomCode)
            && !string.Equals(previousRoomCode, normalizedRoomCode, StringComparison.Ordinal))
        {
            await hubConnection.InvokeAsync(RoomLobbyHubContract.LeaveLobbyGroupMethod, previousRoomCode, cancellationToken);
        }

        await hubConnection.InvokeAsync(RoomLobbyHubContract.JoinLobbyGroupMethod, normalizedRoomCode, cancellationToken);
    }

    public async Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var roomCodeToLeave = string.IsNullOrWhiteSpace(roomCode)
            ? subscribedRoomCode
            : NormalizeRoomCode(roomCode);

        subscribedRoomCode = null;

        if (hubConnection.State == HubConnectionState.Connected && !string.IsNullOrWhiteSpace(roomCodeToLeave))
        {
            await hubConnection.InvokeAsync(RoomLobbyHubContract.LeaveLobbyGroupMethod, roomCodeToLeave, cancellationToken);
        }

        if (hubConnection.State != HubConnectionState.Disconnected)
        {
            await hubConnection.StopAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        subscribedRoomCode = null;
        hubConnection.Reconnected -= OnReconnectedAsync;
        await hubConnection.DisposeAsync();
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (hubConnection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        await hubConnection.StartAsync(cancellationToken);
    }

    private async Task OnLobbyUpdatedAsync(string roomCode)
    {
        var handler = LobbyUpdated;
        if (handler is null)
        {
            return;
        }

        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        foreach (var callback in handler.GetInvocationList().Cast<Func<string, Task>>())
        {
            await callback(normalizedRoomCode);
        }
    }

    private async Task OnReconnectedAsync(string? _)
    {
        if (string.IsNullOrWhiteSpace(subscribedRoomCode))
        {
            return;
        }

        await hubConnection.InvokeAsync(RoomLobbyHubContract.JoinLobbyGroupMethod, subscribedRoomCode);
        await OnLobbyUpdatedAsync(subscribedRoomCode);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        return roomCode.Trim().ToUpperInvariant();
    }
}

