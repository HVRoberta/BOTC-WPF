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
    private readonly SynchronizationContext? _capturedSynchronizationContext = SynchronizationContext.Current;
    private readonly RoomLobbySnapshotState _snapshotState = new();
    private string _currentRoomCode = string.Empty;
    private string _currentPlayerId = string.Empty;
    private string _subscribedRoomCode = string.Empty;
    private bool _isInitialized;
    private bool _isLobbyActive;
    private bool _isRealtimeHandlerAttached;
    private bool _refreshRequested;
    private bool _refreshShouldPreserveScreenMessage;
    private bool _isRefreshLoopRunning;
    private RealtimeConnectionState _realtimeConnectionState = RealtimeConnectionState.Disconnected;

    public ObservableCollection<LobbyPlayerItemViewModel> Players => _snapshotState.Players;

    public string RoomCode => _snapshotState.RoomCode;

    public string RoomStatus => _snapshotState.RoomStatus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreenMessage))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    [NotifyPropertyChangedFor(nameof(ShowLastSuccessfulLobbyHint))]
    private string _screenMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _screenMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRealtimeMessage))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private string _realtimeMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _realtimeMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private bool _isLeaving;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private bool _isTogglingReady;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(LeaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    [NotifyPropertyChangedFor(nameof(HasAnyStatusUpdate))]
    private bool _isStartingGame;


    public bool HasScreenMessage => !string.IsNullOrWhiteSpace(ScreenMessage);

    public bool HasRealtimeMessage => !string.IsNullOrWhiteSpace(RealtimeMessage);

    public bool HasAnyStatusUpdate => HasScreenMessage || HasRealtimeMessage || IsBusy;

    public bool HasLobbyData => _snapshotState.HasLobbyData;

    public bool ShowLastSuccessfulLobbyHint => HasLobbyData && HasScreenMessage;

    public string CurrentUsername => _snapshotState.CurrentUsername;

    public string CurrentUserRole => _snapshotState.CurrentUserRole;

    public string HostName => _snapshotState.HostName;

    public bool IsCurrentUserHost => _snapshotState.IsCurrentUserHost;

    public bool IsCurrentUserReady => _snapshotState.IsCurrentUserReady;

    public string CurrentUserReadyStateText => _snapshotState.CurrentUserReadyStateSummary;

    public string NonHostReadinessSummary => _snapshotState.NonHostReadinessSummary;

    public bool CanStartGame => CanStartGameCore();

    public string StartEligibilityHint => BuildStartEligibilityHint();

    public string PlayerCountSummary => _snapshotState.PlayerCountSummary;

    public string LastSuccessfulRefreshText => _snapshotState.LastSuccessfulRefreshText;

    public bool IsBusy => IsLoading || IsRefreshing || IsLeaving || IsTogglingReady || IsStartingGame;

    public string BusyText => IsLeaving
        ? "Leaving room..."
        : IsStartingGame
            ? "Starting game..."
            : IsTogglingReady
                ? "Updating readiness..."
        : IsLoading
            ? "Loading room lobby..."
            : IsRefreshing
                ? "Refreshing room lobby..."
                : string.Empty;

    public bool HasRealtimeSubscription => !string.IsNullOrWhiteSpace(_subscribedRoomCode);

    public RealtimeConnectionState RealtimeConnectionState => _realtimeConnectionState;

    public string RealtimeStateText => IsRefreshing
        ? "Refreshing"
        : _realtimeConnectionState switch
        {
            RealtimeConnectionState.Connected => "Connected",
            RealtimeConnectionState.Reconnecting => "Reconnecting",
            RealtimeConnectionState.Connecting => "Connecting",
            _ => "Disconnected"
        };

    private bool CanBackToCreateRoom() =>
        !IsLoading
        && !IsRefreshing
        && !IsLeaving
        && !IsTogglingReady
        && !IsStartingGame;

    [RelayCommand(CanExecute = nameof(CanBackToCreateRoom), AllowConcurrentExecutions = false)]
    private async Task BackToCreateRoomAsync()
    {
        await ExitLobbyAsync(CancellationToken.None);
        navigationService.NavigateToCreateRoom();
    }

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = true;
        AttachRealtimeHandler();

        if (!_isInitialized)
        {
            await ActivateLobbyForInitialVisitAsync(cancellationToken);
        }
        else
        {
            await ActivateLobbyForSubsequentVisitAsync(cancellationToken);
        }
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        // RoomLobbyViewModel is transient, so _isInitialized will naturally be false on
        // the next navigation. Resetting explicitly keeps this method correct if the
        // lifetime ever changes to scoped or singleton.
        _isInitialized = false;
        await ExitLobbyAsync(cancellationToken);
    }

    private async Task ActivateLobbyForInitialVisitAsync(CancellationToken cancellationToken)
    {
        _isInitialized = true;

        if (!TrySetSessionContext())
        {
            await HandleInvalidInitialSessionAsync();
            return;
        }

        _snapshotState.SetRoomCode(_currentRoomCode);
        OnPropertyChanged(nameof(RoomCode));
        await TryEnsureRealtimeSubscriptionAsync(cancellationToken);
        await QueueLobbyRefreshAsync(cancellationToken);
    }

    private async Task ActivateLobbyForSubsequentVisitAsync(CancellationToken cancellationToken)
    {
        if (!TrySetSessionContext())
        {
            await HandleMissingActiveSessionAsync();
            return;
        }

        await TryEnsureRealtimeSubscriptionAsync(cancellationToken);
    }

    private async Task HandleInvalidInitialSessionAsync()
    {
        clientSessionService.ClearSession();
        await ExitLobbyAsync(CancellationToken.None);
        ResetLobbyState("Session is invalid. Join or create a room again.", ScreenMessageKind.Error);
        navigationService.NavigateToCreateRoom();
    }

    private async Task HandleMissingActiveSessionAsync()
    {
        await ExitLobbyAsync(CancellationToken.None);
        ResetLobbyState("No active session found. Join or create a room first.", ScreenMessageKind.Error);
        navigationService.NavigateToCreateRoom();
    }

    private bool CanRefresh() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !IsLoading
        && !IsRefreshing
        && !IsLeaving
        && !IsTogglingReady
        && !IsStartingGame;

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
        && !IsLeaving
        && !IsTogglingReady
        && !IsStartingGame;

    private bool CanToggleReady() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !string.IsNullOrWhiteSpace(_currentPlayerId)
        && _snapshotState.CanCurrentUserToggleReady
        && !IsLoading
        && !IsRefreshing
        && !IsLeaving
        && !IsTogglingReady
        && !IsStartingGame;

    [RelayCommand(CanExecute = nameof(CanToggleReady), AllowConcurrentExecutions = false)]
    private async Task ToggleReadyAsync()
    {
        ClearScreenMessage();
        if (!TryGetReadySessionContext(out var currentRoomCode, out var currentPlayerId))
        {
            ShowErrorMessage("No active session found. Join or create a room first.");
            return;
        }

        if (_snapshotState.IsCurrentUserHost)
        {
            ShowScreenMessage("Host does not need to toggle ready state.", ScreenMessageKind.Info);
            return;
        }

        var desiredReadyState = !IsCurrentUserReady;
        IsTogglingReady = true;

        try
        {
            var response = await roomsApiClient.SetPlayerReadyAsync(
                currentRoomCode,
                new SetPlayerReadyRequest(currentPlayerId, desiredReadyState),
                CancellationToken.None);

            ShowScreenMessage(
                response.IsReady
                    ? "You are marked as ready."
                    : "You are marked as not ready.",
                ScreenMessageKind.Info);

            await QueueLobbyRefreshAsync(CancellationToken.None, preserveScreenMessage: true);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ShowErrorMessage("Ready request was invalid.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            ShowErrorMessage("Unable to update readiness because the room or player was not found.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ShowErrorMessage("Unable to update readiness due to a conflicting room state. Please refresh and try again.");
        }
        catch (HttpRequestException)
        {
            ShowErrorMessage("Unable to contact the server. Please try again.");
        }
        catch (Exception)
        {
            ShowErrorMessage("Unexpected error occurred while updating readiness.");
        }
        finally
        {
            IsTogglingReady = false;
        }
    }

    private bool CanStartGameCore() =>
        _snapshotState.IsEligibleToStart
        && !IsLoading
        && !IsRefreshing
        && !IsLeaving
        && !IsTogglingReady
        && !IsStartingGame;

    [RelayCommand(CanExecute = nameof(CanStartGameCore), AllowConcurrentExecutions = false)]
    private async Task StartGameAsync()
    {
        ClearScreenMessage();
        if (!TryGetReadySessionContext(out var currentRoomCode, out var currentPlayerId))
        {
            ShowErrorMessage("No active session found. Join or create a room first.");
            return;
        }

        IsStartingGame = true;

        try
        {
            var response = await roomsApiClient.StartGameAsync(
                currentRoomCode,
                new StartGameRequest(currentPlayerId),
                CancellationToken.None);

            if (response.IsStarted)
            {
                ShowScreenMessage("Game started successfully.", ScreenMessageKind.Info);
                await QueueLobbyRefreshAsync(CancellationToken.None, preserveScreenMessage: true);
                return;
            }

            ShowErrorMessage(BuildStartGameBlockedMessage(response.BlockedReason));
            await QueueLobbyRefreshAsync(CancellationToken.None, preserveScreenMessage: true);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ShowErrorMessage("Start game request was invalid.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            ShowErrorMessage("Unable to start game because the room was not found.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ShowErrorMessage("Unable to start game due to a conflicting room state. Please refresh and try again.");
        }
        catch (HttpRequestException)
        {
            ShowErrorMessage("Unable to contact the server. Please try again.");
        }
        catch (Exception)
        {
            ShowErrorMessage("Unexpected error occurred while starting the game.");
        }
        finally
        {
            IsStartingGame = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLeaveRoom), AllowConcurrentExecutions = false)]
    private async Task LeaveRoomAsync()
    {
        ClearScreenMessage();
        if (!TryGetLeaveSessionContext(out var currentRoomCode, out var currentPlayerId))
        {
            await HandleMissingSessionBeforeLeaveAsync();
            return;
        }

        IsLeaving = true;

        try
        {
            var response = await LeaveCurrentRoomAsync(currentRoomCode, currentPlayerId);
            await CompleteSuccessfulLeaveAsync(response, currentRoomCode);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ShowErrorMessage("Leave request was invalid.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            await HandleLeaveRoomNotFoundAsync();
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

    private bool TryGetLeaveSessionContext(out string roomCode, out string playerId)
    {
        roomCode = NormalizeRoomCode(clientSessionService.CurrentRoomCode ?? string.Empty);
        playerId = NormalizePlayerId(clientSessionService.CurrentPlayerId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(roomCode) || string.IsNullOrWhiteSpace(playerId))
        {
            return false;
        }

        _currentRoomCode = roomCode;
        _currentPlayerId = playerId;
        return true;
    }

    private bool TryGetReadySessionContext(out string roomCode, out string playerId)
    {
        roomCode = NormalizeRoomCode(_currentRoomCode);
        playerId = NormalizePlayerId(_currentPlayerId);
        return !string.IsNullOrWhiteSpace(roomCode)
            && !string.IsNullOrWhiteSpace(playerId);
    }

    private async Task HandleMissingSessionBeforeLeaveAsync()
    {
        ShowErrorMessage("No active session found. Join or create a room first.");
        clientSessionService.ClearSession();
        await ExitLobbyAsync(CancellationToken.None);
        navigationService.NavigateToCreateRoom();
    }

    private Task<LeaveRoomResponse> LeaveCurrentRoomAsync(string roomCode, string playerId)
    {
        return roomsApiClient.LeaveRoomAsync(
            roomCode,
            new LeaveRoomRequest(playerId),
            CancellationToken.None);
    }

    private async Task CompleteSuccessfulLeaveAsync(LeaveRoomResponse response, string roomCode)
    {
        await ExitLobbyAsync(CancellationToken.None);
        clientSessionService.ClearSession();
        ResetLobbyState(response.RoomWasRemoved
            ? "The room was closed while you were leaving. Returned to room setup."
            : $"You left room {roomCode} successfully.", ScreenMessageKind.Info);
        navigationService.NavigateToCreateRoom();
    }

    private async Task HandleLeaveRoomNotFoundAsync()
    {
        await ExitLobbyAsync(CancellationToken.None);
        clientSessionService.ClearSession();
        navigationService.NavigateToCreateRoom();
    }

    private Task HandleLobbyUpdatedAsync(string roomCode)
    {
        if (!IsLobbyEventForCurrentRoom(roomCode))
        {
            return Task.CompletedTask;
        }

        return QueueLobbyRefreshAsync(CancellationToken.None);
    }

    private Task HandleLobbyClosedAsync(string roomCode)
    {
        if (!IsLobbyEventForCurrentRoom(roomCode))
        {
            return Task.CompletedTask;
        }

        return RunOnCapturedContextAsync(async () =>
        {
            await HandleLobbyClosedForCurrentRoomAsync();
        });
    }

    private bool IsLobbyEventForCurrentRoom(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        return _isLobbyActive
            && string.Equals(normalizedRoomCode, _currentRoomCode, StringComparison.Ordinal);
    }

    private async Task HandleLobbyClosedForCurrentRoomAsync()
    {
        await ExitLobbyAsync(CancellationToken.None);
        clientSessionService.ClearSession();
        ResetLobbyState($"The room {_currentRoomCode} was closed by the host. Returned to room setup.", ScreenMessageKind.Info);
        navigationService.NavigateToCreateRoom();
    }

    private async Task QueueLobbyRefreshAsync(CancellationToken cancellationToken, bool preserveScreenMessage = false)
    {
        await RunOnCapturedContextAsync(() => ProcessQueuedLobbyRefreshAsync(cancellationToken, preserveScreenMessage));
    }

    private async Task ProcessQueuedLobbyRefreshAsync(CancellationToken cancellationToken, bool preserveScreenMessage)
    {
        RequestLobbyRefresh(preserveScreenMessage);
        if (_isRefreshLoopRunning)
        {
            return;
        }

        _isRefreshLoopRunning = true;
        try
        {
            while (TryDequeueLobbyRefreshRequest(cancellationToken, out var shouldPreserveScreenMessage))
            {
                await ExecuteLobbyRefreshAsync(cancellationToken, shouldPreserveScreenMessage);
            }
        }
        finally
        {
            _isRefreshLoopRunning = false;
        }
    }

    private void RequestLobbyRefresh(bool preserveScreenMessage)
    {
        _refreshRequested = true;
        _refreshShouldPreserveScreenMessage = _refreshShouldPreserveScreenMessage || preserveScreenMessage;
    }

    private bool TryDequeueLobbyRefreshRequest(CancellationToken cancellationToken, out bool preserveScreenMessage)
    {
        preserveScreenMessage = false;
        cancellationToken.ThrowIfCancellationRequested();
        if (!_refreshRequested)
        {
            return false;
        }

        _refreshRequested = false;
        preserveScreenMessage = _refreshShouldPreserveScreenMessage;
        _refreshShouldPreserveScreenMessage = false;
        return true;
    }

    private async Task ExecuteLobbyRefreshAsync(CancellationToken cancellationToken, bool preserveScreenMessage)
    {
        if (!preserveScreenMessage)
        {
            ClearScreenMessage();
        }

        var preserveCurrentLobbyState = HasLobbyData;
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
        if (!ShouldMaintainRealtimeSubscription())
        {
            return;
        }

        if (IsSubscribedToCurrentRoom())
        {
            return;
        }

        await ReplaceRealtimeSubscriptionAsync(cancellationToken);
    }

    private bool ShouldMaintainRealtimeSubscription()
    {
        return _isLobbyActive
            && !string.IsNullOrWhiteSpace(_currentRoomCode)
            && !IsLeaving;
    }

    private bool IsSubscribedToCurrentRoom()
    {
        return string.Equals(_subscribedRoomCode, _currentRoomCode, StringComparison.Ordinal);
    }

    private async Task ReplaceRealtimeSubscriptionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_subscribedRoomCode))
        {
            await roomLobbyRealtimeClient.UnsubscribeAsync(_subscribedRoomCode, cancellationToken);
        }

        await roomLobbyRealtimeClient.SubscribeAsync(_currentRoomCode, cancellationToken);
        _subscribedRoomCode = _currentRoomCode;
        ApplyRealtimeConnectionState(roomLobbyRealtimeClient.ConnectionState);
        NotifyRealtimeStateChanged();
    }

    private async Task TryEnsureRealtimeSubscriptionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureRealtimeSubscriptionAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            ApplyRealtimeConnectionState(RealtimeConnectionState.Disconnected);
            ShowRealtimeMessage(
                "Realtime updates are unavailable. You can continue with manual refresh.",
                ScreenMessageKind.Error);
        }
    }

    private async Task ExitLobbyAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = false;
        _refreshRequested = false;
        _refreshShouldPreserveScreenMessage = false;
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
        ApplyRealtimeConnectionState(RealtimeConnectionState.Disconnected);
        NotifyRealtimeStateChanged();
    }

    private bool TrySetSessionContext()
    {
        _currentRoomCode = NormalizeRoomCode(clientSessionService.CurrentRoomCode ?? string.Empty);
        _currentPlayerId = NormalizePlayerId(clientSessionService.CurrentPlayerId ?? string.Empty);
        _snapshotState.ApplySessionName(clientSessionService.Name ?? string.Empty);

        NotifyLobbyMetadataChanged();

        RefreshCommand.NotifyCanExecuteChanged();
        LeaveRoomCommand.NotifyCanExecuteChanged();
        ToggleReadyCommand.NotifyCanExecuteChanged();
        StartGameCommand.NotifyCanExecuteChanged();

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
        roomLobbyRealtimeClient.ConnectionStateChanged += HandleRealtimeConnectionStateChanged;
        ApplyRealtimeConnectionState(roomLobbyRealtimeClient.ConnectionState);
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
        roomLobbyRealtimeClient.ConnectionStateChanged -= HandleRealtimeConnectionStateChanged;
        _isRealtimeHandlerAttached = false;
    }

    private void HandleRealtimeConnectionStateChanged(RealtimeConnectionState state)
    {
        _ = RunOnCapturedContextAsync(() =>
        {
            ApplyRealtimeConnectionState(state);
            return Task.CompletedTask;
        });
    }

    private void ApplyRealtimeConnectionState(RealtimeConnectionState state)
    {
        _realtimeConnectionState = state;
        NotifyRealtimeStateChanged();

        if (!_isLobbyActive || IsLeaving)
        {
            ClearRealtimeMessage();
            return;
        }

        switch (state)
        {
            case RealtimeConnectionState.Connected:
                ClearRealtimeMessage();
                break;
            case RealtimeConnectionState.Reconnecting:
                ShowRealtimeMessage("Realtime connection was interrupted. Reconnecting automatically...", ScreenMessageKind.Info);
                break;
            case RealtimeConnectionState.Connecting:
                ShowRealtimeMessage("Connecting realtime updates...", ScreenMessageKind.Info);
                break;
            default:
                ShowRealtimeMessage(
                    "Realtime updates are disconnected. You can keep working and use Refresh to reload the lobby.",
                    ScreenMessageKind.Error);
                break;
        }
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
        _snapshotState.ApplyLobbySnapshot(response, _currentPlayerId);
        NotifyLobbySnapshotChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        ToggleReadyCommand.NotifyCanExecuteChanged();
        StartGameCommand.NotifyCanExecuteChanged();
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

    partial void OnIsTogglingReadyChanged(bool value)
    {
        _ = value;
        NotifyBusyPresentationStateChanged();
    }

    partial void OnIsStartingGameChanged(bool value)
    {
        _ = value;
        NotifyBusyPresentationStateChanged();
    }

    private void ClearScreenMessage()
    {
        ScreenMessage = string.Empty;
        ScreenMessageKind = ScreenMessageKind.None;
    }

    private void ClearRealtimeMessage()
    {
        RealtimeMessage = string.Empty;
        RealtimeMessageKind = ScreenMessageKind.None;
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

    private void ShowRealtimeMessage(string message, ScreenMessageKind kind)
    {
        RealtimeMessage = message;
        RealtimeMessageKind = kind;
    }

    private void NotifyBusyPresentationStateChanged()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(BusyText));
        OnPropertyChanged(nameof(CanStartGame));
    }

    private void NotifyRealtimeStateChanged()
    {
        OnPropertyChanged(nameof(HasRealtimeSubscription));
        OnPropertyChanged(nameof(RealtimeConnectionState));
        OnPropertyChanged(nameof(RealtimeStateText));
    }

    private void NotifyLobbyMetadataChanged()
    {
        OnPropertyChanged(nameof(CurrentUsername));
        OnPropertyChanged(nameof(CurrentUserRole));
        OnPropertyChanged(nameof(IsCurrentUserHost));
        OnPropertyChanged(nameof(IsCurrentUserReady));
        OnPropertyChanged(nameof(CurrentUserReadyStateText));
        OnPropertyChanged(nameof(HostName));
        OnPropertyChanged(nameof(NonHostReadinessSummary));
        OnPropertyChanged(nameof(CanStartGame));
        OnPropertyChanged(nameof(StartEligibilityHint));
        OnPropertyChanged(nameof(PlayerCountSummary));
        OnPropertyChanged(nameof(LastSuccessfulRefreshText));
    }

    private void NotifyLobbySnapshotChanged()
    {
        OnPropertyChanged(nameof(RoomCode));
        OnPropertyChanged(nameof(RoomStatus));
        OnPropertyChanged(nameof(HasLobbyData));
        OnPropertyChanged(nameof(ShowLastSuccessfulLobbyHint));
        NotifyLobbyMetadataChanged();
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
        _snapshotState.Reset();
        ShowScreenMessage(message, messageKind);
        ClearRealtimeMessage();
        IsLoading = false;
        IsRefreshing = false;
        IsLeaving = false;
        IsTogglingReady = false;
        IsStartingGame = false;
        _currentRoomCode = string.Empty;
        _currentPlayerId = string.Empty;
        _realtimeConnectionState = RealtimeConnectionState.Disconnected;
        NotifyLobbySnapshotChanged();
        NotifyRealtimeStateChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        LeaveRoomCommand.NotifyCanExecuteChanged();
        ToggleReadyCommand.NotifyCanExecuteChanged();
        StartGameCommand.NotifyCanExecuteChanged();
    }

    private string BuildStartEligibilityHint()
    {
        if (!_snapshotState.IsCurrentUserHost)
        {
            return "Only the host can start the game.";
        }

        if (!_snapshotState.IsWaitingForPlayers)
        {
            return "Game has already started.";
        }

        if (!_snapshotState.HasEnoughPlayersToStart)
        {
            return "At least 2 players are required to start.";
        }

        if (!_snapshotState.AreAllNonHostPlayersReady)
        {
            return "All non-host players must be ready before starting.";
        }

        return "Ready to start the game.";
    }

    private static string BuildStartGameBlockedMessage(StartGameBlockedReasonContract? blockedReason)
    {
        return blockedReason switch
        {
            StartGameBlockedReasonContract.StartedByNonHost => "Only the host can start the game.",
            StartGameBlockedReasonContract.RoomIsNotWaitingForPlayers => "Unable to start because the room is no longer waiting for players.",
            StartGameBlockedReasonContract.NotEnoughPlayers => "Unable to start because at least 2 players are required.",
            StartGameBlockedReasonContract.NonHostPlayersNotReady => "Unable to start because not all non-host players are ready.",
            StartGameBlockedReasonContract.StarterPlayerNotFound => "Unable to start because the starter player was not found.",
            _ => "Unable to start game right now. Please refresh and try again."
        };
    }

    private static string BuildLobbyErrorMessage(HttpStatusCode? statusCode, bool preserveCurrentLobbyState)
    {
        if (preserveCurrentLobbyState)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "Refresh failed because the room was not found. Showing the last loaded lobby snapshot.",
                HttpStatusCode.BadRequest => "Refresh failed because the room code is invalid. Showing the last loaded lobby snapshot.",
                _ => "Refresh failed. Showing the last loaded lobby snapshot."
            };
        }

        return statusCode switch
        {
            HttpStatusCode.NotFound => "Room was not found. It may have been closed.",
            HttpStatusCode.BadRequest => "Unable to load lobby because the room code is invalid.",
            _ => "Unable to load the room lobby. Please try again."
        };
    }

}
