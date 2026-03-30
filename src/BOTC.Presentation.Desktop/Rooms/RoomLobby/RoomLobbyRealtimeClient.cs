﻿using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR.Client;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public sealed class RoomLobbyRealtimeClient : IRoomLobbyRealtimeClient, IDisposable
{
    private readonly HubConnection _hubConnection;
    private string? _subscribedRoomCode;
    private bool _isDisposed;

    public RoomLobbyRealtimeClient(Uri roomsApiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(roomsApiBaseAddress);

        var hubUri = new Uri(roomsApiBaseAddress, RoomLobbyHubContract.HubRoute);
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>(RoomLobbyHubContract.LobbyUpdatedEvent, OnLobbyUpdatedAsync);
        _hubConnection.On<string>(RoomLobbyHubContract.LobbyClosedEvent, OnLobbyClosedAsync);
        _hubConnection.Reconnected += OnReconnectedAsync;
    }

    public event Func<string, Task>? LobbyUpdated;

    public event Func<string, Task>? LobbyClosed;

    public async Task SubscribeAsync(string roomCode, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var previousRoomCode = _subscribedRoomCode;
        _subscribedRoomCode = normalizedRoomCode;

        await EnsureStartedAsync(cancellationToken);

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(previousRoomCode)
            && !string.Equals(previousRoomCode, normalizedRoomCode, StringComparison.Ordinal))
        {
            await _hubConnection.InvokeAsync(RoomLobbyHubContract.LeaveLobbyGroupMethod, previousRoomCode, cancellationToken);
        }

        await _hubConnection.InvokeAsync(RoomLobbyHubContract.JoinLobbyGroupMethod, normalizedRoomCode, cancellationToken);
    }

    public async Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var roomCodeToLeave = string.IsNullOrWhiteSpace(roomCode)
            ? _subscribedRoomCode
            : NormalizeRoomCode(roomCode);

        _subscribedRoomCode = null;

        if (_hubConnection.State == HubConnectionState.Connected && !string.IsNullOrWhiteSpace(roomCodeToLeave))
        {
            await _hubConnection.InvokeAsync(RoomLobbyHubContract.LeaveLobbyGroupMethod, roomCodeToLeave, cancellationToken);
        }

        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _subscribedRoomCode = null;
        _hubConnection.Reconnected -= OnReconnectedAsync;
        await _hubConnection.DisposeAsync();
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        await _hubConnection.StartAsync(cancellationToken);
    }

    private async Task OnLobbyUpdatedAsync(string roomCode)
    {
        await RaiseAsync(LobbyUpdated, roomCode);
    }

    private async Task OnLobbyClosedAsync(string roomCode)
    {
        await RaiseAsync(LobbyClosed, roomCode);
    }

    private async Task OnReconnectedAsync(string? _)
    {
        if (string.IsNullOrWhiteSpace(_subscribedRoomCode))
        {
            return;
        }

        await _hubConnection.InvokeAsync(RoomLobbyHubContract.JoinLobbyGroupMethod, _subscribedRoomCode);
        await OnLobbyUpdatedAsync(_subscribedRoomCode);
    }

    private static async Task RaiseAsync(Func<string, Task>? handler, string roomCode)
    {
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        return roomCode.Trim().ToUpperInvariant();
    }
}

