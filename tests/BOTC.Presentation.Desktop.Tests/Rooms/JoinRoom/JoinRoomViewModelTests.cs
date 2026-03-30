using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.JoinRoom;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;

namespace BOTC.Presentation.Desktop.Tests.Rooms.JoinRoom;

public sealed class JoinRoomViewModelTests
{
    [Fact]
    public async Task JoinRoom_WhenDisplayNameIsMissing_ShowsErrorBannerState()
    {
        var apiClient = new FakeRoomsApiClient();
        var sessionService = new FakeClientSessionService();
        var navigationService = new FakeNavigationService();
        var viewModel = new JoinRoomViewModel(apiClient, sessionService, navigationService)
        {
            RoomCode = "AB12CD"
        };

        await viewModel.JoinRoomCommand.ExecuteAsync(null);

        Assert.Equal("Display name is required.", viewModel.ScreenMessage);
        Assert.Equal(ScreenMessageKind.Error, viewModel.ScreenMessageKind);
        Assert.True(viewModel.HasScreenMessage);
    }

    [Fact]
    public void BackToCreateRoom_WhenBusy_IsDisabled()
    {
        var viewModel = new JoinRoomViewModel(new FakeRoomsApiClient(), new FakeClientSessionService(), new FakeNavigationService())
        {
            IsBusy = true
        };

        Assert.False(viewModel.BackToCreateRoomCommand.CanExecute(null));
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

        public Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public void NavigateToCreateRoom()
        {
        }

        public void NavigateToJoinRoom()
        {
        }

        public Task NavigateToRoomLobbyAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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

