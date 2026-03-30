using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public partial class RoomLobbyViewModel(
    IRoomsApiClient roomsApiClient,
    IRoomLobbyRealtimeClient roomLobbyRealtimeClient,
    IClientSessionService clientSessionService,
    INavigationService navigationService) : ObservableObject
{
    private const string UnknownValue = "-";

    private readonly SynchronizationContext? _capturedSynchronizationContext = SynchronizationContext.Current;
    private string _currentRoomCode = string.Empty;
    private string _currentPlayerId = string.Empty;
    private string _subscribedRoomCode = string.Empty;
    private bool _hasLobbyData;
    private bool _isLobbyActive;
    private bool _isRealtimeHandlerAttached;
    private bool _refreshRequested;
    private bool _isRefreshLoopRunning;

    public ObservableCollection<LobbyPlayerItemViewModel> Players { get; } = new();

    [ObservableProperty]
    private string _roomCode = UnknownValue;

    [ObservableProperty]
    private string _roomStatus = UnknownValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorMessage))]
    [NotifyPropertyChangedFor(nameof(HasScreenMessage))]
    private string _screenMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _screenMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    private bool _isLeaving;

    public string ErrorMessage => ScreenMessage;

    public bool HasScreenMessage => !string.IsNullOrWhiteSpace(ScreenMessage);

    public bool HasLobbyData => _hasLobbyData;

    public bool IsBusy => IsLoading || IsRefreshing || IsLeaving;

    public string BusyText => IsLeaving
        ? "Leaving room..."
        : IsLoading
            ? "Loading room lobby..."
            : IsRefreshing
                ? "Refreshing room lobby..."
                : string.Empty;

    public bool HasRealtimeSubscription => !string.IsNullOrWhiteSpace(_subscribedRoomCode);

    public string RealtimeStateText => IsRefreshing
        ? "Refreshing lobby"
        : HasRealtimeSubscription
            ? "Realtime updates active"
            : "Realtime updates idle";

    private bool CanBackToCreateRoom() => !IsLoading && !IsRefreshing && !IsLeaving;

    [RelayCommand(CanExecute = nameof(CanBackToCreateRoom), AllowConcurrentExecutions = false)]
    private async Task BackToCreateRoomAsync()
    {
        await ExitLobbyAsync(CancellationToken.None);
        navigationService.NavigateToCreateRoom();
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!TrySetSessionContext())
        {
            clientSessionService.ClearSession();
            await ExitLobbyAsync(CancellationToken.None);
            ResetLobbyState("Session is invalid. Join or create a room again.", ScreenMessageKind.Error);
            navigationService.NavigateToCreateRoom();
            return;
        }

        RoomCode = _currentRoomCode;
        await EnsureRealtimeSubscriptionAsync(cancellationToken);
        await QueueLobbyRefreshAsync(cancellationToken);
    }

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = true;
        AttachRealtimeHandler();

        if (!TrySetSessionContext())
        {
            await ExitLobbyAsync(CancellationToken.None);
            ResetLobbyState("No active session found. Join or create a room first.", ScreenMessageKind.Error);
            navigationService.NavigateToCreateRoom();
            return;
        }

        try
        {
            await EnsureRealtimeSubscriptionAsync(cancellationToken);
        }
        catch
        {
            // Realtime is best-effort; HTTP remains the source of truth for lobby state.
        }
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        await ExitLobbyAsync(cancellationToken);
    }

    private bool CanRefresh() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !IsLoading
        && !IsRefreshing
        && !IsLeaving;

    [RelayCommand(CanExecute = nameof(CanRefresh), AllowConcurrentExecutions = false)]
    private Task RefreshAsync()
    {
        return QueueLobbyRefreshAsync(CancellationToken.None);
    }

    private bool CanLeaveRoom() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !string.IsNullOrWhiteSpace(_currentPlayerId)
        && !IsLoading
        && !IsRefreshing
        && !IsLeaving;

    [RelayCommand(CanExecute = nameof(CanLeaveRoom), AllowConcurrentExecutions = false)]
    private async Task LeaveRoomAsync()
    {
        ClearScreenMessage();
        var currentRoomCode = NormalizeRoomCode(clientSessionService.CurrentRoomCode ?? string.Empty);
        var currentPlayerId = NormalizePlayerId(clientSessionService.CurrentPlayerId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(currentRoomCode) || string.IsNullOrWhiteSpace(currentPlayerId))
        {
            ShowErrorMessage("No active session found. Join or create a room first.");
            clientSessionService.ClearSession();
            await ExitLobbyAsync(CancellationToken.None);
            navigationService.NavigateToCreateRoom();
            return;
        }

        _currentRoomCode = currentRoomCode;
        _currentPlayerId = currentPlayerId;
        IsLeaving = true;

        try
        {
            var response = await roomsApiClient.LeaveRoomAsync(
                currentRoomCode,
                new LeaveRoomRequest(currentPlayerId),
                CancellationToken.None);

            await ExitLobbyAsync(CancellationToken.None);
            clientSessionService.ClearSession();
            ResetLobbyState(response.RoomWasRemoved
                ? "The room was closed."
                : "You left the room.", ScreenMessageKind.Info);
            navigationService.NavigateToCreateRoom();
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ShowErrorMessage("Leave request was invalid.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            await ExitLobbyAsync(CancellationToken.None);
            clientSessionService.ClearSession();
            navigationService.NavigateToCreateRoom();
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ShowErrorMessage("Unable to leave the room due to a conflicting room state. Please refresh and try again.");
        }
        catch (HttpRequestException)
        {
            ShowErrorMessage("Unable to contact the server. Please try again.");
        }
        catch (Exception)
        {
            ShowErrorMessage("Unexpected error occurred while leaving the room.");
        }
        finally
        {
            IsLeaving = false;
        }
    }

    private Task HandleLobbyUpdatedAsync(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        if (!_isLobbyActive || !string.Equals(normalizedRoomCode, _currentRoomCode, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return QueueLobbyRefreshAsync(CancellationToken.None);
    }

    private Task HandleLobbyClosedAsync(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        if (!_isLobbyActive || !string.Equals(normalizedRoomCode, _currentRoomCode, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return RunOnCapturedContextAsync(async () =>
        {
            await ExitLobbyAsync(CancellationToken.None);
            clientSessionService.ClearSession();
            ResetLobbyState("The room was closed.", ScreenMessageKind.Info);
            navigationService.NavigateToCreateRoom();
        });
    }

    private async Task QueueLobbyRefreshAsync(CancellationToken cancellationToken)
    {
        await RunOnCapturedContextAsync(async () =>
        {
            _refreshRequested = true;
            if (_isRefreshLoopRunning)
            {
                return;
            }

            _isRefreshLoopRunning = true;
            try
            {
                while (_refreshRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _refreshRequested = false;
                    await ExecuteLobbyRefreshAsync(cancellationToken);
                }
            }
            finally
            {
                _isRefreshLoopRunning = false;
            }
        });
    }

    private async Task ExecuteLobbyRefreshAsync(CancellationToken cancellationToken)
    {
        ClearScreenMessage();

        var preserveCurrentLobbyState = _hasLobbyData;
        SetBusyState(preserveCurrentLobbyState);

        try
        {
            var response = await roomsApiClient.GetRoomLobbyAsync(_currentRoomCode, cancellationToken);
            ApplyLobbyState(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            ShowErrorMessage(BuildLobbyErrorMessage(exception.StatusCode, preserveCurrentLobbyState));
        }
        catch (Exception)
        {
            ShowErrorMessage(preserveCurrentLobbyState
                ? "Couldn't refresh the lobby. Showing the last loaded data."
                : "Unexpected error occurred while loading the room lobby.");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private async Task EnsureRealtimeSubscriptionAsync(CancellationToken cancellationToken)
    {
        if (!_isLobbyActive || string.IsNullOrWhiteSpace(_currentRoomCode) || IsLeaving)
        {
            return;
        }

        if (string.Equals(_subscribedRoomCode, _currentRoomCode, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_subscribedRoomCode))
        {
            await roomLobbyRealtimeClient.UnsubscribeAsync(_subscribedRoomCode, cancellationToken);
        }

        await roomLobbyRealtimeClient.SubscribeAsync(_currentRoomCode, cancellationToken);
        _subscribedRoomCode = _currentRoomCode;
        NotifyRealtimeStateChanged();
    }

    private async Task ExitLobbyAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = false;
        _refreshRequested = false;
        DetachRealtimeHandler();

        try
        {
            await StopRealtimeUpdatesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort cleanup to avoid leaving background connections running.
        }
    }

    private async Task StopRealtimeUpdatesAsync(CancellationToken cancellationToken)
    {
        await roomLobbyRealtimeClient.UnsubscribeAsync(_subscribedRoomCode, cancellationToken);
        _subscribedRoomCode = string.Empty;
        NotifyRealtimeStateChanged();
    }

    private bool TrySetSessionContext()
    {
        _currentRoomCode = NormalizeRoomCode(clientSessionService.CurrentRoomCode ?? string.Empty);
        _currentPlayerId = NormalizePlayerId(clientSessionService.CurrentPlayerId ?? string.Empty);

        RefreshCommand.NotifyCanExecuteChanged();
        LeaveRoomCommand.NotifyCanExecuteChanged();

        return !string.IsNullOrWhiteSpace(_currentRoomCode)
            && !string.IsNullOrWhiteSpace(_currentPlayerId);
    }

    private void AttachRealtimeHandler()
    {
        if (_isRealtimeHandlerAttached)
        {
            return;
        }

        roomLobbyRealtimeClient.LobbyUpdated += HandleLobbyUpdatedAsync;
        roomLobbyRealtimeClient.LobbyClosed += HandleLobbyClosedAsync;
        _isRealtimeHandlerAttached = true;
    }

    private void DetachRealtimeHandler()
    {
        if (!_isRealtimeHandlerAttached)
        {
            return;
        }

        roomLobbyRealtimeClient.LobbyUpdated -= HandleLobbyUpdatedAsync;
        roomLobbyRealtimeClient.LobbyClosed -= HandleLobbyClosedAsync;
        _isRealtimeHandlerAttached = false;
    }

    private async Task RunOnCapturedContextAsync(Func<Task> callback)
    {
        if (_capturedSynchronizationContext is null || SynchronizationContext.Current == _capturedSynchronizationContext)
        {
            await callback();
            return;
        }

        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _capturedSynchronizationContext.Post(static state =>
        {
            var callbackState = (PostedCallbackState)state!;
            _ = callbackState.ExecuteAsync();
        }, new PostedCallbackState(callback, completionSource));

        await completionSource.Task;
    }

    private sealed class PostedCallbackState(Func<Task> callback, TaskCompletionSource<object?> completionSource)
    {
        public async Task ExecuteAsync()
        {
            try
            {
                await callback();
                completionSource.TrySetResult(null);
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
        }
    }

    private void ApplyLobbyState(GetRoomLobbyResponse response)
    {
        _currentRoomCode = NormalizeRoomCode(response.RoomCode);
        RoomCode = string.IsNullOrWhiteSpace(_currentRoomCode) ? UnknownValue : _currentRoomCode;
        RoomStatus = ToDisplayStatus(response.Status);

        Players.Clear();
        foreach (var player in response.Players)
        {
            Players.Add(new LobbyPlayerItemViewModel(player.DisplayName, player.IsHost));
        }

        SetHasLobbyData(true);
        RefreshCommand.NotifyCanExecuteChanged();
    }

    private void SetBusyState(bool preserveCurrentLobbyState)
    {
        IsLoading = !preserveCurrentLobbyState;
        IsRefreshing = preserveCurrentLobbyState;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        _ = value;
        NotifyBusyPresentationStateChanged();
    }

    partial void OnIsRefreshingChanged(bool value)
    {
        _ = value;
        NotifyBusyPresentationStateChanged();
        NotifyRealtimeStateChanged();
    }

    partial void OnIsLeavingChanged(bool value)
    {
        _ = value;
        NotifyBusyPresentationStateChanged();
    }

    private void ClearScreenMessage()
    {
        ScreenMessage = string.Empty;
        ScreenMessageKind = ScreenMessageKind.None;
    }

    private void ShowErrorMessage(string message)
    {
        ShowScreenMessage(message, ScreenMessageKind.Error);
    }

    private void ShowScreenMessage(string message, ScreenMessageKind kind)
    {
        ScreenMessage = message;
        ScreenMessageKind = kind;
    }

    private void NotifyBusyPresentationStateChanged()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(BusyText));
    }

    private void NotifyRealtimeStateChanged()
    {
        OnPropertyChanged(nameof(HasRealtimeSubscription));
        OnPropertyChanged(nameof(RealtimeStateText));
    }

    private void SetHasLobbyData(bool value)
    {
        if (_hasLobbyData == value)
        {
            return;
        }

        _hasLobbyData = value;
        OnPropertyChanged(nameof(HasLobbyData));
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        return string.IsNullOrWhiteSpace(roomCode)
            ? string.Empty
            : roomCode.Trim().ToUpperInvariant();
    }

    private static string NormalizePlayerId(string playerId)
    {
        return string.IsNullOrWhiteSpace(playerId)
            ? string.Empty
            : playerId.Trim();
    }

    private void ResetLobbyState(string message, ScreenMessageKind messageKind)
    {
        Players.Clear();
        RoomCode = UnknownValue;
        RoomStatus = UnknownValue;
        ShowScreenMessage(message, messageKind);
        IsLoading = false;
        IsRefreshing = false;
        IsLeaving = false;
        SetHasLobbyData(false);
        _currentRoomCode = string.Empty;
        _currentPlayerId = string.Empty;
        RefreshCommand.NotifyCanExecuteChanged();
        LeaveRoomCommand.NotifyCanExecuteChanged();
    }


    private static string BuildLobbyErrorMessage(HttpStatusCode? statusCode, bool preserveCurrentLobbyState)
    {
        if (preserveCurrentLobbyState)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "Room was not found. Showing the last loaded data.",
                HttpStatusCode.BadRequest => "Room code is invalid. Showing the last loaded data.",
                _ => "Couldn't refresh the lobby. Showing the last loaded data."
            };
        }

        return statusCode switch
        {
            HttpStatusCode.NotFound => "Room was not found.",
            HttpStatusCode.BadRequest => "Enter a valid room code.",
            _ => "Unable to load the room lobby. Please try again."
        };
    }

    private static string ToDisplayStatus(RoomStatusContract status)
    {
        return status switch
        {
            RoomStatusContract.WaitingForPlayers => "Waiting for players",
            _ => "Unknown"
        };
    }
}
