using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;

namespace BOTC.Presentation.Desktop.Tests.Rooms.CreateRoom;

public sealed class CreateRoomViewModelTests
{
    [Fact]
    public async Task CreateRoom_WhenHostDisplayNameIsMissing_ShowsErrorBannerState()
    {
        var apiClient = new FakeRoomsApiClient();
        var sessionService = new FakeClientSessionService();
        var navigationService = new FakeNavigationService();
        var viewModel = new CreateRoomViewModel(apiClient, sessionService, navigationService);

        await viewModel.CreateRoomCommand.ExecuteAsync(null);

        Assert.Equal("Host display name is required.", viewModel.ScreenMessage);
        Assert.Equal(ScreenMessageKind.Error, viewModel.ScreenMessageKind);
        Assert.True(viewModel.HasScreenMessage);
        Assert.Equal(0, navigationService.NavigateToJoinRoomCallCount);
    }

    [Fact]
    public void NavigateToJoinRoom_WhenBusy_IsDisabled()
    {
        var viewModel = new CreateRoomViewModel(new FakeRoomsApiClient(), new FakeClientSessionService(), new FakeNavigationService())
        {
            IsBusy = true
        };

        Assert.False(viewModel.NavigateToJoinRoomCommand.CanExecute(null));
    }

    private sealed class FakeRoomsApiClient : IRoomsApiClient
    {
        public Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<JoinRoomResponse> JoinRoomAsync(string roomCode, JoinRoomRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<LeaveRoomResponse> LeaveRoomAsync(string roomCode, LeaveRoomRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SetPlayerReadyResponse> SetPlayerReadyAsync(string roomCode, SetPlayerReadyRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StartGameResponse> StartGameAsync(string roomCode, StartGameRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public int NavigateToJoinRoomCallCount { get; private set; }

        public void NavigateToEntry()
        {
        }

        public void NavigateToCreateRoom()
        {
        }

        public void NavigateToJoinRoom()
        {
            NavigateToJoinRoomCallCount++;
        }

        public void NavigateToRoomLobby()
        {
        }
    }

    private sealed class FakeClientSessionService : IClientSessionService
    {
        public string? CurrentRoomCode { get; private set; }

        public string? CurrentPlayerId { get; private set; }

        public string? DisplayName { get; private set; }

        public bool HasActiveSession =>
            !string.IsNullOrWhiteSpace(CurrentRoomCode) &&
            !string.IsNullOrWhiteSpace(CurrentPlayerId);

        public void SetDisplayName(string displayName)
        {
            DisplayName = displayName;
        }

        public void SetSession(string roomCode, string playerId, string displayName)
        {
            CurrentRoomCode = roomCode;
            CurrentPlayerId = playerId;
            DisplayName = displayName;
        }

        public void ClearSession()
        {
            CurrentRoomCode = null;
            CurrentPlayerId = null;
            DisplayName = null;
        }
    }
}

