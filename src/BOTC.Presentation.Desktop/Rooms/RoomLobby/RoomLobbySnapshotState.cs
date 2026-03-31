using System.Collections.ObjectModel;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

internal sealed class RoomLobbySnapshotState
{
    private const string UnknownValue = "-";

    private string _currentUserDisplayName = string.Empty;
    private string _currentUserRole = UnknownValue;
    private string _hostDisplayName = string.Empty;
    private DateTimeOffset? _lastSuccessfulRefreshAt;

    public ObservableCollection<LobbyPlayerItemViewModel> Players { get; } = new();

    public string RoomCode { get; private set; } = UnknownValue;

    public string RoomStatus { get; private set; } = UnknownValue;

    public bool HasLobbyData { get; private set; }

    public string CurrentUserDisplayName => string.IsNullOrWhiteSpace(_currentUserDisplayName) ? UnknownValue : _currentUserDisplayName;

    public string CurrentUserRole => _currentUserRole;

    public string HostDisplayName => string.IsNullOrWhiteSpace(_hostDisplayName) ? UnknownValue : _hostDisplayName;

    public string PlayerCountSummary => Players.Count == 1 ? "1 player in lobby" : $"{Players.Count} players in lobby";

    public string LastSuccessfulRefreshText => _lastSuccessfulRefreshAt is null
        ? "No successful refresh yet"
        : $"Last synced at {_lastSuccessfulRefreshAt.Value.ToLocalTime():HH:mm:ss}";

    public void SetRoomCode(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        RoomCode = string.IsNullOrWhiteSpace(normalizedRoomCode) ? UnknownValue : normalizedRoomCode;
    }

    public void ApplySessionDisplayName(string displayName)
    {
        _currentUserDisplayName = NormalizeDisplayName(displayName);
    }

    public void ApplyLobbySnapshot(GetRoomLobbyResponse response, string currentPlayerId)
    {
        var normalizedRoomCode = NormalizeRoomCode(response.RoomCode);
        RoomCode = string.IsNullOrWhiteSpace(normalizedRoomCode) ? UnknownValue : normalizedRoomCode;
        RoomStatus = ToDisplayStatus(response.Status);

        ApplyLobbyParticipantMetadata(response, currentPlayerId);
        ReplacePlayers(response.Players, currentPlayerId);

        _lastSuccessfulRefreshAt = DateTimeOffset.UtcNow;
        HasLobbyData = true;
    }

    public void Reset()
    {
        Players.Clear();
        RoomCode = UnknownValue;
        RoomStatus = UnknownValue;
        HasLobbyData = false;
        _currentUserDisplayName = string.Empty;
        _currentUserRole = UnknownValue;
        _hostDisplayName = string.Empty;
        _lastSuccessfulRefreshAt = null;
    }

    private void ApplyLobbyParticipantMetadata(GetRoomLobbyResponse response, string currentPlayerId)
    {
        var hostPlayer = response.Players.FirstOrDefault(player => player.IsHost);
        var currentPlayer = response.Players.FirstOrDefault(player => string.Equals(player.PlayerId, currentPlayerId, StringComparison.Ordinal));
        _hostDisplayName = NormalizeDisplayName(hostPlayer?.DisplayName ?? string.Empty);

        if (currentPlayer is not null)
        {
            _currentUserDisplayName = NormalizeDisplayName(currentPlayer.DisplayName);
            _currentUserRole = currentPlayer.IsHost ? "Host" : "Player";
            return;
        }

        _currentUserRole = UnknownValue;
    }

    private void ReplacePlayers(IReadOnlyList<LobbyPlayerResponse> players, string currentPlayerId)
    {
        Players.Clear();
        for (var index = 0; index < players.Count; index++)
        {
            var player = players[index];
            Players.Add(new LobbyPlayerItemViewModel(
                player.PlayerId,
                player.DisplayName,
                player.IsHost,
                string.Equals(player.PlayerId, currentPlayerId, StringComparison.Ordinal),
                index + 1));
        }
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        return string.IsNullOrWhiteSpace(roomCode)
            ? string.Empty
            : roomCode.Trim().ToUpperInvariant();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? string.Empty
            : displayName.Trim();
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
